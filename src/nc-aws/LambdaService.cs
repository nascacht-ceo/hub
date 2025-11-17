using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.S3;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace nc.Aws;

public class LambdaService: nc.Cloud.IFunctionService<LambdaService>
{
	private readonly IAmazonLambda _lambdaClient;
	private readonly IAmazonS3 _s3Client;
	private readonly ILogger<LambdaService>? _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="LambdaService"/> class.
	/// </summary>
	/// <remarks>This constructor sets up the necessary dependencies for the <see cref="LambdaService"/> to  perform
	/// operations involving AWS Lambda and S3. Ensure that valid, initialized instances of  <see cref="IAmazonLambda"/>
	/// and <see cref="IAmazonS3"/> are provided.</remarks>
	/// <param name="lambdaClient">An instance of <see cref="IAmazonLambda"/> used to interact with AWS Lambda services.</param>
	/// <param name="s3Client">An instance of <see cref="IAmazonS3"/> used to interact with AWS S3 services.</param>
	public LambdaService(IAmazonLambda lambdaClient, IAmazonS3 s3Client, ILogger<LambdaService>? logger = null)
	{
		_lambdaClient = lambdaClient;
		_s3Client = s3Client;
		_logger = logger;
	}

	/// <summary>
	/// Deploys the specified Lambda function definition to AWS.
	/// </summary>
	/// <remarks>This method checks the current version of the Lambda function in AWS and performs one of the
	/// following actions: - Creates the function if it does not exist. - Updates the function if the version in the
	/// definition is greater than the current version. No action is taken if the current version matches the version in
	/// the definition.</remarks>
	/// <param name="definition">The definition of the Lambda function to deploy, including its configuration and version.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public async Task<LambdaDefinition> DeployAsync(LambdaDefinition definition, CancellationToken cancellationToken = default)
	{

		var version = await CurrentAsync(definition, cancellationToken);
		if (version == null)
			definition = await CreateAsync(definition, cancellationToken);
		else if (version < definition.Version)
			definition = await UpdateAsync(definition, cancellationToken);
		var functions = await _lambdaClient.ListFunctionsAsync(cancellationToken);

		for (int i = 0; i < 10; i++)
		{
			// 1. Get the current function configuration
			var getConfigRequest = new GetFunctionConfigurationRequest { FunctionName = definition.LambdaArn };
			var configResponse = await _lambdaClient.GetFunctionConfigurationAsync(getConfigRequest);

			// 2. Check the state
			if (configResponse.State == State.Active)
			{
				Console.WriteLine($"Function {definition.Name} is now Active.");
			}

			if (configResponse.State == State.Failed)
			{
				// If the state is Failed, throw the exception immediately
				throw new Exception($"Lambda deployment failed. State: {configResponse.State}. Reason: {configResponse.StateReason}");
			}

			// 3. Wait before trying again
			await Task.Delay(1000);
		}

		return definition;
	}

	/// <summary>
	/// Creates a new AWS Lambda function asynchronously based on the provided definition.
	/// </summary>
	/// <remarks>This method uploads the function code to the specified S3 bucket, creates the Lambda function using
	/// the AWS SDK, and tags the function with its version for tracking purposes. The function's configuration, such as
	/// memory size and timeout, is determined by the properties of the <paramref name="definition"/> parameter.</remarks>
	/// <param name="definition">The <see cref="LambdaDefinition"/> object containing the configuration details for the Lambda function, including
	/// its name, runtime, role, handler, and deployment package information.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
	/// <returns></returns>
	private async Task<LambdaDefinition> CreateAsync(LambdaDefinition definition, CancellationToken cancellationToken)
	{
		await UploadCodeAsync(definition, cancellationToken);

		using var activity = Tracing.Source.StartActivity($"{nameof(LambdaService)}.{nameof(CreateAsync)}", System.Diagnostics.ActivityKind.Client);
		activity?.SetTag("name", definition.Name);
		activity?.SetTag("version", definition.Version);

		var createRequest = new CreateFunctionRequest
		{
			FunctionName = definition.Name,
			Runtime = definition.Runtime,
			Role = definition.RoleArn,
			Handler = definition.MethodHandler,
			PackageType = PackageType.Image,
			Code = new FunctionCode
			{
				S3Bucket = definition.S3Bucket,
				S3Key = definition.S3Key,
				ImageUri = definition.ImageUri
			},
			MemorySize = definition.MemorySizeMb,
			Timeout = definition.TimeoutSeconds,
			Description = $"Version {definition.Version}",
			
		};

		var response = await _lambdaClient.CreateFunctionAsync(createRequest, cancellationToken);

		definition.LambdaArn = response.FunctionArn;
		// Tag the function with the version for tracking
		if (!string.IsNullOrEmpty(response.FunctionArn))
		{
			var tagRequest = new TagResourceRequest
			{
				Resource = response.FunctionArn,
				Tags = new Dictionary<string, string>
				{
					{ "Version", definition.Version.ToString() }
				}
			};
			await _lambdaClient.TagResourceAsync(tagRequest, cancellationToken);
		}
		_logger?.LogInformation("Created Lambda function {definition}.", definition);

		await AliasAsync(definition, response.RevisionId);
		return definition;
	}

