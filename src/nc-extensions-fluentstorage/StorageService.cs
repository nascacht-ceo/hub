using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Azure.Storage.Blobs.Models;
using FluentStorage;
using FluentStorage.Blobs;
using FluentStorage.Utils.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace nc.Extensions.FluentStorage;

public class StorageService
{
	private readonly StorageServiceOptions _options;
	private readonly ILogger<StorageServiceOptions>? _logger;

	public StorageService()
	{
		_options ??= new StorageServiceOptions();
		StorageFactory.Modules.UseAwsStorage();
		StorageFactory.Modules.UseAzureBlobStorage();
		StorageFactory.Modules.UseAzureFilesStorage();
		StorageFactory.Modules.UseGoogleCloudStorage();
		StorageFactory.Modules.UseFtpStorage();
		StorageFactory.Modules.UseSftpStorage();
	}

	public StorageService(StorageServiceOptions options)
		: this()
	{
		_options = options ?? new StorageServiceOptions();
	}

	public StorageService(IOptions<StorageServiceOptions> options, ILogger<StorageServiceOptions>? logger = null)
		: this()
	{
		_options = options.Value ?? new StorageServiceOptions();
		_logger = logger;
	}

	public string GetConnectionString(Uri uri)
	{
		// 1. Validation: Ensure we support this scheme
		var scheme = uri.Scheme.ToLowerInvariant();
		if (!_options.PrefixMapping.TryGetValue(scheme, out string? prefix))
			throw new ArgumentOutOfRangeException(nameof(uri),
				$"Uri scheme '{scheme}' is invalid. Allowed schemes are: {string.Join(",", _options.PrefixMapping.Keys)}");

		// 2. Lookup Credentials (Longest Prefix Match)
		var credential = _options.CredentialCache.GetCredential(uri, _options.AuthenticationType);
		bool isImplicit = credential == null;

		// 3. Hydrate Connection Strings
		return prefix switch
		{
			StorageProviders.AwsS3 => isImplicit
				? $"aws.s3://bucket={uri.Host};region={credential?.Domain ?? _options.AwsRegionDefault}"
				: $"aws.s3://key={credential!.UserName};secret={credential.Password};region={credential?.Domain ?? _options.AwsRegionDefault};bucket={uri.Host}",

			StorageProviders.AzureBlob => isImplicit
				? $"azure.blob://account={uri.Host};identity=true"
				: $"azure.blob://account={uri.Host};key={credential!.Password}",

			StorageProviders.AzureFile => isImplicit
				? throw new NotSupportedException("Azure Files requires explicit keys in FluentStorage.")
				: $"azure.file://account={uri.Host};key={credential!.Password}",

			StorageProviders.GoogleStorage => !_options.GcpIdentities.ContainsKey(uri.Host)
				? $"google.storage://bucket={uri.Host}" // Uses GOOGLE_APPLICATION_CREDENTIALS env var
				: $"google.storage://project={uri.Host};cred={_options.GetGcpCredential(uri.Host)}", // Pass JSON path as password

			StorageProviders.Sftp => isImplicit
				? throw new UnauthorizedAccessException("SFTP requires explicit credentials.")
				: $"sftp://host={uri.Host};user={credential!.UserName};pass={credential.Password}",

			StorageProviders.Ftp => isImplicit
				? $"ftp://host={uri.Host};user=anonymous"
				: $"ftp://host={uri.Host};user={credential!.UserName};pass={credential.Password}",

			StorageProviders.Disk => $"disk://path={uri.LocalPath}",

			StorageProviders.Memory => "mem://",

			_ => throw new ArgumentOutOfRangeException(nameof(uri),
				$"Uri scheme mapping '{prefix}' is invalid. Allowed prefixes are: {string.Join(",", _options.PrefixMapping.Values.Distinct())}")
		};
	}
	public IBlobStorage GetBlobStorage(Uri uri)
	{
		var connectionString = GetConnectionString(uri);
		switch (connectionString.Split("://")[0])
		{
			case "google.storage":
				if (connectionString.Contains("cred"))
					return StorageFactory.Blobs.FromConnectionString(connectionString);
				else 
					return StorageFactory.Blobs.GoogleCloudStorageFromEnvironmentVariable(uri.Host);
			default:
				return StorageFactory.Blobs.FromConnectionString(connectionString);
		}
	}

