using Microsoft.Extensions.Logging;
using nc.Scaling;
using System;
using System.Collections.Generic;
using System.Text;

namespace nc.Ai.Tests;

public class GeminiBatchSample
{
	private readonly ILogger<TplPipeline> _logger;

	public GeminiBatchSample()
	{
		_logger = LoggerFactory.Create(builder =>
		{
			builder.AddConsole();
			builder.SetMinimumLevel(LogLevel.Debug);
		}).CreateLogger<TplPipeline>();

	}
	public async Task AnalyzeDocuments()
	{
		//var pipeline = new TplPipeline(logger: _logger)
		//	.From(GetOneMillionFilePathsAsync())
		//	.Transform<string, string>(async path => await UploadToGcsAsync(path))

		//	// Split 1M docs into chunks of 200k
		//	.Batch<string>(200000)

		//	.Transform<IEnumerable<string>, string>(async chunk => {
		//		// Build the JSONL manifest for THIS 200k chunk
		//		string manifestUri = await CreateAndUploadJsonl(chunk);
		//		return manifestUri;
		//	})

		//	.Act(async manifestUri => {
		//		// Submit 5 separate jobs in total
		//		await SubmitGeminiBatchJobAsync(manifestUri);
		//		_logger.LogInformation($"Batch Job submitted for manifest: {manifestUri}");
		//	});
	}
}
