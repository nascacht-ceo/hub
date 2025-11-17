using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Sample.Aws.Lambda;

await LambdaBootstrapBuilder.Create<SomePoco, OtherPoco>(
        new Sample.Aws.Lambda.Sample().TransformHandler,
        new DefaultLambdaJsonSerializer()
    )
    .Build()
    .RunAsync();