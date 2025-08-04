using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace nc.Reflection;

/// <summary>
/// Provides functionality for dynamically creating .NET types at runtime based on class definitions.
/// </summary>
/// <remarks>This service is designed to support scenarios where types need to be generated dynamically, such as
/// for runtime data modeling or dynamic proxy generation. It uses reflection emit to define types and their properties,
/// grouping them by solution namespaces.</remarks>
public class TypeService
{
    /// <summary>
    /// Track a solution and it's classes.
    /// </summary>
    private readonly ConcurrentDictionary<SafeString, ConcurrentDictionary<SafeString, ClassDefinition>> _solutions = new();

    /// <summary>
    /// Track <see cref="ModuleBuilder"/> by solution.
    /// </summary>
    private readonly ConcurrentDictionary<SafeString, ModuleBuilder> _moduleBuilders = new();
    private readonly ILogger<TypeService>? _logger;

    public TypeService(ILogger<TypeService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a <see cref="ModuleBuilder"/> for the given solution namespace.
    /// </summary>
    /// <param name="solution">Name of the solution this class belongs to.</param>
    /// <returns><see cref="ModuleBuilder"/> uses to create types with a namespace of <paramref name="solution"/></returns>
    private ModuleBuilder GetModuleBuilder(SafeString solution)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solution);
        return _moduleBuilders.GetOrAdd(solution, ns =>
        {
            using var activity = Tracing.Source.StartActivity("TypeService.GetModuleBuilder", System.Diagnostics.ActivityKind.Internal);
            activity?.SetTag("solution", solution.Value);
            _logger?.LogTrace("Creating module builder for solution: {solution}", solution);

            var assemblyName = new AssemblyName(ns);
            var builder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            return builder.DefineDynamicModule(ns);
        });
    }

    /// <summary>
    /// Dynamically builds a .NET type based on the provided class definition.
    /// </summary>
    /// <remarks>This method uses reflection emit to dynamically create a class with public properties based
    /// on the provided <paramref name="classDefinition"/>. Each property includes a private backing field, a getter,
    /// and a setter.</remarks>
    /// <param name="classDefinition">The definition of the class, including its properties and their types. Cannot be null.</param>
    /// <returns>A <see cref="Type"/> representing the dynamically created class.</returns>
    private Type BuildType(ClassDefinition classDefinition)
    {
        if (classDefinition is null)
            throw new ArgumentNullException(nameof(classDefinition), "Class definition cannot be null.");

        classDefinition.Validate();

        using var activity = Tracing.Source.StartActivity("TypeService.BuildType", System.Diagnostics.ActivityKind.Internal);
        activity?.SetTag("Solution", classDefinition.Solution);
        activity?.SetTag("ClassName", classDefinition.ClassName);

        var moduleBuilder = GetModuleBuilder(classDefinition.Solution);
        var tb = moduleBuilder.DefineType(classDefinition.ClassName, TypeAttributes.Public | TypeAttributes.Class, classDefinition.BaseClass);
        var propertyMap = new Dictionary<string, (FieldBuilder field, MethodBuilder getter, MethodBuilder setter)>();

        foreach (var property in classDefinition.Properties.Where(p => p.DeclaringType == null))
        {
            var field = tb.DefineField($"_{property.Name}", property.ClrType, FieldAttributes.Private);

            var propertyInfo = tb.DefineProperty(property.Name, PropertyAttributes.HasDefault, property.ClrType, null);

            var getter = tb.DefineMethod($"get_{property.Name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                property.ClrType, Type.EmptyTypes);
            var getIL = getter.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, field);
            getIL.Emit(OpCodes.Ret);

            var setter = tb.DefineMethod($"set_{property.Name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                null, new[] { property.ClrType });
            var setIL = setter.GetILGenerator();
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, field);
            setIL.Emit(OpCodes.Ret);

            propertyInfo.SetGetMethod(getter);
            propertyInfo.SetSetMethod(setter);

            propertyMap[property.Name] = (field, getter, setter);
        }

        foreach (var interfaceType in classDefinition.Interfaces)
        {
            tb.AddInterfaceImplementation(interfaceType);

            foreach (var interfaceProp in interfaceType.GetProperties())
            {
                if (!propertyMap.TryGetValue(interfaceProp.Name, out var memberInfo))
                {
                    throw new InvalidOperationException($"Property '{interfaceProp.Name}' required by interface '{interfaceType.FullName}' is not defined in ClassDefinition.");
                }

                if (interfaceProp.GetGetMethod() is MethodInfo interfaceGetter)
                {
                    tb.DefineMethodOverride(memberInfo.getter, interfaceGetter);
                }

                if (interfaceProp.GetSetMethod() is MethodInfo interfaceSetter)
                {
                    tb.DefineMethodOverride(memberInfo.setter, interfaceSetter);
                }
            }
        }

        return tb.CreateTypeInfo()!;
    }

    /// <summary>
    /// Retrieves the <see cref="Type"/> associated with the given class definition, creating it if it doesn't exist.
    /// </summary>
    /// <param name="classDefinition">The definition of the class whose type is to be retrieved. Cannot be null.</param>
    /// <returns>A <see cref="Type"/> representing the class.</returns>
    public Type GetClass(ClassDefinition classDefinition)
    {
        if (classDefinition is null)
            throw new ArgumentNullException(nameof(classDefinition), "Class definition cannot be null.");

        var solution = classDefinition.Solution;
        var className = classDefinition.ClassName;

        // Get or create the solution dictionary
        var classes = _solutions.GetOrAdd(solution, _ => new ConcurrentDictionary<SafeString, ClassDefinition>());

        // Check if the class definition already exists
        if (classes.TryGetValue(className, out var existingDefinition))
        {
            // If the definition matches, return the type
            // Otherwise, rebuild the type (optional: you may want to compare definitions for changes)
            return Type.GetType($"{solution.Value}.{className.Value}") 
                ?? BuildType(existingDefinition);
        }
        else
        {
            // Add the new class definition and build the type
            classes[className] = classDefinition;
            return BuildType(classDefinition);
        }
    }


    //public void AddDatabase(string connectionString, string tablePattern)
    //{
    //    using var conn = new SqlConnection(connectionString);
    //    conn.Open();

    //    var cmd = conn.CreateCommand();
    //    cmd.CommandText = @"
    //        SELECT TABLE_NAME 
    //        FROM INFORMATION_SCHEMA.TABLES 
    //        WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME LIKE @pattern";
    //    cmd.Parameters.AddWithValue("@pattern", tablePattern);

    //    using var reader = cmd.ExecuteReader();
    //    var tableNames = new List<string>();

    //    while (reader.Read())
    //        tableNames.Add(reader.GetString(0));

    //    foreach (var tableName in tableNames)
    //    {
    //        if (_types.ContainsKey(tableName))
    //            continue;

    //        var columns = GetColumnsForTable(conn, tableName);
    //        var type = BuildType(tableName, columns);
    //        lock (_lock)
    //        {
    //            _types[tableName] = type;
    //        }
    //    }
    //}

    //private List<(string Name, Type ClrType)> GetColumnsForTable(SqlConnection conn, string tableName)
    //{
    //    var cmd = conn.CreateCommand();
    //    cmd.CommandText = @"
    //        SELECT COLUMN_NAME, DATA_TYPE
    //        FROM INFORMATION_SCHEMA.COLUMNS 
    //        WHERE TABLE_NAME = @table";
    //    cmd.Parameters.AddWithValue("@table", tableName);

    //    var columns = new List<(string, Type)>();
    //    using var reader = cmd.ExecuteReader();
    //    while (reader.Read())
    //    {
    //        var columnName = reader.GetString(0);
    //        var sqlType = reader.GetString(1);
    //        var clrType = SqlTypeToClrType(sqlType);
    //        columns.Add((columnName, clrType));
    //    }

    //    return columns;
    //}

    //private Type SqlTypeToClrType(string sqlType) => sqlType switch
    //{
    //    "int" => typeof(int),
    //    "bigint" => typeof(long),
    //    "nvarchar" => typeof(string),
    //    "varchar" => typeof(string),
    //    "datetime" => typeof(DateTime),
    //    "bit" => typeof(bool),
    //    "decimal" => typeof(decimal),
    //    "float" => typeof(double),
    //    _ => typeof(string), // fallback
    //};

}
