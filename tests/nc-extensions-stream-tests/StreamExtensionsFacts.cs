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

	public class VisualHashFact : StreamExtensionsFacts
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
			Assert.Equal(original.VisualHash(), nearDuplicate.VisualHash());
		}
	}
}
