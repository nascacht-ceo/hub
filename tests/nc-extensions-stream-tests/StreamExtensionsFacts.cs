using nc.Extensions.Stream.Tests;
using nc_extensions_streaming;
using System.Runtime.CompilerServices;

namespace nc_extensions_stream_tests;

public class StreamExtensionsFacts
{
	public class CryptographicHashFact: StreamExtensionsFacts
	{
		[Fact]
		public void Test1()
		{

		}
	}

	public class VisualHashFacts : StreamExtensionsFacts
	{
		[Theory]
		[InlineData("../../../../data/sample.477x640.jpg")]
		[InlineData("../../../../data/sample.jpeg")]
		[InlineData("../../../../data/sample.moving.gif")]
		public void ComputesFingerprint(string path)
		{
			using var stream = File.OpenRead(path);
			var fingerprint = stream.VisualHash();
			Assert.NotEqual(0UL, fingerprint);
		}

		[Theory]
		[InlineData("../../../../data/sample.477x640.jpg")]
		[InlineData("../../../../data/sample.jpeg")]
		[InlineData("../../../../data/sample.moving.gif")]
		public void MatchesSimilarImages(string path)
		{
			var extension = Path.GetExtension(path);
			var duplicate = $"{Guid.NewGuid()}.{extension}";
			Helpers.CreateImageNearDuplicate(path, duplicate);
			using var original = File.OpenRead(path);
			using var nearDuplicate = File.OpenRead(duplicate);
			Assert.NotEqual(original.CryptographicHash(), nearDuplicate.CryptographicHash());
			var originalHash = original.VisualHash();
			var nearDuplicateHash = nearDuplicate.VisualHash();
			int distance = System.Numerics.BitOperations.PopCount(originalHash ^ nearDuplicateHash);
			Assert.True(distance <= 3);
		}

		[Theory]
		[InlineData("../../../../data/sample.477x640.jpg", "../../../../data/sample.jpeg")]
		[InlineData("../../../../data/sample.jpeg", "../../../../data/sample.moving.gif")]
		[InlineData("../../../../data/sample.477x640.jpg", "../../../../data/sample.moving.gif")]
		public void DoesNotMatchSimilarImages(string imageA, string imageB)
		{
			var streamA = File.OpenRead(imageA);
			var streamB = File.OpenRead(imageB);
			var hashA = streamA.VisualHash();
			var hashB = streamB.VisualHash();
			int distance = System.Numerics.BitOperations.PopCount(hashA ^ hashB);
			Assert.True(distance >= 10);
		}

	}

	public class GetMimeTypeFacts: StreamExtensionsFacts
	{
		[Theory]
		[InlineData("../../../../data/sample.477x640.jpg", "image/jpeg")]
		[InlineData("../../../../data/sample.jpeg", "image/jpeg")]
		[InlineData("../../../../data/sample.moving.gif", "image/gif")]
		[InlineData("../../../../data/speech.mp3", "audio/mpeg")]
		[InlineData("../../../../data/speech.wav", "audio/wav")]
		[InlineData("../../../../data/sample.flac", "audio/flac")]
		[InlineData("../../../../data/bookmark.pdf", "application/pdf")]
		public void IdentifiesMimeType(string path, string expectedMimeType)
		{
			using var stream = File.OpenRead(path);
			var mimeType = stream.GetMimeType();
			Assert.Equal(expectedMimeType, mimeType);
		}
	}
}
