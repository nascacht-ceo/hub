using System.Reflection;
using System.Reflection.Emit;

namespace nc.Hub;

/// <summary>
/// Provides functionality to dynamically build a .NET type based on a specified model definition.
/// </summary>
/// <remarks>The <see cref="ModelBuilder"/> class is used to construct a runtime type by defining its properties,
/// interfaces, and other characteristics based on the provided <see cref="ModelDefinition"/>. This is typically used in
/// scenarios where types need to be generated dynamically, such as in code generation or runtime type creation
/// frameworks.</remarks>
/// <param name="moduleBuilder"></param>
/// <param name="modelDefinition"></param>
public class ModelBuilder(ModuleBuilder moduleBuilder, ModelDefinition modelDefinition)
{
	public readonly ModuleBuilder ModuleBuilder = moduleBuilder;
	public readonly ModelDefinition ModelDefinition = modelDefinition;
	public readonly TypeBuilder TypeBuilder = moduleBuilder.DefineType($"{modelDefinition.FullName}", TypeAttributes.Public | TypeAttributes.Class, modelDefinition.BaseClass);
	public readonly IDictionary<string, ModelProperty> PropertyMap = new Dictionary<string, ModelProperty>();

	public Type CreateTypeInfo()
	{
		BuildProperties();
		BuildInterfaces();
		return TypeBuilder.CreateTypeInfo()!;
	}

	public void BuildProperties()
	{
		foreach (var property in ModelDefinition.Properties.Where(p => p.DeclaringType == null))
		{
			var classProperty = new ModelProperty(property, TypeBuilder);
			PropertyMap[property.Name] = classProperty;
		}
	}

	public void BuildInterfaces()
	{
		foreach (var interfaceType in ModelDefinition.Interfaces)
		{
			TypeBuilder.AddInterfaceImplementation(interfaceType);

			foreach (var interfaceProp in interfaceType.GetProperties())
			{
				if (!PropertyMap.TryGetValue(interfaceProp.Name, out var classProperty))
					throw new InvalidOperationException($"Property '{interfaceProp.Name}' required by interface '{interfaceType.FullName}' is not defined in ClassDefinition.");

				if (interfaceProp.GetGetMethod() is MethodInfo interfaceGetter)
					TypeBuilder.DefineMethodOverride(classProperty.Getter, interfaceGetter);

				if (interfaceProp.GetSetMethod() is MethodInfo interfaceSetter)
					TypeBuilder.DefineMethodOverride(classProperty.Setter, interfaceSetter);
			}
		}
	}
}
