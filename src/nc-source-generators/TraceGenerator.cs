using Microsoft.CodeAnalysis;

namespace nc.SourceGenerators;

/// <summary>
/// Generates a source file containing a static class with a <see cref="System.Diagnostics.ActivitySource"/>  for
/// tracing, using the assembly name of the compilation as the source name.
/// </summary>
/// <remarks>This generator creates a static class named <c>Tracing</c> in the <c>nc</c> namespace.  The class
/// contains a single static readonly field, <c>Source</c>, which is an instance of  <see
/// cref="System.Diagnostics.ActivitySource"/> initialized with the assembly name of the compilation.  The generated
/// source file is named <c>Tracing.g.cs</c>.</remarks>
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