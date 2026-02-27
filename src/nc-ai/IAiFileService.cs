using System.IO.Pipelines;

namespace nc.Ai;

/// <summary>
/// Uploads files to an AI provider's file storage using the System.IO.Pipelines abstraction.
/// </summary>
/// <typeparam name="TService">The concrete service type (curiously recurring template pattern).</typeparam>
/// <typeparam name="TReturn">The type returned by the provider after a successful upload.</typeparam>
public interface IAiFileService<TService, TReturn> where TService : IAiFileService<TService, TReturn>
{
	/// <summary>Uploads content from a <see cref="PipeReader"/> to the AI provider's file storage.</summary>
	/// <param name="reader">The pipeline reader providing the file bytes.</param>
	/// <param name="mediaType">The MIME type of the content (e.g. <c>application/pdf</c>).</param>
	/// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
	Task<TReturn> UploadAsync(PipeReader reader, string mediaType, CancellationToken cancellationToken);
}

/// <summary>
/// Extension methods for <see cref="IAiFileService{TService,TReturn}"/> that adapt
/// <see cref="Stream"/> and <see cref="Uri"/> inputs to the pipe-based core interface.
/// </summary>
public static class Extensions
{
	/// <summary>Uploads content from a <see cref="Stream"/> by bridging it to a <see cref="Pipe"/>.</summary>
	/// <param name="service">The file service instance.</param>
	/// <param name="stream">The source stream to upload.</param>
	/// <param name="mediaType">The MIME type of the content.</param>
	/// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
	public static async Task<TReturn> UploadAsync<TService, TReturn>(this IAiFileService<TService, TReturn> service, Stream stream, string mediaType, CancellationToken cancellationToken)
		where TService : IAiFileService<TService, TReturn>
	{
		var pipe = new Pipe();
		_ = Task.Run(async () =>
		{
			await stream.CopyToAsync(pipe.Writer, cancellationToken);
			await pipe.Writer.CompleteAsync();
		}, cancellationToken);

		return await service.UploadAsync(pipe.Reader, mediaType, cancellationToken);
	}

	/// <summary>Downloads content from a <see cref="Uri"/> and uploads it to the AI provider's file storage.</summary>
	/// <param name="service">The file service instance.</param>
	/// <param name="uri">The HTTP/HTTPS URI to download from.</param>
	/// <param name="mediaType">The MIME type of the content.</param>
	/// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
	public static async Task<TReturn> UploadUriAsync<TService, TReturn>(this IAiFileService<TService, TReturn> service, Uri uri, string mediaType, CancellationToken cancellationToken)
		where TService : IAiFileService<TService, TReturn>
	{
		ArgumentNullException.ThrowIfNull(uri);
		using var httpClient = new HttpClient();
		using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		response.EnsureSuccessStatusCode();
		using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

		//using var target = new FileStream("w2.pdf", FileMode.Create, FileAccess.Write);
		//await stream.CopyToAsync(target, cancellationToken);
		//target.Flush();
		//target.Dispose();
		//stream.Position = 0;
		var pipe = new Pipe();

		var copy = Task.Run(async () =>
		{
			try
			{
				await stream.CopyToAsync(pipe.Writer, cancellationToken);
			}
			finally
			{
				await pipe.Writer.CompleteAsync(); 
			}
		}, cancellationToken).ContinueWith(t =>
		{
			if (t.Exception != null)
				throw t.Exception;
		}, TaskContinuationOptions.OnlyOnFaulted);

		// Start copying from the HTTP stream to the pipe writer in the background
		//var copyTask = Task.Run(async () =>
		//{
		//	await stream.CopyToAsync(pipe.Writer, cancellationToken);
		//	await pipe.Writer.CompleteAsync();
		//}, cancellationToken);

		//var copyTask = stream
		//	.CopyToAsync(pipe.Writer, cancellationToken)
		//	.ContinueWith(t => pipe.Writer.CompleteAsync(t.Exception), cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);


		try
		{
			// 3. Pass the PipeReader directly to the service
			// The service's PipeReader.AsStream() will now pull data as the copyTask pushes it.
			var result = await service.UploadAsync(pipe.Reader, mediaType, cancellationToken);

			// 4. Await the copy task here to ensure all data was written and to propagate any exceptions
			await copy;

			return result;
		}
		finally
		{
			// Ensure the PipeReader is completed/disposed if something failed before the copyTask completed
			await pipe.Reader.CompleteAsync();
		}
	}
}
