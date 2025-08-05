using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace nc.Reflection;

/// <summary>
/// Provides functionality for dynamically creating .NET types at runtime based on class definitions.
/// </summary>
/// <remarks>This service is designed to support scenarios where types need to be generated dynamically, such as
/// for runtime data modeling or dynamic proxy generation. It uses reflection emit to define types and their properties,
/// grouping them by solution namespaces.</remarks>
public class TypeService: ITypeService
{
    /// <summary>
    /// Track a solution and it's classes.
    /// </summary>
    private readonly ConcurrentDictionary<SafeString, ConcurrentDictionary<SafeString, ClassDefinition>> _solutions = new();
    private readonly ConcurrentDictionary<SafeString, Type> _types = new();

    /// <summary>
    /// Track <see cref="ModuleBuilder"/> by solution.
    /// </summary>
    private readonly ConcurrentDictionary<SafeString, ModuleBuilder> _moduleBuilders = new();
    private readonly IEnumerable<ITypeBuilderExtension> _extensions;
    private readonly ILogger<TypeService>? _logger;

    public TypeService(IEnumerable<ITypeBuilderExtension>? extensions = null, ILogger<TypeService>? logger = null)
    {
        _extensions = extensions ?? new List<ITypeBuilderExtension>();
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

        var classBuilder = new ClassBuilder(moduleBuilder, classDefinition);
        foreach (var extension in _extensions)
        {
            try
            {
                extension.Apply(classBuilder);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error building class with extension {ExtensionName}", extension.GetType().Name);
                throw;
            }
        }
        return classBuilder.CreateTypeInfo();
        //var tb = moduleBuilder.DefineType($"{classDefinition.Solution}.{classDefinition.ClassName}", TypeAttributes.Public | TypeAttributes.Class, classDefinition.BaseClass);
        //var propertyMap = new Dictionary<string, (PropertyBuilder property, FieldBuilder field, MethodBuilder getter, MethodBuilder setter)>();

        //// Add properties
        //foreach (var property in classDefinition.Properties.Where(p => p.DeclaringType == null))
        //{
        //    var field = tb.DefineField($"_{property.Name}", property.ClrType, FieldAttributes.Private);

        //    var propertyBuilder = tb.DefineProperty(property.Name, PropertyAttributes.HasDefault, property.ClrType, null);

        //    // Define the getter for the property
        //    var getter = tb.DefineMethod($"get_{property.Name}",
        //        MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
        //        property.ClrType, Type.EmptyTypes);
        //    var getIL = getter.GetILGenerator();
        //    getIL.Emit(OpCodes.Ldarg_0);
        //    getIL.Emit(OpCodes.Ldfld, field);
        //    getIL.Emit(OpCodes.Ret);

        //    // Define the setter for the property
        //    var setter = tb.DefineMethod($"set_{property.Name}",
        //        MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
        //        null, new[] { property.ClrType });
        //    var setIL = setter.GetILGenerator();
        //    setIL.Emit(OpCodes.Ldarg_0);
        //    setIL.Emit(OpCodes.Ldarg_1);
        //    setIL.Emit(OpCodes.Stfld, field);
        //    setIL.Emit(OpCodes.Ret);

        //    propertyBuilder.SetGetMethod(getter);
        //    propertyBuilder.SetSetMethod(setter);

        //    propertyMap[property.Name] = (propertyBuilder, field, getter, setter);
        //}

        //// Add interfaces
        //foreach (var interfaceType in classDefinition.Interfaces)
        //{
        //    tb.AddInterfaceImplementation(interfaceType);

        //    foreach (var interfaceProp in interfaceType.GetProperties())
        //    {
        //        if (!propertyMap.TryGetValue(interfaceProp.Name, out var memberInfo))
        //            throw new InvalidOperationException($"Property '{interfaceProp.Name}' required by interface '{interfaceType.FullName}' is not defined in ClassDefinition.");

        //        if (interfaceProp.GetGetMethod() is MethodInfo interfaceGetter)
        //            tb.DefineMethodOverride(memberInfo.getter, interfaceGetter);

        //        if (interfaceProp.GetSetMethod() is MethodInfo interfaceSetter)
        //            tb.DefineMethodOverride(memberInfo.setter, interfaceSetter);
        //    }
        //}

        //return tb.CreateTypeInfo()!;
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
        _solutions
            .GetOrAdd(classDefinition.Solution, _ => new ConcurrentDictionary<SafeString, ClassDefinition>())
            .GetOrAdd(classDefinition.ClassName, classDefinition);
        return _types.GetOrAdd(classDefinition.FullName, _ => BuildType(classDefinition));
    }

    public IEnumerable<Type> GetTypes() => _types.Values;
    public IEnumerable<Type> GetTypes(SafeString solution) => _types.Where(t => t.Key.Value.StartsWith($"{solution}.")).Select(t => t.Value);

    public IEnumerable<string> GetSolutions() => _solutions.Keys.Select(s => s.Value);

    public Type? GetType(string solution, string className)
    {
        return _types.GetValueOrDefault($"{solution}.{className}");
    }

    public ClassDefinition? GetClassDefinition(Type type)
    {
        var parts = type.FullName?.Split('.');
        var solution = parts?.Length > 1 ? parts[0] : null;
        var className = parts?.Length > 1 ? string.Join('.', parts.Skip(1)) : type.FullName;
        if (solution is null || className is null)
            return null;
        if (_solutions.TryGetValue(solution, out var classes))
        {
            if (classes.TryGetValue(className, out var classDef))
                return classDef;
        }
        return null;
    }

    public IEnumerable<ClassDefinition> GetClassDefinitions(SafeString solution)
    {
        if (_solutions.TryGetValue(solution, out var classes))
            return classes.Values;
        return Enumerable.Empty<ClassDefinition>();
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
