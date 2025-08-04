using Microsoft.CodeAnalysis;

namespace nc.SourceGenerators;

[Generator]
public class TraceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        var assemblyName = context.Compilation.AssemblyName;
        var source = $$"""
namespace nc;

public static class Tracing
{
    public static readonly System.Diagnostics.ActivitySource Source = 
        new("{{assemblyName}}");
}
""";
        context.AddSource("Tracing.g.cs", source);
    }
}

