using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace nc.Cloud;

public interface IStorageProvider: IAsyncDisposable
{
	public IStorageInfo New(string driveName, string relativePath, bool isFolder = false, IDictionary<string, string?>? Tags = null, IDictionary<string, string?>? Metadata = null);

	/// <summary>
	/// Locate a file at the given path.
	/// </summary>
	/// <param name="subpath">Relative path that identifies the file.</param>
	/// <returns>The file information. Caller must check Exists property.</returns>
	Task<IStorageInfo> GetStorageInfoAsync(string driveName, string subpath, CancellationToken cancellation = default);

	/// <summary>
	/// Enumerate a folder at the given path, if any.
	/// </summary>
	/// <param name="subpath">The relative path that identifies the folder.</param>
	/// <returns>The contents of the folder.</returns>
	IAsyncEnumerable<IStorageInfo> GetFolderContentsAsync(string driveName, string subpath, CancellationToken cancellation = default);

	/// <summary>
	/// Creates a <see cref="IChangeToken"/> for the specified <paramref name="filter"/>.
	/// </summary>
	/// <param name="filter">Filter string used to determine what files or folders to monitor. Example: **/*.cs, *.*, subFolder/**/*.cshtml.</param>
	/// <returns>An <see cref="IChangeToken"/> that is notified when a file matching <paramref name="filter"/> is added, modified or deleted.</returns>
	IChangeToken Watch(string driveName, string filter);
}

