using Microsoft.Extensions.Configuration;
using nc.Storage;
using nc.Extensions.Streaming;
using System.IO;
using Xunit;

namespace nc.Extensions.Stream.Tests;

public class StreamExtensionsFacts: IClassFixture<StreamExtensionsFixture>
{
	protected readonly StreamExtensionsFixture _fixture;

	public StreamExtensionsFacts(StreamExtensionsFixture fixture) 
		=> _fixture = fixture;

	public class CryptographicHashFact(StreamExtensionsFixture fixture) 
		: StreamExtensionsFacts(fixture)
	{
		[Theory]
		[InlineData("data/bookmark.pdf")]
		[InlineData("data/sample.477x640.jpg")]
		[InlineData("data/sample.jpeg")]
		[InlineData("data/sample.moving.gif")]
		public void ComputersCryptographicFingerprint(string path)
		{
			var copy = Path.GetTempFileName();
			File.Copy(path, copy, true);
			using var source = File.OpenRead(path);
			var sourceHash = source.CryptographicHash();
			using var target = File.OpenRead(copy);
			var targetHash = target.CryptographicHash();
			Assert.Equal(sourceHash, targetHash);
		}
	}

	public class VisualHashFacts : StreamExtensionsFacts
	{
		public VisualHashFacts(StreamExtensionsFixture fixture) : base(fixture)
		{
		}

		[Theory]
		[InlineData("data/sample.477x640.jpg")]
		[InlineData("data/sample.jpeg")]
		[InlineData("data/sample.moving.gif")]
		public void ComputesVisualFingerprint(string path)
		{
			using var stream = File.OpenRead(path);
			var fingerprint = stream.VisualHash();
			Assert.NotEqual(0UL, fingerprint);
		}

		[Theory]
		[InlineData("data/sample.477x640.jpg")]
		[InlineData("data/sample.jpeg")]
		[InlineData("data/sample.moving.gif")]
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
		[InlineData("data/sample.477x640.jpg", "data/sample.jpeg")]
		[InlineData("data/sample.jpeg", "data/sample.moving.gif")]
		[InlineData("data/sample.477x640.jpg", "data/sample.moving.gif")]
		public void DoesNotMatchDifferentImages(string imageA, string imageB)
		{
			var streamA = File.OpenRead(imageA);
			var streamB = File.OpenRead(imageB);
			var hashA = streamA.VisualHash();
			var hashB = streamB.VisualHash();
			int distance = System.Numerics.BitOperations.PopCount(hashA ^ hashB);
			Assert.True(distance >= 10);
		}

	}

	public class SemanticHashFacts(StreamExtensionsFixture fixture) 
		: StreamExtensionsFacts(fixture)
	{
		[Theory]
		[InlineData("data/bookmark.txt")]
		[InlineData("../../../StreamExtensionsFacts.cs")]
		public void ComputesSemanticFingerprint(string path)
		{
			using var stream = File.OpenRead(path);
			var fingerprint = stream.SemanticHash();
			Assert.NotEqual(0UL, fingerprint);
		}

		[Theory]
		[InlineData("data/bookmark.txt")]
		[InlineData("../../../StreamExtensionsFacts.cs")]
		public void MatchesSimilarSemantics(string path)
		{
			var extension = Path.GetExtension(path);
			var duplicate = $"{Guid.NewGuid()}.{extension}";
			Helpers.CreateTextNearDuplicate(path, duplicate);
			using var original = File.OpenRead(path);
			using var nearDuplicate = File.OpenRead(duplicate);
			Assert.NotEqual(original.CryptographicHash(), nearDuplicate.CryptographicHash());
			var originalHash = original.VisualHash();
			var nearDuplicateHash = nearDuplicate.VisualHash();
			int distance = System.Numerics.BitOperations.PopCount(originalHash ^ nearDuplicateHash);
			Assert.True(distance <= 3);
		}

		[Theory]
		[InlineData("data/bookmark.txt", "data/bookmark.pdf")]
		[InlineData("data/bookmark.txt", "../../../StreamExtensionsFacts.cs")]
		public void DoesNotMatchDifferentSemantics(string imageA, string imageB)
		{
			var streamA = File.OpenRead(imageA);
			var streamB = File.OpenRead(imageB);
			var hashA = streamA.SemanticHash();
			var hashB = streamB.SemanticHash();
			int distance = System.Numerics.BitOperations.PopCount(hashA ^ hashB);
			Assert.True(distance >= 10);
		}

	}
	public class GetMimeTypeFacts: StreamExtensionsFacts
	{
		public GetMimeTypeFacts(StreamExtensionsFixture fixture) : base(fixture)
		{
		}

		[Theory]
		[InlineData("data/sample.477x640.jpg", "image/jpeg")]
		[InlineData("data/sample.jpeg", "image/jpeg")]
		[InlineData("data/sample.moving.gif", "image/gif")]
		[InlineData("data/speech.mp3", "audio/mpeg")]
		[InlineData("data/speech.wav", "audio/wav")]
		[InlineData("data/sample.flac", "audio/flac")]
		[InlineData("data/bookmark.pdf", "application/pdf")]
		public void IdentifiesMimeType(string path, string expectedMimeType)
		{
			using var stream = File.OpenRead(path);
			var mimeType = stream.GetMimeType();
			Assert.Equal(expectedMimeType, mimeType);
		}
	}
}

public class StreamExtensionsFixture: IAsyncLifetime
{
	public async Task InitializeAsync()
	{
		var storage = new StorageService();
		var samples = await storage.ListAsync("google.storage://nascacht-io-tests/");
		Directory.CreateDirectory("data");
		await Parallel.ForEachAsync(samples, async (blob, ct) =>
		{
			var localPath = Path.Combine("data/", blob.Name);
			if (!File.Exists(localPath))
			{
				using var stream = await storage.OpenReadAsync($"google.storage://nascacht-io-tests/{blob.FullPath}", ct);
				using var fileStream = File.Create(localPath);
				await stream.CopyToAsync(fileStream, ct);
			}
		});
	}
	public Task DisposeAsync()
	{
		// No cleanup required
		return Task.CompletedTask;
	}
}