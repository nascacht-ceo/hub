using FluentStorage.Blobs;

namespace nc.Extensions.FluentStorage.Tests;

public class BlobExtensionsFacts
{
	public class Uri: BlobExtensionsFacts
	{
		[Fact]
		public void MapsMetadata()
		{
			// Arrange
			var blob = new Blob("/some/folder/file.txt", BlobItemKind.File)
			{
				Uri = "memory://some/folder/file.txt"
			};
			Assert.Contains(BlobExtensions.Uri, blob.Metadata);
			Assert.Equal("memory://some/folder/file.txt", blob.Metadata[BlobExtensions.Uri]);
		}
	}

	public class MimeType : BlobExtensionsFacts
	{
		[Theory]
		[InlineData("ContentType")]
		[InlineData("Content-Type")]
		public void ReadsProperty(string key)
		{
			// Arrange
			var blob = new Blob("/some/folder/file.txt", BlobItemKind.File);
			blob.Properties.Add(key, "text/plain");
			Assert.Equal("text/plain", blob.MimeType);
		}

		[Fact]
		public void MapsMetadata()
		{
			// Arrange
			var blob = new Blob("/some/folder/file.txt", BlobItemKind.File)
			{
				MimeType = "text/plain"
			};
			Assert.Contains(BlobExtensions.MimeType, blob.Metadata);
			Assert.Equal("text/plain", blob.Metadata[BlobExtensions.MimeType]);
		}
	}

	public class Extension : BlobExtensionsFacts
	{
		[Theory]
		[InlineData("/some/folder/file.txt", ".txt")]
		[InlineData("/some/folder/file.txt.pdf", ".pdf")]
		[InlineData("/some/folder/guid", "")]
		[InlineData("/some/folder/", "")]
		[InlineData(null, "")]
		public void ExtractsFromFilename(string? path, string? extension)
		{
			var blob = new Blob(path, BlobItemKind.File);
			Assert.Equal(extension, blob.Extension);
		}
	}

}
