using NuGet.Versioning;

namespace nc.Aws;

public class LambdaDefinition
{
	public LambdaDefinition()
	{ }

	public LambdaDefinition(Type type, string methodName)
	{
		Name = $"{type.FullName}_{methodName}".ToLower().Replace(".", "-");
	}
	public string Name { get; set; }

	public SemanticVersion Version { get; set; }

	public string S3Bucket { get; set; }

	public string S3Key { get; set; }

	public string Runtime { get; set; }

	public string RoleArn { get; set; }

	public string MethodHandler { get; set; } 

	public int MemorySizeMb { get; set; } = 256;

	public int TimeoutSeconds { get; set; } = 30;

	/// <summary>
	/// Gets or sets the Amazon Resource Name (ARN) of the AWS Lambda function.
	/// </summary>
	/// <example>arn:aws:lambda:{region}:{account}:function:my-function</example>
	public string? LambdaArn { get; set; }

	/// <summary>
	/// Gets or sets a delegate that asynchronously provides a <see cref="Stream"/> containing code data.
	/// </summary>
	/// <remarks>Used by <see cref="LambdaService"/> to upload zip file contents to S3 for use by a Lamdba function.</remarks>
	public Func<Task<Stream>>? GetCodeStream { get; set; }


}