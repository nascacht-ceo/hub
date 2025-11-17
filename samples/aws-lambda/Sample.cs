using Amazon.Lambda.Core;


[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace Sample.Aws.Lambda;

public class SomePoco
{
	public string Name { get; set; }
}

public class OtherPoco
{
	public string Name { get; set; }
}

public class Sample
{
	// Lambda handler for transformation
	public async Task<OtherPoco> TransformHandler(SomePoco some, ILambdaContext context)
	{
		return await TransformAsync(some);
	}

	// Lambda handler for notification
	public async Task<string> NotifyHandler(OtherPoco input, ILambdaContext context)
	{
		return await NotifyAsync(input);
	}

	public async Task<OtherPoco> TransformAsync(SomePoco some)
	{
		// The guts of your original logic snippet:
		return await Task.FromResult(new OtherPoco()
		{
			Name = $"Other {some.Name}"
		});
	}

	public async Task<string> NotifyAsync(OtherPoco input)
	{
		string message = $"Successfully processed {input.Name}. Notification sent at {DateTime.Now}";
		return await Task.FromResult(message);
	}
}
