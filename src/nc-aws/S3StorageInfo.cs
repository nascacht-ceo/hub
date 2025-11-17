using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using nc.Cloud;
using NetTopologySuite.Algorithm;
using System.IO.Pipelines;
using System.Threading;

namespace nc.Aws;

public class S3StorageInfo : IStorageInfo
{
	private readonly string _bucketName;
	private readonly string _key;
	private readonly IAmazonS3 _s3Client;
	private readonly S3Object? _s3Object;
	private readonly GetObjectMetadataResponse? _metadata;
	private readonly bool _isFolder;

	//public S3StorageInfo(string bucketName, string key, S3Object? s3Object, IAmazonS3 s3Client, bool isFolder = false)
	//{
	//	_bucketName = bucketName;
	//	_key = key;
	//	_s3Object = s3Object;
	//	_s3Client = s3Client;
	//	_isFolder = isFolder;
	//	Exists = s3Object != null;
	//	Name = key.Split('/').Last();
	//	RelativePath = key;
	//	Length = s3Object?.Size ?? -1;
	//	LastModified = s3Object?.LastModified ?? DateTimeOffset.MinValue;
	//	Metadata = new Dictionary<string, string?>();
	//}

	public S3StorageInfo(IAmazonS3 s3Client)
	{
		_s3Client = s3Client;
		//IsFolder = key.EndsWith("/");
		//Exists = metadata != null;
		//Name = key.Split('/').Last();
		//RelativePath = key;
		//Length = metadata?.ContentLength ?? -1;
		//LastModified = metadata?.LastModified ?? DateTimeOffset.MinValue;
		//Metadata = metadata?.Metadata?.ToDictionary()
		//	?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
	}
	public required string DriveName { get; set; }
	public bool Exists { get; private set; }
	public long Length { get; private set; }
	public required string RelativePath { get; set; }
	public string Name { get; private set; }
	public DateTimeOffset LastModified { get; private set; }
	public bool IsFolder { get; private set; }
	public IDictionary<string, string?> Metadata { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

	public IDictionary<string, string?> Tags { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

	public ValueTask<PipeReader> CreateReaderAsync(CancellationToken cancellation = default)
	{
		var pipe = new Pipe();
		var pipeWriter = pipe.Writer;

		// Start a background task to handle the download and stream-to-pipe transfer
		_ = Task.Run(async () =>
		{
			try
			{
				// 1. Initiate S3 Download
				var request = new GetObjectRequest { BucketName = DriveName, Key = RelativePath };
				using var response = await _s3Client.GetObjectAsync(request);
				using var s3Stream = response.ResponseStream;

				// 2. Efficiently copy the S3 Stream content to the PipeWriter
				// Use CopyToAsync for efficient transfer from a Stream to another Stream/PipeWriter's AsStream()
				await s3Stream.CopyToAsync(pipeWriter.AsStream(), cancellation);

				// Or, for maximum performance using a manual loop (advanced):
				// await StreamToPipeTransferAsync(s3Stream, pipeWriter, cancellationToken);

				// 3. Mark the PipeWriter as complete when done
				await pipeWriter.CompleteAsync();
			}
			catch (Exception ex)
			{
				// 4. Signal failure to the PipeReader's consumer
				await pipeWriter.CompleteAsync(ex);
			}
		}, cancellation);

		// Return the reader side to the consumer
		return ValueTask.FromResult(pipe.Reader);
	}

	public async ValueTask<PipeWriter> CreateWriterAsync(CancellationToken cancellation = default)
	{
		// 1. Create a Pipe to manage the flow of data
		var pipe = new Pipe();
		var pipeReader = pipe.Reader;
		var pipeWriter = pipe.Writer;

		// 2. Convert the PipeReader into a Stream
		// The AsStream() extension method (from Microsoft.IO.Pipelines.Extensions) 
		// is often the simplest way to get a readable Stream from a PipeReader.
		var uploadStream = pipeReader.AsStream();

		// 3. Start the S3 upload in a background task
		_ = Task.Run(async () =>
		{
			try
			{
				var transferUtility = new TransferUtility(_s3Client);

				var uploadRequest = new TransferUtilityUploadRequest
				{
					BucketName = DriveName,
					Key = RelativePath,
					InputStream = uploadStream
				};

				// Add user metadata
				foreach (var kvp in Metadata)
				{
					if (kvp.Key != null && kvp.Value != null)
					{
						uploadRequest.Metadata.Add(kvp.Key, kvp.Value);
					}
				}
				// Add tags
				uploadRequest.TagSet.AddRange(Tags.Select(t => new Amazon.S3.Model.Tag()
				{
					Key = t.Key,
					Value = t.Value
				}));

				// This call initiates the TransferUtility's background logic.
				// The TransferUtility will read from 'uploadStream' (which reads from pipeReader) 
				// in chunks to perform the multipart upload.
				await transferUtility.UploadAsync(uploadRequest, cancellation);

				// NOTE: The PipeReader is automatically completed when the 
				// PipeWriter's Complete/CompleteAsync method is called by the consumer 
				// (see step 3).
			}
			catch (Exception ex)
			{
				// If the S3 upload fails (network, S3 error, etc.), propagate the error 
				// back to the consumer trying to write to the pipe.
				await pipeWriter.CompleteAsync(ex);
			}
			finally
			{
				// Ensure the PipeReader is completed/cleaned up regardless of success or failure.
				await pipeReader.CompleteAsync();
			}
		}, cancellation);

		// Return the writer side to the consumer
		return pipeWriter;
	}

	//public async Task<Stream> CreateReadStreamAsync(CancellationToken cancellation = default)
	//{
	//	var response = _s3Client.GetObjectAsync(_bucketName, _key).GetAwaiter().GetResult();
	//	return response.ResponseStream;
	//}

	//public async Task<Stream> CreateWriteStreamAsync(CancellationToken cancellation = default)
	//{
	//	// S3 does not support direct streaming writes; use a MemoryStream and upload on dispose
	//	return new S3WriteStream(_s3Client, _bucketName, _key);
	//}

	private class S3WriteStream : MemoryStream
	{
		private readonly IAmazonS3 _client;
		private readonly string _bucket;
		private readonly string _key;

		public S3WriteStream(IAmazonS3 client, string bucket, string key)
		{
			_client = client;
			_bucket = bucket;
			_key = key;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Position = 0;
				var putRequest = new PutObjectRequest
				{
					BucketName = _bucket,
					Key = _key,
					InputStream = this
				};
				_client.PutObjectAsync(putRequest).GetAwaiter().GetResult();
			}
			base.Dispose(disposing);
		}
	}
}
