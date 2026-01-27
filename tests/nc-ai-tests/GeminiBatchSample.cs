using Google.Cloud.AIPlatform.V1;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using nc.Scaling;
using nc.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

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

	private const string ProjectId = "seventh-seeker-476512-r1";
	private const string Location = "us-central1";
	private const string BucketName = "nascacht-ai-tests";
	private const string ModelId = "gemini-2.5-flash";

	[Fact(Skip ="wip")]
	public async Task SubmitBatchJob_ForTwoDocuments_Succeeds()
	{
		// 1. Initialize Clients
		var config = new ConfigurationBuilder()
			.AddUserSecrets("nc-hub")
			.AddEnvironmentVariables("nc_hub__")
			.Build();

		var options = new StorageServiceOptions();
		config.GetSection("nc_ai_tests:storage").Bind(options);

		var storage = new StorageService(options);
		var jobClient = new JobServiceClientBuilder
		{
			Endpoint = $"{Location}-aiplatform.googleapis.com"
		}.Build();

		// 2. Upload PDFs to GCS
		string[] localFiles = { "doc1.pdf", "doc2.pdf" };
		var gcsUris = new List<string>() { $"gs://{BucketName}/compound-a.pdf", $"gs://{BucketName}/compound-b.pdf" };

		//foreach (var file in localFiles)
		//{
		//	using var stream = File.OpenRead(file);
		//	var obj = await storageClient.UploadObjectAsync(BucketName, $"inputs/{file}", "application/pdf", stream);
		//	gcsUris.Add($"gs://{BucketName}/{obj.Name}");
		//}

		// 3. Create JSONL Manifest (Implicit Caching: Same System Instruction per line)
		var systemInstruction = "Templates: { 'Deed': 'Property transfer' }. Labels: ['Signed', 'Recorded']. Output JSON.";
		var jsonLines = new StringBuilder();

		foreach (var uri in gcsUris)
		{
			var request = new
			{
				system_instruction = new { parts = new[] { new { text = systemInstruction } } },
				contents = new[] {
					new {
						role = "user",
						parts = new object[] {
							new { file_data = new { mime_type = "application/pdf", file_uri = uri } }
						}
					}
				}
			};
			jsonLines.AppendLine(JsonSerializer.Serialize(request));
		}

		// 4. Upload Manifest to GCS
		var manifestName = "manifests/test_batch.jsonl";
		//await storageClient.UploadObjectAsync(BucketName, manifestName, "application/jsonl",
		//	new MemoryStream(Encoding.UTF8.GetBytes(jsonLines.ToString())));
		string manifestUri = $"gs://{BucketName}/{manifestName}";

		// 5. Submit Batch Job
		var batchJob = new BatchPredictionJob
		{
			DisplayName = "XUnit_Test_Batch",
			Model = $"projects/{ProjectId}/locations/{Location}/publishers/google/models/{ModelId}",
			InputConfig = new BatchPredictionJob.Types.InputConfig
			{
				InstancesFormat = "jsonl",
				GcsSource = new GcsSource { Uris = { manifestUri } }
			},
			OutputConfig = new BatchPredictionJob.Types.OutputConfig
			{
				PredictionsFormat = "jsonl",
				GcsDestination = new GcsDestination { OutputUriPrefix = $"gs://{BucketName}/results/" }
			}
		};

		//var result = await jobClient.CreateBatchPredictionJobAsync(new LocationName(ProjectId, Location), batchJob);

		// Assert
		//Assert.NotNull(result.Name);
		//Assert.Equal(JobState.JobStatePending, result.State);
	}

}
