using nc.Scaling;
using System.Threading.Tasks.Dataflow;

namespace nc.Scaling.Tests;

public class TplScalingServiceFacts
{
    [Fact]
    public async Task ScalesEnumerableSync()
    {
        var options = new ExecutionDataflowBlockOptions();
        var service = new TplScalingService();
        var inputs = Enumerable.Range(1, 5);
        var results = await service.ExecuteAsync(inputs, (i) => i * 2).ToListAsync();
        Assert.Equal(new[] { 2, 4, 6, 8, 10 }, results.OrderBy(x => x));
    }

	[Fact]
	public async Task ScalesEnumerableAsync()
	{
		var options = new ExecutionDataflowBlockOptions();
		var service = new TplScalingService();
		var inputs = Enumerable.Range(1, 5);
		var results = await service.ExecuteAsync(inputs, async (i) => {
			await Task.Delay(10);
			return i * 2;
		}).ToListAsync();
		Assert.Equal(new[] { 2, 4, 6, 8, 10 }, results.OrderBy(x => x));
	}

	[Fact]
	public async Task ScalesAsyncEnumerableSync()
	{
		var options = new ExecutionDataflowBlockOptions();
		var service = new TplScalingService();
		var inputs = Enumerable.Range(1, 5).ToAsyncEnumerable();
		var results = await service.ExecuteAsync(inputs, (i) => {
			return i * 2;
		}).ToListAsync();
		Assert.Equal(new[] { 2, 4, 6, 8, 10 }, results.OrderBy(x => x));
	}

	[Fact]
	public async Task ScalesAsyncEnumerableAsync()
	{
		var options = new ExecutionDataflowBlockOptions();
		var service = new TplScalingService();
		var inputs = Enumerable.Range(1, 5).ToAsyncEnumerable();
		var results = await service.ExecuteAsync(inputs, async (i) => {
			await Task.Delay(10);
			return i * 2;
		}).ToListAsync();
		Assert.Equal(new[] { 2, 4, 6, 8, 10 }, results.OrderBy(x => x));
	}
}