	/// <summary>
	/// Updates an existing AWS Lambda function with the specified definition, including its code, configuration, and tags.
	/// </summary>
	/// <remarks>This method performs the following operations: <list type="bullet"> <item><description>Uploads the
	/// new function code to the specified S3 bucket and key.</description></item> <item><description>Updates the
	/// function's configuration, including its runtime, memory size, timeout, and role.</description></item>
	/// <item><description>Tags the function with the new version.</description></item> </list> The method uses AWS SDK
	/// clients to perform these updates and logs the operation upon completion.</remarks>
	/// <param name="definition">The <see cref="LambdaDefinition"/> object containing the details of the Lambda function to update, such as its
	/// name, version, S3 bucket and key for the code, runtime, memory size, timeout, and role ARN.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
	/// <returns></returns>
	public async Task<LambdaDefinition> UpdateAsync(LambdaDefinition definition, CancellationToken cancellationToken = default)
	{
		await UploadCodeAsync(definition, cancellationToken);
		using var activity = Tracing.Source.StartActivity($"{nameof(LambdaService)}.{nameof(UpdateAsync)}", System.Diagnostics.ActivityKind.Client);
		activity?.SetTag("name", definition.Name);
		activity?.SetTag("version", definition.Version);

		// Update function code
		var updateCodeRequest = new UpdateFunctionCodeRequest
		{
			FunctionName = definition.Name,
			S3Bucket = definition.S3Bucket,
			S3Key = definition.S3Key,
			Publish = true,
			ImageUri = definition.ImageUri
		};
		var updateFunctionResponse = await _lambdaClient.UpdateFunctionCodeAsync(updateCodeRequest, cancellationToken);
		definition.LambdaArn = updateFunctionResponse.FunctionArn;
		// Update function configuration
		var updateConfigRequest = new UpdateFunctionConfigurationRequest
		{
			FunctionName = definition.Name,
			Handler = definition.MethodHandler,
			MemorySize = definition.MemorySizeMb,
			Timeout = definition.TimeoutSeconds,
			Role = definition.RoleArn,
			Runtime = definition.Runtime,
			Description = $"Version {definition.Version}",
			
		};
		await _lambdaClient.UpdateFunctionConfigurationAsync(updateConfigRequest, cancellationToken);

		_logger?.LogInformation("Updated Lambda function {definition}.", definition);

		await AliasAsync(definition, updateFunctionResponse.RevisionId);

		return definition;
	}

