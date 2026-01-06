using FluentStorage.Blobs;
using System.IO.Abstractions;

namespace nc.Extensions.FluentStorage;

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
				if (blob.Metadata.ContainsKey(MimeType))
					return blob.Metadata[MimeType];
				string? mimeType = null;
				if (blob.Properties.ContainsKey("ContentType"))
					mimeType = blob.Properties["ContentType"] as string;
				if (mimeType == null && blob.Properties.ContainsKey("Content-Type"))
					mimeType = blob.Properties["Content-Type"] as string;
				if (mimeType != null)
					blob.Metadata[MimeType] = mimeType;
				return mimeType;
			}
			set => blob.Metadata[MimeType] = value;
		}

		public string? Extension => Path.GetExtension(blob?.Name);
	}
}
