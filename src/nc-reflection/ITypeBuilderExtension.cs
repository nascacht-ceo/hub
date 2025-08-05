using System.Reflection.Emit;

namespace nc.Reflection;

/// <summary>
/// Hook for extending the type building process.
/// </summary>
public interface ITypeBuilderExtension
{
    /// <summary>
    /// Modify the <paramref name="classBuilder"/>.
    /// </summary>
    /// <param name="classBuilder"></param>
    void Apply(ClassBuilder classBuilder);
}