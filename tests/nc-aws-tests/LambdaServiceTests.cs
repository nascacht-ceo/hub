using Amazon.CDK.AWS.S3;
using Amazon.Lambda;
using Amazon.S3;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace nc.Aws.Tests;

[Collection((nameof(AmazonFixture)))]
public class LambdaServiceTests: IAsyncLifetime
{
	private readonly AmazonFixture _fixture;
	private LambdaService _lambdaService;

	public LambdaServiceTests(AmazonFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task CurrentAsync_ReturnsNullForNonExistentFunction()
	{
		var definition = new LambdaDefinition
		{
			Name = "NonExistentFunction",
			Version = NuGet.Versioning.SemanticVersion.Parse("1.0.0"),
			LambdaArn = "arn:aws:lambda:us-east-1:000000000000:function:NonExistentFunction"
		};
		var version = await _lambdaService.CurrentAsync(definition);
		Assert.Null(version);
	}

	[Fact(Skip = "work in progress")]
	public async Task DeployAsync_CreatesSampleFunction()
	{
		var definition = new LambdaDefinition(typeof(Sample.Aws.Lambda.Sample), "Transform")
		{
			Version = SemanticVersion.Parse("1.0.0"),
			S3Bucket = "sample-bucket",
			S3Key = "aws-lambda.lambda.zip",
			Runtime = "dotnet8",
			RoleArn = "arn:aws:iam::000000000000:role/lambda-exec-role",
			MethodHandler = $"{typeof(Sample.Aws.Lambda.Sample).FullName}::TransformAsync",
			MemorySizeMb = 128,
			TimeoutSeconds = 30,
			ImageUri = "public.ecr.aws/lambda/dotnet:9-x86_64",
			GetCodeStream = () => Task.FromResult<Stream>(
				File.OpenRead("../../../../../samples/aws-lambda/aws-lambda.lambda.zip")
			)
		};

		// Deploy the function (creates or updates as needed)
		definition = await _lambdaService.DeployAsync(definition);

		// Verify the function now exists and is current
		var version = await _lambdaService.CurrentAsync(definition);
		Assert.NotNull(version);

		var inputPayload = new { Message = "Hello LocalStack" };
		var payloadJson = System.Text.Json.JsonSerializer.Serialize(inputPayload);
		var payloadStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(payloadJson));

		// 5. Invoke the Lambda function
		// Assuming _lambdaService has an InvokeAsync method that returns the output Stream
		using var resultStream = await _lambdaService.InvokeAsync(definition, payloadStream);

		// 6. Read and deserialize the result
		using var reader = new StreamReader(resultStream);
		var responseJson = await reader.ReadToEndAsync();

		// Assuming the Lambda returns a simple object with a 'Result' property
		// var output = System.Text.Json.JsonSerializer.Deserialize<SampleLambdaOutput>(responseJson);

	}

	public Task DisposeAsync() => Task.CompletedTask;

	public async Task InitializeAsync()
	{
		var lambdaClient = _fixture.Tenant.GetService<IAmazonLambda>();
		var s3Client = _fixture.Tenant.GetService<IAmazonS3>();
		var logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace)).CreateLogger<LambdaService>();
		_lambdaService = new LambdaService(lambdaClient, s3Client, logger);

	}
}
