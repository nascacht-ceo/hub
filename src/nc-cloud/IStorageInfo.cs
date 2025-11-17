using System.IO.Pipelines;

namespace nc.Cloud;

/// <summary>
/// Represents an abstraction for accessing and managing information about a file or directory in an underlying storage
/// system.
/// </summary>
/// <remarks>This interface provides properties and methods to retrieve metadata, check existence, and perform
/// read/write operations on files or directories. It supports both synchronous and asynchronous operations for working
/// with data streams. Implementations of this interface are expected to handle storage-specific details, such as file
/// system paths or metadata storage, while exposing a consistent API for consumers.</remarks>
public interface IStorageInfo
{
	/// <summary>
	/// Gets a value that indicates if the resource exists in the underlying storage system.
	/// </summary>
	bool Exists { get; }

	/// <summary>
	/// Gets the length of the file in bytes, or -1 for a directory or nonexistent file.
	/// </summary>
	long Length { get; }

	/// <summary>
	/// Name of the drive or storage container where the file is located. Returns <see langword="null"/> if not applicable.
	/// </summary>
	string DriveName { get; }

	/// <summary>
	/// Gets the path to the file, including the file name. Returns <see langword="null"/> if the file is not directly accessible.
	/// </summary>
	string RelativePath { get; }

	/// <summary>
	/// Gets the name of the file or directory, not including any path.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets the time when the file was last modified.
	/// </summary>
	DateTimeOffset LastModified { get; }

	/// <summary>
	/// Gets a value that indicates whether the path is a folder.
	/// </summary>
	bool IsFolder { get; }


	/// <summary>
	/// Gets a collection of metadata key-value pairs associated with the object.
	/// </summary>
	/// <remarks>The metadata can be used to store additional information about the object, such as custom
	/// attributes or descriptive properties. Keys are case-sensitive, and duplicate keys are not allowed.</remarks>
	IDictionary<string, string?> Metadata { get; }

	/// <summary>
	/// Key-value pairs intended for indexing, search filtering, or lifecycle management.
	/// Limited in count by providers (e.g., 10 for S3/Azure).
	/// </summary>
	IDictionary<string, string?> Tags { get; }

	/// <summary>
	/// Creates and returns a <see cref="PipeReader"/> for reading data asynchronously.
	/// </summary>
	/// <remarks>The returned <see cref="PipeReader"/> provides an efficient way to process data streams
	/// asynchronously. Ensure proper disposal of the <see cref="PipeReader"/> to release resources when it is no longer
	/// needed.</remarks>
	/// <returns>A <see cref="PipeReader"/> instance that can be used to read data from the underlying pipe.</returns>
	ValueTask<PipeReader> CreateReaderAsync(CancellationToken cancellation = default);

	/// <summary>
	/// Creates and returns a <see cref="PipeWriter"/> for writing data asynchronously.
	/// </summary>
	/// <remarks>The returned <see cref="PipeWriter"/> is designed for asynchronous operations. Ensure proper
	/// disposal of the writer to release any associated resources.</remarks>
	/// <returns>A <see cref="PipeWriter"/> instance that can be used to write data to the underlying pipe.</returns>
	ValueTask<PipeWriter> CreateWriterAsync(CancellationToken cancellation = default);

	/// <summary>
	/// Creates a readable <see cref="Stream"/> for consuming data asynchronously.
	/// </summary>
	/// <remarks>The returned <see cref="Stream"/> supports asynchronous read operations.  Callers are responsible
	/// for properly disposing of the stream when it is no longer needed.</remarks>
	/// <returns>A <see cref="Stream"/> that can be used to read data asynchronously.</returns>
	async ValueTask<Stream> ReadAsync(CancellationToken cancellation = default)
		=> (await CreateReaderAsync(cancellation)).AsStream();

	/// <summary>
	/// Asynchronously creates a writable <see cref="Stream"/> for sending data.
	/// </summary>
	/// <remarks>The returned <see cref="Stream"/> can be used to write data asynchronously.  Ensure proper disposal
	/// of the stream to release resources.</remarks>
	/// <returns>A writable <see cref="Stream"/> for sending data.</returns>
	virtual async ValueTask<IStorageInfo> WriteAsync(Stream source, CancellationToken cancellation = default)
	{
		var writer = await CreateWriterAsync(cancellation);
		await source.CopyToAsync(writer.AsStream(), cancellation);
		await writer.CompleteAsync();
		return this;
	}
}
