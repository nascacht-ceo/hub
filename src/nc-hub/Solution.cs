using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;

namespace nc.Hub;

public partial class Solution: ISolution
{
	/// <summary>
	/// <inheritdoc/>
	/// </summary>
    public required SafeString Name { get; set; }

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public string Version { get; set; } = "1.0.0";

	private ConcurrentDictionary<SafeString, ModelDefinition> _models { get; set; } = new ConcurrentDictionary<SafeString, ModelDefinition>();

	private ConcurrentDictionary<ModelDefinition, Type> _modelTypes { get; set; } = new ConcurrentDictionary<ModelDefinition, Type>();

	/// <summary>
	/// Gets or sets the collection of endpoints, such as Datbases, Cloud Tenants, or OpenApi specification.
	/// </summary>
	/// <remarks>Use this property to manage and access the endpoints associated with their unique identifiers.
	/// Modifications to the dictionary will directly affect the stored endpoints.</remarks>
	public IDictionary<SafeString, IEndpoint> Endpoints { get; set; } = new ConcurrentDictionary<SafeString, IEndpoint>();

	public T GetEndpoint<T>(SafeString name) where T : IEndpoint
	{
		if (Endpoints.TryGetValue(name, out var endpoint) && endpoint is T typedEndpoint)
		{
			return typedEndpoint;
		}
		throw new KeyNotFoundException($"No endpoint of type {typeof(T).Name} with name '{name}' found.");
	}

	//public IEnumerable<string> FileStorage { get; set; }

	//public IEnumerable<string> CloudTenants { get; set; }
	CredentialCache CredentialCache { get; set; } = new CredentialCache();

	public IServiceProvider ServiceProvider { get; init; }

	private IEnumerable<Func<Task>> _initializers = new List<Func<Task>>();

	private Solution()
	{
		_moduleBuilder = new Lazy<ModuleBuilder>(() => { 
			var assemblyName = new AssemblyName(Name);
			var builder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			return builder.DefineDynamicModule(Name);
		});

		ServiceProvider = new ServiceCollection().AddSingleton<ISolution>(sp => this).BuildServiceProvider();


	}

	public Solution(IEnumerable<ITypeBuilderExtension>? extensions = null, ILogger<Solution>? logger = null)
		: this()
	{
		_extensions = extensions;
		_logger = logger;
		logger?.LogInformation("Initializing solution: {SolutionName}", Name);
		Initialize();
	}

	partial void Initialize();

	public async Task InitializeAsync()
	{
		foreach (var initializer in _initializers)
		{
			await initializer();
		}
	}

	public Type GetModelType(ModelDefinition modelDefinition)
	{
		if (modelDefinition is null)
			throw new ArgumentNullException(nameof(modelDefinition), "Model definition cannot be null.");
		return _modelTypes.GetOrAdd(modelDefinition, _ => BuildType(modelDefinition));
	}

	public ModelDefinition GetModelDefinition(Type modelType)
	{
		throw new NotImplementedException();
	}

	public IEnumerable<ModelDefinition> GetModelDefinitions() => _models.Values;

	public IEnumerable<Type> GetModelTypes()
	{
		foreach (var model in _models.Values)
		{
			yield return GetModelType(model);
		}
	} 


	public ISolution AddModel(ModelDefinition definition)
	{
		_models.GetOrAdd(definition.ModelName, _ => definition);
		return this;
	}

	public void AddEndpoint<T>(T endpoint) where T : IEndpoint
	{
		throw new NotImplementedException();
	}

	public void AddCredential(NetworkCredential credential)
	{
		throw new NotImplementedException();
	}

	public NetworkCredential GetCredential(Uri uri)
	{
		throw new NotImplementedException();
	}

	public NetworkCredential? GetCredential(Uri uri, string authType)
	{
		throw new NotImplementedException();
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

		var modelBuilder = new ModelBuilder(_moduleBuilder.Value, modelDefinition);
		if (_extensions is not null)
		{
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
		}
		return modelBuilder.CreateTypeInfo();
	}

	private Lazy<ModuleBuilder> _moduleBuilder;
	private readonly IEnumerable<ITypeBuilderExtension>? _extensions;
	private readonly ILogger<Solution>? _logger;

	public override string ToString() => Name.Value;
}