	/// <summary>
	/// Uploads the code for the specified Lambda function to an S3 bucket.
	/// </summary>
	/// <remarks>This method uploads the code stream provided by the <see cref="LambdaDefinition.GetCodeStream"/>
	/// delegate to the specified S3 bucket and key. If <see cref="LambdaDefinition.GetCodeStream"/> is <c>null</c>, the
	/// method returns without performing any operation.</remarks>
	/// <param name="definition">The <see cref="LambdaDefinition"/> containing the details of the Lambda function, including the S3 bucket, S3 key,
	/// and a delegate to retrieve the code stream.</param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
	/// <returns></returns>
	private async Task UploadCodeAsync(LambdaDefinition definition, CancellationToken cancellationToken)
	{
		if (definition.GetCodeStream == null)
			return;
		using var activity = Tracing.Source.StartActivity($"{nameof(LambdaService)}.{nameof(UploadCodeAsync)}", System.Diagnostics.ActivityKind.Client);
		activity?.SetTag("name", definition.Name);
		activity?.SetTag("version", definition.Version);

		try
		{
			var headResponse = await _s3Client.HeadBucketAsync(new Amazon.S3.Model.HeadBucketRequest() { BucketName = definition.S3Bucket }, cancellationToken);
		} 
		catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
		{
			await _s3Client.PutBucketAsync(new Amazon.S3.Model.PutBucketRequest
			{
				BucketName = definition.S3Bucket
			}, cancellationToken);
		}
		using var stream = await definition.GetCodeStream!();
		await _s3Client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
		{
			BucketName = definition.S3Bucket,
			Key = definition.S3Key,
			InputStream = stream
		});
		return;
	}

	/// <summary>
	/// Retrieves the current semantic version of the specified AWS Lambda function, if available.
	/// </summary>
	/// <remarks>This method queries the tags associated with the specified Lambda function to determine its
	/// version.  The version is expected to be stored in a tag named "Version" and must conform to semantic versioning. If
	/// the function does not exist or the "Version" tag is missing or invalid, the method returns <see
	/// langword="null"/>.</remarks>
	/// <param name="definition">The definition of the Lambda function, including its Amazon Resource Name (ARN).</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
	/// <returns>The current semantic version of the Lambda function as specified by its "Version" tag,  or <see langword="null"/>
	/// if the tag is not present, the version is invalid, or the function does not exist.</returns>
	public async Task<SemanticVersion?> CurrentAsync(LambdaDefinition definition, CancellationToken cancellationToken = default)
	{
		using var activity = Tracing.Source.StartActivity($"{nameof(LambdaService)}.{nameof(CurrentAsync)}", System.Diagnostics.ActivityKind.Client);
		activity?.SetTag("name", definition.Name);
		activity?.SetTag("version", definition.Version);


		if (definition.LambdaArn == null)
		{
			var request = new GetFunctionRequest
			{
				FunctionName = definition.Name
			};
			try
			{
				var response = await _lambdaClient.GetFunctionAsync(request, cancellationToken);
				definition.LambdaArn = response.Configuration.FunctionArn;
			}
			catch(Amazon.Lambda.Model.ResourceNotFoundException)
			{
				return null;
			}
		}
		try
		{
			var tagsResponse = await _lambdaClient.ListTagsAsync(new ListTagsRequest
			{
				Resource = definition.LambdaArn
			}, cancellationToken);

			// Assume the version tag is named "Version"
			if (tagsResponse.Tags.TryGetValue("Version", out var versionTag))
			{
				if (SemanticVersion.TryParse(versionTag, out var deployedVersion))
				{
					return deployedVersion;
				}
			}
		} 
		catch (ResourceNotFoundException)
		{
			// Function does not exist
			return null;
		}

		return null;
	}

	public async Task<Stream> InvokeAsync(LambdaDefinition definition, MemoryStream payload)
	{
		// 1. Create the InvokeRequest
		var invokeRequest = new InvokeRequest
		{
			FunctionName = definition.LambdaArn,
			InvocationType = InvocationType.RequestResponse, // To wait for the result
			PayloadStream = payload
		};

		// 2. Invoke the function using the LocalStack client
		var response = await _lambdaClient.InvokeAsync(invokeRequest);

		// 3. Check for an error in the Lambda's response body
		if (response.FunctionError != null)
		{
			// If the Lambda execution itself failed (e.g., runtime error, unhandled exception)
			// The error details are often in the Payload stream.
			using var reader = new StreamReader(response.Payload);
			var errorDetails = await reader.ReadToEndAsync();

			throw new Exception($"Lambda invocation failed for {definition.LambdaArn}. Error: {errorDetails}");
		}

		// 4. Return the result payload stream
		// Note: The stream should be returned for the calling test to dispose of it.
		return response.Payload;
	}

	public async Task AliasAsync(LambdaDefinition definition, string? revisionId = null, string? alias = null)
	{
		alias ??= definition.Version.ToString().Replace(".", "_");
		using var activity = Tracing.Source.StartActivity($"{nameof(LambdaService)}.{nameof(AliasAsync)}", System.Diagnostics.ActivityKind.Client);
		activity?.SetTag("name", definition.Name);
		activity?.SetTag("version", definition.Version);

		var publishRequest = new PublishVersionRequest
		{
			FunctionName = definition.Name,
			Description = $"Publishing version {definition.Version}",
		};
		var publishResponse = await _lambdaClient.PublishVersionAsync(publishRequest);

		var request = new CreateAliasRequest
		{
			FunctionName = definition.Name,
			Name = alias,
			FunctionVersion = publishResponse.Version,
			Description = $"Alias for {alias} deployment.",
		};

		try
		{
			var response = await _lambdaClient.CreateAliasAsync(request);
		}
		catch (ResourceConflictException ex)
		{
			_logger?.LogError(ex, "Alias {alias} already exists. Use UpdateAliasAsync to change its configuration.", alias);
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "Error creating alias {alias}.", alias);
			Console.WriteLine($"Error creating alias: {ex.Message}");
			throw;
		}
	}
}
