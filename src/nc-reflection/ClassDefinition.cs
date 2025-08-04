using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace nc.Reflection;

/// <summary>
/// Defines a class that represents a database table, OpenApi schema, or similar structure.
/// </summary>
public class ClassDefinition : IValidateOptions<ClassDefinition>
{
    public ClassDefinition()
    {
        Properties = Enumerable.Empty<PropertyDefinition>();
        Interfaces = new();
    }

    /// <summary>
    /// Creates a new instance of <see cref="ClassDefinition"/> based on the provided <paramref name="type"/>.
    /// </summary>
    public ClassDefinition(Type type)
        : this()
    {
        BaseClass = type;
        Properties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => new PropertyDefinition
            {
                Name = p.Name,
                ClrType = p.PropertyType,
                DeclaringType = p.DeclaringType
            })
            .ToList();

        ClassName = new SafeString(type.Name);
        Solution = new SafeString(type.Namespace ?? "Anonymous");
        SourceType = "C#";
        SourceUri = type.FullName ?? "Unknown";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassDefinition"/> class using the type of the specified POCO
    /// (Plain Old CLR Object).
    /// </summary>
    /// <param name="poco">The object whose type will be used to define the class. Must not be <see langword="null"/>.</param>
    public ClassDefinition(object poco)
        : this(poco.GetType())
    { }

    /// <summary>
    /// Properties of the class, representing columns in a database table or fields in a schema.
    /// </summary>
    public IEnumerable<PropertyDefinition> Properties { get; set; }


    /// <summary>
    /// Interfaces that should be applied when emitting this class dynamically.
    /// </summary>
    public HashSet<Type> Interfaces { get; set; } 

    /// <summary>
    /// Namespace of the class, used for organization and avoiding naming conflicts.
    /// </summary>
    public SafeString Solution { get; set; } = default!;

    /// <summary>
    /// Name of the class, typically matching the table or schema name.
    /// </summary>
    public SafeString ClassName { get; set; } = default!;

    /// <summary>
    /// The base class which an emitted class inherits from, if any.
    /// </summary>
    public Type? BaseClass { get; set; }

    /// <summary>
    /// Defines the type of source this class represents, such as "DatabaseTable", "OpenApiSchema", etc.
    /// </summary>
    public string SourceType { get; set; } = default!;

    /// <summary>
    /// Uri or identifier of the source from which this class is derived, such as a database connection string or OpenApi URL.
    /// </summary>
    public string SourceUri { get; set; } = default!;

    public void Validate()
    {
        var definedProps = Properties?.ToDictionary(p => p.Name) ?? new();

        foreach (var interfaceType in Interfaces)
        {
            foreach (var interfaceProperty in interfaceType.GetProperties())
            {
                if (!definedProps.TryGetValue(interfaceProperty.Name, out var match))
                    throw new MissingMemberException(ClassName, interfaceProperty.Name);

                if (match.ClrType != interfaceProperty.PropertyType)
                    throw new ArgumentOutOfRangeException(interfaceProperty.Name, match.ClrType, $"Type mismatch on interface property '{interfaceProperty.Name}': expected {interfaceProperty.PropertyType}, found {match.ClrType}.");
            }
        }
    }
    /// <summary>
    /// Validates the specified options instance and returns the result of the validation.
    /// </summary>
    /// <remarks>This method invokes the <see cref="ClassDefinition.Validate"/> method on the provided
    /// <paramref name="options"/> instance. If an exception is thrown during validation, the exception message is
    /// included in the failure result.</remarks>
    /// <param name="name">The name of the options instance being validated. This parameter is optional and may be <see langword="null"/>.</param>
    /// <param name="options">The options instance to validate. This parameter cannot be <see langword="null"/>.</param>
    /// <returns>A <see cref="ValidateOptionsResult"/> indicating the success or failure of the validation. Returns <see
    /// cref="ValidateOptionsResult.Success"/> if the validation succeeds; otherwise, returns a failure result with the
    /// associated error message.</returns>

    public ValidateOptionsResult Validate(string? name, ClassDefinition options)
    {
        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}

/// <summary>
/// Represents a class definition for a specified type, providing metadata and functionality related to the type. This
/// generic version allows specifying the type at compile time.
/// </summary>
/// <typeparam name="T">The type for which the class definition is created. Must be a reference type.</typeparam>
public class ClassDefinition<T>: ClassDefinition where T : class
{
    public ClassDefinition()
        : base(typeof(T))
    { }
}

/// <summary>
/// Provides a mechanism to compare two <see cref="ClassDefinition"/> objects for equality based on their solution and
/// class name.
/// </summary>
/// <remarks>This comparer considers two <see cref="ClassDefinition"/> objects equal 
/// if their <c>Solution</c> and <c>ClassName</c> properties are equal. 
/// It is designed to be used in collections that require equality checks, 
/// such as dictionaries or hash sets.</remarks>
public class ClassDefinitionComparer : IEqualityComparer<ClassDefinition>
{
    /// <summary>
    /// Determines whether two <see cref="ClassDefinition"/> instances are equal based on their properties.
    /// </summary>
    /// <param name="x">The first <see cref="ClassDefinition"/> instance to compare, or <see langword="null"/>.</param>
    /// <param name="y">The second <see cref="ClassDefinition"/> instance to compare, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if both instances are equal or if they reference the same object; otherwise, <see langword="false"/>.</returns>
    public bool Equals(ClassDefinition? x, ClassDefinition? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return string.Equals(x.Solution, y.Solution)
            && string.Equals(x.ClassName, y.ClassName);
    }

    /// <summary>
    /// Generates a hash code for the specified <see cref="ClassDefinition"/> object.
    /// </summary>
    /// <remarks>The hash code is computed using the values of the <c>Solution</c> and <c>ClassName</c>
    /// properties of the <paramref name="obj"/> parameter,  with a case-insensitive comparison.</remarks>
    /// <param name="obj">The <see cref="ClassDefinition"/> object for which to generate the hash code. Cannot be <c>null</c>.</param>
    /// <returns>An integer hash code that represents the specified <see cref="ClassDefinition"/> object.</returns>
    public int GetHashCode(ClassDefinition obj)
    {
        var hash = new HashCode();

        hash.Add(obj.Solution.Value, StringComparer.OrdinalIgnoreCase);
        hash.Add(obj.ClassName.Value, StringComparer.OrdinalIgnoreCase);

        return hash.ToHashCode();
    }
}


