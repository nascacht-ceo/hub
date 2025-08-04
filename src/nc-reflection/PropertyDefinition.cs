using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Reflection;

/// <summary>
/// Defines a property of a class, representing a column in a database table or a field in an OpenApi schema.
/// </summary>
public class PropertyDefinition
{
    /// <summary>
    /// Name of the property, typically matching the column name in a database or field name in a schema.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets the CLR type associated with this property.
    /// </summary>
    public Type ClrType { get; set; } = default!;

    /// <summary>
    /// Description of the property, providing additional context or information about its purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the type that declares the current member.
    /// </summary>
    public Type? DeclaringType { get; set; }

    /// <summary>
    /// Represents whether this property should be used for a Status behavior.
    /// </summary>
    public bool IsStatus { get; set; }

    /// <summary>
    /// Represents whether this property is a key field, such as a primary key in a database.
    /// </summary>
    public bool IsKey { get; set; }
}
