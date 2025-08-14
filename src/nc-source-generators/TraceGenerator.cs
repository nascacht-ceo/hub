using Microsoft.CodeAnalysis;
using System.Linq;

namespace nc.SourceGenerators;

// Specify supported languages to avoid RS1041 when targeting .NET 9.0
[Generator]
public class TraceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// 1. Create a provider for the compilation's AssemblyName
		IncrementalValueProvider<string> assemblyNameProvider =
			context.CompilationProvider
				.Select((compilation, cancellationToken) => compilation.AssemblyName ?? string.Empty);

		// 2. Register a source output using the AssemblyNameProvider
		context.RegisterSourceOutput(assemblyNameProvider, (productionContext, assemblyName) =>
		{
			var source = $$"""
namespace nc;

public static class Tracing
{
    public static readonly System.Diagnostics.ActivitySource Source = 
        new("{{assemblyName}}");
}
""";
			productionContext.AddSource("Tracing.g.cs", source);
		});
	}
}