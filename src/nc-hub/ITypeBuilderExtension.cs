using System.Reflection.Emit;

namespace nc.Hub;

/// <summary>
/// Hook for extending the type building process.
/// </summary>
public interface ITypeBuilderExtension
{
    /// <summary>
    /// Modify the <paramref name="classBuilder"/>.
    /// </summary>
    /// <param name="classBuilder"></param>
    void Apply(ModelBuilder classBuilder);
}