using FluentStorage.Blobs;

namespace nc.Storage;

public interface IBlobTransformer
{
	Blob TransformAsync(Blob source);
}