	#region IBlobStorage Implementation
	public async Task<IEnumerable<Blob>> ListAsync(string url, CancellationToken cancellationToken = default)
	{
		var uri = new Uri(url);	
		var options = ToListOptions(uri, out var extension);
		using var storage = GetBlobStorage(uri);

		var allBlobs = await storage.ListAsync(options, cancellationToken);
		foreach (var blob in allBlobs)
			blob.Uri = $"{uri.Scheme}://{uri.Host}/{blob.FullPath}";

			// Update FullPath to be a universal URI
		if (string.IsNullOrEmpty(extension))
			return allBlobs;

		return allBlobs.Where(b => b.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
	}

	public async Task WriteAsync(string url, Stream dataStream, bool append = false, CancellationToken cancellationToken = default)
	{
		var uri = new Uri(url);
		using var blobStorage = GetBlobStorage(uri);
		// await blobStorage.WriteAsync(uri.AbsolutePath.TrimStart('/'), dataStream, append, cancellationToken);
		await blobStorage.WriteAsync(StoragePath.Normalize(uri.AbsolutePath), dataStream, append, cancellationToken);
	}

	public async Task<Stream> OpenReadAsync(string url, CancellationToken cancellationToken = default)
	{
		var uri = new Uri(url);
		using var blobStorage = GetBlobStorage(uri);
		return await blobStorage.OpenReadAsync(StoragePath.Normalize(uri.AbsolutePath), cancellationToken);
	}

	public async Task DeleteAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default)
	{
		// 1. Convert strings to Uris and group by the root (Scheme + Host)
		var groups = urls
			.Select(path => new Uri(path))
			.GroupBy(uri => $"{uri.Scheme}://{uri.Host}");

		// 2. Parallel execute across the different storage targets
		await Parallel.ForEachAsync(groups, cancellationToken, async (group, ct) =>
		{
			// group.Key is the root URI (e.g., s3://my-bucket)
			var rootUri = new Uri(group.Key);

			// Resolve the storage provider once for this group
			using var blobStorage = GetBlobStorage(rootUri);

			// Convert the group's Uris into relative paths for FluentStorage
			// We trim the leading '/' because FluentStorage prefers 'folder/file.txt' 
			// over '/folder/file.txt' for many providers.
			string[] relativePaths = [.. group.Select(uri => StoragePath.Normalize(uri.AbsolutePath))];

			// 3. Batch delete all files for this specific provider
			await blobStorage.DeleteAsync(relativePaths, ct);
		});
	}

