using FluentStorage.Blobs;
using System.IO.Abstractions;

namespace nc.Storage;

public static class BlobExtensions
{
	public const string Uri = "nc:uri";
	public const string MimeType = "nc:mime-type";

	extension(Blob blob)
	{
		public string? Uri 
		{
			get => blob?.Metadata[Uri];
			set => blob.Metadata[Uri] = value;
		}

		public string? MimeType
		{
			get 
			{
				if (blob == null) return null;
				if (blob.Metadata.TryGetValue(MimeType, out string? value))
					return value;
				string? mimeType = null;
				if (blob.Properties.TryGetValue("ContentType", out object? value1))
					mimeType = value1 as string;
				if (mimeType == null && blob.Properties.TryGetValue("Content-Type", out object? value2))
					mimeType = value2 as string;
				if (mimeType != null)
					blob.Metadata[MimeType] = mimeType;
				return mimeType;
			}
			set => blob.Metadata[MimeType] = value;
		}

		public string? Extension => Path.GetExtension(blob?.Name);
	}
}
