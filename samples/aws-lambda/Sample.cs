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
	public async Task<OtherPoco> TransformAsync(SomePoco some)
	{
		// The guts of your original logic snippet:
		return await Task.FromResult(new OtherPoco()
		{
			Name = $"Other {some.Name}"
		});
	}

	public async Task<string> NotifyAsync(OtherPoco input, ILambdaContext context)
	{
		context.Logger.LogLine($"Notification triggered for item: {input.Name}");

		// Simulation of sending an email or message
		string message = $"Successfully processed {input.Name}. Notification sent at {DateTime.Now}";

		context.Logger.LogLine(message);

		return await Task.FromResult(message);
	}
}
