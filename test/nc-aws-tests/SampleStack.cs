using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using System.Collections.Generic;

namespace nc.Aws.Tests;

public class SampleStack : Stack
{
	public SampleStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
	{
		var lambdaExecutionRole = new Role(this, "PluginLambdaExecutionRole", new RoleProps
		{
			AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
			ManagedPolicies = new List<IManagedPolicy>
				{
					ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole")
				}.ToArray()
		});

		// --- 2. Define Shared Build Options ---
		// This BundlingOptions object tells the CDK how to compile and package your C# code.
		// It runs 'dotnet lambda package' inside a Docker container provided by AWS.
		var bundlingOptions = new BundlingOptions
		{
			// Use the official .NET 8 Lambda image for building
			Image = DockerImage.FromRegistry("public.ecr.aws/lambda/dotnet:8"),

			// Define the commands to compile and create the deployment package (zip)
			Command = new string[]
			{
					"/bin/sh",
					"-c",
					"dotnet tool install -g Amazon.Lambda.Tools" +
					" && dotnet build" +
					" && dotnet lambda package --output-package /asset-output/function.zip"
			},
			User = "root",
			OutputType = BundlingOutput.ARCHIVED
		};

		// --- 3. Deploy the First Lambda Function (SampleTransform) ---
		new Function(this, "SampleTransformFunction", new FunctionProps
		{
			FunctionName = "Cdk-SampleTransformFunction",

			// Assumes your Lambda source code is located in the 'src/PluginLambda' directory 
			// relative to where the CDK is executed.
			Code = Code.FromAsset("../PluginLambda", new Amazon.CDK.AWS.S3.Assets.AssetOptions { Bundling = bundlingOptions }),

			Handler = "PluginLambda::PluginLambda.Function::FunctionHandler", // Assembly::Namespace.Class::Method
			Runtime = new Runtime("dotnet8"),
			MemorySize = 256,
			Timeout = Duration.Seconds(30),
			Role = lambdaExecutionRole // Assign the shared role
		});

		new Function(this, "SampleTransformFunction", new FunctionProps
		{
			FunctionName = "Cdk-SampleTransformFunction",

			// Assumes your Lambda source code is located in the 'src/PluginLambda' directory 
			// relative to where the CDK is executed.
			Code = Code.FromAsset("../PluginLambda", new Amazon.CDK.AWS.S3.Assets.AssetOptions { Bundling = bundlingOptions }),

			Handler = "PluginLambda::PluginLambda.Function::FunctionHandler", // Assembly::Namespace.Class::Method

			// FIX: Explicitly set the runtime using the string identifier, 
			// which is stable across CDK versions.
			Runtime = new Runtime("dotnet8"),

			MemorySize = 256,
			Timeout = Duration.Seconds(30),
			Role = lambdaExecutionRole // Assign the shared role
		});

		new CfnOutput(this, "TransformFunctionName", new CfnOutputProps { Value = "Cdk-SampleTransformFunction" });
		new CfnOutput(this, "NotificationFunctionName", new CfnOutputProps { Value = "Cdk-SendNotificationFunction" });

	}
}
