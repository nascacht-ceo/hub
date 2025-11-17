using Amazon.S3;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using nc.Cloud;

namespace nc.Aws;

public class S3StorageProvider : IStorageProvider
{
	private readonly IAmazonS3 _s3Client;
	private readonly S3StorageProviderOptions _options;

	public S3StorageProvider(IOptions<S3StorageProviderOptions> options, IAmazonS3 s3Client)
	{
		_s3Client = s3Client;
		_options = options.Value ?? throw new ArgumentNullException(nameof(S3StorageProviderOptions));
	}
	public ValueTask DisposeAsync()
	{
		throw new NotImplementedException();
	}

	public IAsyncEnumerable<IStorageInfo> GetFolderContentsAsync(string driveName, string subpath, CancellationToken cancellation = default)
	{
		throw new NotImplementedException();
	}

	public Task<IStorageInfo> GetStorageInfoAsync(string driveName, string subpath, CancellationToken cancellation = default)
	{
		throw new NotImplementedException();
	}

	public IStorageInfo New(string driveName, string relativePath, bool isFolder = false, IDictionary<string, string?>? Tags = null, IDictionary<string, string?>? Metadata = null)
	{
		return new S3StorageInfo(_s3Client)
		{
			DriveName = driveName,
			RelativePath = relativePath,
			Metadata = Metadata ?? new Dictionary<string, string?>(),
			Tags = Tags ?? new Dictionary<string, string?>()
		};
	}

	public IChangeToken Watch(string driveName, string filter)
	{
		throw new NotImplementedException();
	}
}
