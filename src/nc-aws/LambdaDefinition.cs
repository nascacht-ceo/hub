using NuGet.Versioning;

namespace nc.Aws;

/// <summary>
/// Represents the configuration and deployment details for an AWS Lambda function, including code location, runtime,
/// resource settings, and related metadata.
/// </summary>
/// <remarks>Use this class to define the properties required to deploy or manage an AWS Lambda function
/// programmatically. Properties such as S3Bucket, S3Key, ImageUri, and GetCodeStream specify the source of the function
/// code, while Runtime, MemorySizeMb, and TimeoutSeconds configure the execution environment. This type is typically
/// used in conjunction with Lambda deployment or management services.</remarks>
public class LambdaDefinition
{
	/// <summary>
	/// Initializes a new instance of the LambdaDefinition class.
	/// </summary>
	public LambdaDefinition()
	{ }

	/// <summary>
	/// Initializes a new instance of the LambdaDefinition class for the specified type and method name.
	/// </summary>
	/// <param name="type">The type that contains the method to be represented by this lambda definition. Cannot be null.</param>
	/// <param name="methodName">The name of the method to be associated with this lambda definition. Cannot be null or empty.</param>
	public LambdaDefinition(Type type, string methodName)
	{
		Name = $"{type.FullName}_{methodName}".ToLower().Replace(".", "-");
	}

	/// <summary>
	/// Gets or sets the name associated with the object.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets the semantic version associated with the object.
	/// </summary>
	public SemanticVersion Version { get; set; }

	/// <summary>
	/// Gets or sets the name of the Amazon S3 bucket to use for storage operations.
	/// </summary>
	public string S3Bucket { get; set; }

	/// <summary>
	/// Gets or sets the key (path) within the S3 bucket where the code package is stored.
	/// </summary>
	public string S3Key { get; set; }

	/// <summary>
	/// Gets or sets the runtime environment for the AWS Lambda function (e.g., "nodejs14.x", "python3.8", "dotnet6").
	/// </summary>
	public string Runtime { get; set; }

	/// <summary>
	/// Gets or sets the Amazon Resource Name (ARN) of the IAM role that the Lambda function assumes when it is executed.
	/// </summary>
	public string RoleArn { get; set; }

	/// <summary>
	/// Gets or sets the method handler for the Lambda function (e.g., "MyNamespace.MyClass::MyMethod").
	/// </summary>
	public string MethodHandler { get; set; }

	/// <summary>
	/// Gets or sets the amount of memory, in megabytes, allocated to the Lambda function.
	/// </summary>
	public int MemorySizeMb { get; set; } = 256;

	/// <summary>
	/// Gets or sets the maximum execution time, in seconds, for the Lambda function before it is terminated.
	/// </summary>
	public int TimeoutSeconds { get; set; } = 30;

	/// <summary>
	/// Gets or sets the Amazon Resource Name (ARN) of the AWS Lambda function.
	/// </summary>
	/// <example>arn:aws:lambda:{region}:{account}:function:my-function</example>
	public string? LambdaArn { get; set; }

	/// <summary>
	/// Elastic Container Registry (ECR) image URI for Lambda function deployment.
	/// </summary>
	public string? ImageUri { get; set; }

	/// <summary>
	/// Gets or sets a delegate that asynchronously provides a <see cref="Stream"/> containing code data.
	/// </summary>
	/// <remarks>Used by <see cref="LambdaService"/> to upload zip file contents to S3 for use by a Lamdba function.</remarks>
	public Func<Task<Stream>>? GetCodeStream { get; set; }


}