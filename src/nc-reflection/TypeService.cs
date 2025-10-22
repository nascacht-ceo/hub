using Microsoft.Extensions.Logging;
using nc.Hub;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace nc.Reflection;

/// <summary>
/// Provides functionality for dynamically creating .NET types at runtime based on model definitions.
/// </summary>
/// <remarks>This service is designed to support scenarios where types need to be generated dynamically, such as
/// for runtime data modeling or dynamic proxy generation. It uses reflection emit to define types and their properties,
/// grouping them by solution namespaces.</remarks>
public class TypeService(IEnumerable<ITypeBuilderExtension>? extensions = null, ILogger<TypeService>? logger = null) : ITypeService
{
	/// <summary>
	/// Track a solution and it's models.
	/// </summary>
	private readonly ConcurrentDictionary<SafeString, ConcurrentDictionary<SafeString, ModelDefinition>> _solutions = new();
	private readonly ConcurrentDictionary<SafeString, Type> _types = new();

	/// <summary>
	/// Track <see cref="ModuleBuilder"/> by solution.
	/// </summary>
	private readonly ConcurrentDictionary<SafeString, ModuleBuilder> _moduleBuilders = new();
	private readonly IEnumerable<ITypeBuilderExtension> _extensions = extensions ?? [];

	/// <summary>
	/// Creates a <see cref="ModuleBuilder"/> for the given solution namespace.
	/// </summary>
	/// <param name="solution">Name of the solution this model belongs to.</param>
	/// <returns><see cref="ModuleBuilder"/> uses to create types with a namespace of <paramref name="solution"/></returns>
	private ModuleBuilder GetModuleBuilder(SafeString solution)
	{
		if (string.IsNullOrEmpty(solution))
			throw new ArgumentNullException(nameof(solution), "Solution cannot be null or empty.");
		return _moduleBuilders.GetOrAdd(solution, ns =>
		{
			//using var activity = Tracing.Source.StartActivity("TypeService.GetModuleBuilder", System.Diagnostics.ActivityKind.Internal);
			//activity?.SetTag("solution", solution.Value);
			logger?.LogTrace("Creating module builder for solution: {solution}", solution);

			var assemblyName = new AssemblyName(ns);
			var builder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			return builder.DefineDynamicModule(ns);
		});
	}

	/// <summary>
	/// Dynamically builds a .NET type based on the provided model definition.
	/// </summary>
	/// <remarks>This method uses reflection emit to dynamically create a class with public properties based
	/// on the provided <paramref name="modelDefinition"/>. Each property includes a private backing field, a getter,
	/// and a setter.</remarks>
	/// <param name="modelDefinition">The definition of the model, including its properties and their types. Cannot be null.</param>
	/// <returns>A <see cref="Type"/> representing the dynamically created class.</returns>
	private Type BuildType(ModelDefinition modelDefinition)
	{
		if (modelDefinition is null)
			throw new ArgumentNullException(nameof(modelDefinition), "Model definition cannot be null.");

		modelDefinition.Validate();

		//using var activity = Tracing.Source.StartActivity("TypeService.BuildType", System.Diagnostics.ActivityKind.Internal);
		//activity?.SetTag("Solution", modelDefinition.Solution);
		//activity?.SetTag("ModelName", modelDefinition.ModelName);

		var moduleBuilder = GetModuleBuilder(modelDefinition.Solution.Name);

		var modelBuilder = new ModelBuilder(moduleBuilder, modelDefinition);
		foreach (var extension in _extensions)
		{
			try
			{
				extension.Apply(modelBuilder);
			}
			catch (Exception ex)
			{
				logger?.LogError(ex, "Error building model with extension {ExtensionName}", extension.GetType().Name);
				throw;
			}
		}
		return modelBuilder.CreateTypeInfo();
	}

	/// <summary>
	/// Retrieves the <see cref="Type"/> associated with the given model definition, creating it if it doesn't exist.
	/// </summary>
	/// <param name="modelDefinition">The definition of the model whose type is to be retrieved. Cannot be null.</param>
	/// <returns>A <see cref="Type"/> representing the class.</returns>
	public Type GetModel(ModelDefinition modelDefinition)
	{
		if (modelDefinition is null)
			throw new ArgumentNullException(nameof(modelDefinition), "Model definition cannot be null.");
		//_solutions
		//	.GetOrAdd(modelDefinition.Solution, _ => new ConcurrentDictionary<SafeString, ModelDefinition>())
		//	.GetOrAdd(modelDefinition.ModelName, modelDefinition);
		return _types.GetOrAdd(modelDefinition.FullName, _ => BuildType(modelDefinition));
	}

	public IEnumerable<Type> GetTypes() => _types.Values;
	public IEnumerable<Type> GetTypes(SafeString solution) => _types.Where(t => t.Key.Value.StartsWith($"{solution}.")).Select(t => t.Value);

	public IEnumerable<string> GetSolutions() => _solutions.Keys.Select(s => s.Value);

	public Type? GetType(string solution, string modelName)
	{
		_types.TryGetValue($"{solution}.{modelName}", out var type);
		return type;
	}

	public ModelDefinition? GetModelDefinition(SafeString modelFullName)
	{
		foreach (var solution in _solutions)
		{
			if (modelFullName.Value.StartsWith(solution.Key.Value + "."))
			{
				var modelName = modelFullName.Value.Substring(solution.Key.Value.Length + 1);
				if (solution.Value.TryGetValue(modelName, out var modelDefinition))
					return modelDefinition;
			}
		}
		return null;
	}

	public ModelDefinition? GetModelDefinition(Type type)
	{
		var parts = type.FullName?.Split('.').ToList();
		if (parts == null)
			return null;
		var solutionName = parts?.Count > 1 ? parts[0] : null;
		
		var modelName = parts?.Count > 1 ? string.Join(".", parts.Skip(1)) : type.FullName;
		if (solutionName is null || modelName is null)
			return null;
		if (_solutions.TryGetValue(solutionName, out var models))
		{
			if (models.TryGetValue(modelName, out var modelDefinition))
				return modelDefinition;
		}
		foreach (var modelType in _types)
		{
			if (modelType.Value.IsAssignableFrom(type))
			{
				return GetModelDefinition(modelType.Key);
			}
		}
		foreach (var solution in _solutions.Values)
		{
			foreach (var model in solution.Values)
			{
				var baseType = model.BaseClass;
				if (baseType != null && baseType.IsAssignableFrom(type))
				{
					return model;
				}
			}
		}
		return null;
	}

	public IEnumerable<ModelDefinition> GetModelDefinitions(SafeString solution)
	{
		if (_solutions.TryGetValue(solution, out var models))
			return models.Values;
		return [];
	}

}
