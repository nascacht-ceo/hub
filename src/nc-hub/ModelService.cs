using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace nc.Hub;

public class ModelService : IModelService
{
	private readonly ILogger<ModelService>? _logger;
	private ModelServiceOptions _options;
	private readonly ConcurrentDictionary<ModelDefinition, Type> _types = new();
	private readonly ConcurrentDictionary<ISolution, ModuleBuilder> _moduleBuilders = new();
	private readonly IEnumerable<ITypeBuilderExtension> _extensions;

	public ModelService(IOptionsMonitor<ModelServiceOptions>? options = null, IEnumerable<ITypeBuilderExtension>? extensions = null, ILogger<ModelService>? logger = null)
	{
		_logger = logger;
		_options = options?.CurrentValue ?? new ModelServiceOptions();
		options?.OnChange(opt => _options = opt);
		_extensions = extensions ?? [];
	}

	public ModelDefinition? GetModelDefinition(Type type)
	{
		var entry =  _types.FirstOrDefault(kv => kv.Value == type);
		return entry.Key;
	}
		

	public Type GetModelType(ModelDefinition modelDefinition)
	{
		if (modelDefinition is null)
			throw new ArgumentNullException(nameof(modelDefinition), "Model definition cannot be null.");
		return _types.GetOrAdd(modelDefinition, _ => BuildType(modelDefinition));

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

		//using var activity = Tracing.Source.StartActivity("ModelService.BuildType", System.Diagnostics.ActivityKind.Internal);
		//activity?.SetTag("Solution", modelDefinition.Solution);
		//activity?.SetTag("ModelName", modelDefinition.ModelName);

		var moduleBuilder = GetModuleBuilder(modelDefinition.Solution);

		var modelBuilder = new ModelBuilder(moduleBuilder, modelDefinition);
		foreach (var extension in _extensions)
		{
			try
			{
				extension.Apply(modelBuilder);
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Error building model with extension {ExtensionName}", extension.GetType().Name);
				throw;
			}
		}
		return modelBuilder.CreateTypeInfo();
	}

	/// <summary>
	/// Creates a <see cref="ModuleBuilder"/> for the given solution namespace.
	/// </summary>
	/// <param name="solution">Name of the solution this model belongs to.</param>
	/// <returns><see cref="ModuleBuilder"/> uses to create types with a namespace of <paramref name="solution"/></returns>
	private ModuleBuilder GetModuleBuilder(ISolution solution)
	{
		if (solution == null)
			throw new ArgumentNullException(nameof(solution), "Solution cannot be null or empty.");
		return _moduleBuilders.GetOrAdd(solution, ns =>
		{
			//using var activity = Tracing.Source.StartActivity("TypeService.GetModuleBuilder", System.Diagnostics.ActivityKind.Internal);
			//activity?.SetTag("solution", solution.Value);
			_logger?.LogTrace("Creating module builder for solution: {solution}", solution);

			var assemblyName = new AssemblyName(ns.Name);
			var builder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			return builder.DefineDynamicModule(ns.Name);
		});
	}
}