	public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<string> urls, [EnumeratorCancellation] CancellationToken ct = default)
	{
		// 1. Group by provider as before
		var groups = urls
			.Select(p => new Uri(p))
			.GroupBy(uri => $"{uri.Scheme}://{uri.Host}");

		foreach (var group in groups)
		{
			using var storage = GetBlobStorage(new Uri(group.Key));
			var paths = group.Select(uri => StoragePath.Normalize(uri.AbsolutePath)).ToList();

			// 2. Process this group in chunks
			for (int i = 0; i < paths.Count; i += _options.BatchSize)
			{
				var batch = paths.Skip(i).Take(_options.BatchSize).ToArray();
				var results = await storage.ExistsAsync(batch, ct);

				// 3. Yield results immediately for this batch
				foreach (var result in results)
				{
					yield return result;
				}
			}
		}
	}

	public async IAsyncEnumerable<Blob> GetBlobsAsync(IEnumerable<string> urls, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		// 1. Group by storage target (Scheme + Host)
		var groups = urls
			.Select(p => new Uri(p))
			.GroupBy(uri => $"{uri.Scheme}://{uri.Host}");

		// 2. Create an unbounded channel to collect results from all parallel tasks
		var channel = Channel.CreateUnbounded<Blob>();

		// 3. Fire off the parallel processing
		var backgroundTask = Parallel.ForEachAsync(groups, cancellationToken, async (group, token) =>
		{
			try
			{
				var uri = new Uri(group.Key);
				using var storage = GetBlobStorage(uri);
				var relativePaths = group.Select(uri => StoragePath.Normalize(uri.AbsolutePath)).ToArray();

				// Fetch metadata in bulk for this specific provider
				var blobs = await storage.GetBlobsAsync(relativePaths, token);

				foreach (var blob in blobs)
				{
					blob.Uri = $"{uri.Scheme}://{uri.Host}/{blob.FullPath}";
					// Write each blob to the channel as soon as it's ready
					await channel.Writer.WriteAsync(blob, token);
				}
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Error fetching blobs from storage provider for group {GroupKey}", group.Key);
				// Optional: Log or handle partial failures here
				// Note: Parallel.ForEachAsync will propagate exceptions when the task is awaited
			}
		}).ContinueWith(_ => channel.Writer.Complete(), cancellationToken); // Close channel when all groups finish

		// 4. Yield items from the channel reader as they arrive
		await foreach (var blob in channel.Reader.ReadAllAsync(cancellationToken))
		{
			yield return blob;
		}

		// 5. Ensure any exceptions from the background processing are thrown
		await backgroundTask;
	}

	//public Task SetBlobsAsync(IEnumerable<Blob> blobs, CancellationToken cancellationToken = default)
	//{
	//	throw new NotImplementedException();
	//}

	public async Task SetBlobsAsync(IEnumerable<Blob> blobs, CancellationToken cancellationToken = default)
	{
		// 1. Group by the root storage target (Scheme + Host)
		// We assume the Blob.FullPath is a universal URI (e.g., s3://bucket/path)
		var groups = blobs
			.Select(blob => new { Original = blob, Uri = new Uri(blob.FullPath) })
			.GroupBy(item => $"{item.Uri.Scheme}://{item.Uri.Host}");

		// 2. Parallel execute updates across providers
		await Parallel.ForEachAsync(groups, cancellationToken, async (group, ct) =>
		{
			var rootUri = new Uri(group.Key);
			using var blobStorage = GetBlobStorage(rootUri);

			await blobStorage.SetBlobsAsync(group.Select(item =>
			{
				var relativePath = StoragePath.Normalize(item.Uri.AbsolutePath);
				var targetBlob = new Blob(relativePath, item.Original.Kind)
				{
					Uri = $"{rootUri.Scheme}://{rootUri.Host}/{relativePath}",
				};
				targetBlob.Metadata.AddRange(item.Original.Metadata);
				targetBlob.Properties.AddRange(item.Original.Properties);
				return targetBlob;
			}), ct);
		});
	}

	public static ListOptions ToListOptions(Uri uri, out string? extensionFilter)
	{

		// 1. Separate the bucket/host from the path
		// Path: "/folder/*.pdf"
		string fullPath = StoragePath.Normalize(uri.AbsolutePath);

		// 2. Identify the wildcard index to find the 'Static Prefix'
		int wildcardIndex = fullPath.IndexOf('*');

		if (wildcardIndex == -1)
		{
			// No wildcard: standard folder list
			extensionFilter = null;
			return new ListOptions { FolderPath = fullPath };
		}

		// 3. Extract the static part for server-side filtering (e.g., "folder/")
		string prefix = fullPath[..wildcardIndex];

		// 4. Extract the extension for client-side filtering (e.g., ".pdf")
		extensionFilter = Path.GetExtension(fullPath);

		return new ListOptions
		{
			// We set FolderPath to the directory containing the wildcard
			FolderPath = Path.GetDirectoryName(prefix)?.Replace('\\', '/'),

			// Use FilePrefix for an extra layer of server-side optimization
			// (e.g., if path was /logs/2024-*.log, prefix is "2024-")
			FilePrefix = Path.GetFileName(prefix),

			// Recurse should be true if the URI has a double wildcard (**)
			Recurse = fullPath.Contains("**")
		};
	}
	#endregion IBlobStorage Implementation
}
