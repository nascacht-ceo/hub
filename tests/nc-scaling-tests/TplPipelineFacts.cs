using Microsoft.Extensions.Logging;

namespace nc.Scaling.Tests
{
	public class TplPipelineFacts
	{
		private readonly ILogger<TplPipeline> _logger;

		public TplPipelineFacts()
		{
			_logger = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Debug);
			}).CreateLogger<TplPipeline>();
		}

		[Fact]
		public async Task WorksWithNoBlocks()
		{
			var inputs = Enumerable.Range(1, 5).ToAsyncEnumerable();
			var results = await new TplPipeline(logger: _logger)
				.From(inputs)
				.ExecuteAsync<int>()
				.ToListAsync();
			Assert.Equal([1, 2, 3, 4, 5], results.OrderBy(x => x));
		}

		[Fact]
		public async Task Transforms()
		{
			var inputs = Enumerable.Range(1, 5).ToAsyncEnumerable();
			var results = await new TplPipeline(logger: _logger)
				.From(inputs)
				.Transform<int, int>(async i => await Task.FromResult(i * 2))
				.ExecuteAsync<int>()
				.ToListAsync();
			Assert.Equal([2, 4, 6, 8, 10], results.OrderBy(x => x));
		}

		[Fact]
		public void TransformThrowsOnMismatchTypes()
		{
			var inputs = Enumerable.Range(1, 5).ToAsyncEnumerable();
			Assert.Throws<ArgumentOutOfRangeException>(() =>
			{
				new TplPipeline(logger: _logger)
					.From(inputs)
					.Transform<string, int>(async i => await Task.FromResult(1));
			});
		}

		[Fact]
		public async Task TransformsMany()
		{
			var inputs = Enumerable.Range(1, 5).ToAsyncEnumerable();
			var results = await new TplPipeline(logger: _logger)
				.From(inputs)
				.TransformMany<int, int>(i => GetTransformedMany(i))
				.ExecuteAsync<int>()
				.ToListAsync();
			Assert.Equal(new[] { 1, 2, 2, 3, 4, 4, 5, 6, 8, 10 }, results.OrderBy(x => x));
		}

		[Fact]
		public async Task Filter()
		{
			var inputs = Enumerable.Range(1, 5).ToAsyncEnumerable();
			var results = await new TplPipeline(logger: _logger)
				.From(inputs)
				.Filter<int>(i => (i % 2 == 0))
				.ExecuteAsync<int>()
				.ToListAsync();
			Assert.Equal([2, 4], results.OrderBy(x => x));
		}

		[Fact]
		public async Task RemovesDuplicates()
		{
			var inputs = Enumerable.Range(1, 5).ToAsyncEnumerable();
			var results = await new TplPipeline(logger: _logger)
				.From(inputs)
				.TransformMany<int, int>(i => GetTransformedMany(i))
				.RemoveDuplicates<int>()
				.ExecuteAsync<int>()
				.ToListAsync();
			Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 8, 10 }, results.OrderBy(x => x));
		}

		[Fact]
		public async Task Batches()
		{
			var inputs = Enumerable.Range(1, 9).ToAsyncEnumerable();
			var results = await new TplPipeline(logger: _logger)
				.From(inputs)
				.Batch<int>(3)
				.ExecuteAsync<IEnumerable<int>>()
				.ToListAsync();
			Assert.Equal(3, results.Count);
			Assert.Equal([1, 2, 3], results[0].OrderBy(x => x));
			Assert.Equal([4, 5, 6], results[1].OrderBy(x => x));
			Assert.Equal([7, 8, 9], results[2].OrderBy(x => x));
		}

		[Fact]
		public async Task Joins()
		{
			var inputsA = Enumerable.Range(1, 5).ToAsyncEnumerable();
			var inputsB = Enumerable.Range(6, 5).ToAsyncEnumerable();
			var results = await new TplPipeline(logger: _logger)
				.From(inputsA)
				.From(inputsB)
				.Join<int, int>()
				.ExecuteAsync<Tuple<int, int>>()
				.ToListAsync();
			Assert.Equal(5, results.Count);
			Assert.Contains(new Tuple<int, int>(1, 6), results);
		}

		[Fact]
		public async Task Act()
		{
			var total = 0;
			var inputs = Enumerable.Range(1, 5).ToAsyncEnumerable();
			var results = await new TplPipeline(logger: _logger)
				.From(inputs)
				.Act<int>(i => {
					total += i;
					return Task.CompletedTask;
				})
				.ExecuteAsync<int>()
				.ToListAsync();
			Assert.Equal(15, total);
		}

		private async IAsyncEnumerable<int> GetTransformedMany(int i)
		{
			yield return i;
			yield return i * 2;
			await Task.CompletedTask;
		}
	}
}
