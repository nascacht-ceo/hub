using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace nc.Ai.Tests;

public class AiContextCacheFacts
{
	private readonly IAiContextCache _cache;

	public AiContextCacheFacts()
	{
		var config = new ConfigurationBuilder().AddJsonFile("tests.json").Build();
		var services = new ServiceCollection()
			.Configure<AiContextCacheOptions>(config)
			.AddSingleton<IDistributedCache, MemoryDistributedCache>()
			.AddSingleton<IAiContextCache, AiContextCache>()
			.BuildServiceProvider();
		_cache = services.GetRequiredService<IAiContextCache>();
	}

	public class GetContextAsync : AiContextCacheFacts
	{
		[Fact]
		public async Task ThrowsWhenKeyNotFound()
		{
			await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.GetContextAsync("missing-key"));
		}

		[Fact]
		public async Task ReturnsStoredContent()
		{
			var content = new TextContent("hello world");
			await _cache.SetContextAsync("key1", content);

			var result = await _cache.GetContextAsync("key1");

			var text = Assert.IsType<TextContent>(result);
			Assert.Equal("hello world", text.Text);
		}
	}

	public class SetContextAsync : AiContextCacheFacts
	{
		[Fact]
		public async Task StoresContentRetrievableByGet()
		{
			var content = new TextContent("stored content");

			await _cache.SetContextAsync("key1", content);

			await _cache.GetContextAsync("key1"); // does not throw
		}
	}
}

public class AiContextCacheOptionsFacts
{
	public class EntryOptionsProperty : AiContextCacheOptionsFacts
	{
		[Fact]
		public void DefaultDurationIsFiveMinutes()
		{
			var opts = new AiContextCacheOptions();

			Assert.Equal(TimeSpan.FromMinutes(5), opts.EntryOptions.AbsoluteExpirationRelativeToNow);
		}

		[Fact]
		public void ReflectsCacheDurationMinutes()
		{
			var opts = new AiContextCacheOptions { CacheDurationMinutes = 30 };

			Assert.Equal(TimeSpan.FromMinutes(30), opts.EntryOptions.AbsoluteExpirationRelativeToNow);
		}

		[Fact]
		public void ReturnsSameInstance()
		{
			var opts = new AiContextCacheOptions();

			Assert.Same(opts.EntryOptions, opts.EntryOptions);
		}
	}
}
