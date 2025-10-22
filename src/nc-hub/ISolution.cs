using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace nc.Hub;


/// <summary>
/// Represents a solution that provides access to models, endpoints, and associated metadata.
/// </summary>
/// <remarks>This interface defines the structure and behavior of a solution, including its name, version, 
/// service provider, models, and endpoints. It also provides methods to retrieve model types  and definitions, as well
/// as specific endpoints by name.</remarks>
public interface ISolution: ICredentials
{
	/// <summary>
	/// Name of the solution.
	/// </summary>
	public SafeString Name { get; set; }

	/// <summary>
	/// Gets or sets the version of the solution.
	/// </summary>
	public string Version { get; set; }

	/// <summary>
	/// Gets the service provider used to resolve solution-specific services.
	/// </summary>
	public IServiceProvider ServiceProvider { get; }

	
	/// <summary>
	/// Adds a new model definition to the solution.
	/// </summary>
	/// <remarks>Use this method to register a new model definition. The provided <paramref name="definition"/> must
	/// contain all required properties and adhere to the expected format.</remarks>
	/// <param name="definition">The model definition to add. This parameter cannot be <see langword="null"/>.</param>
	public ISolution AddModel(ModelDefinition definition);

	/// <summary>
	/// Retrieves the <see cref="Type"/> of the model represented by the specified <paramref name="modelDefinition"/>.
	/// </summary>
	/// <param name="modelDefinition">The definition of the model whose type is to be retrieved. Cannot be <see langword="null"/>.</param>
	/// <returns>The <see cref="Type"/> of the model described by <paramref name="modelDefinition"/>.</returns>
	public Type GetModelType(ModelDefinition modelDefinition);

	/// <summary>
	/// Retrieves the <see cref="ModelDefinition"/> for the specified model type.
	/// </summary>
	/// <remarks>This method is typically used to obtain metadata about a model type, such as its properties,
	/// relationships, and other structural details.</remarks>
	/// <param name="modelType">The type of the model for which the definition is being retrieved. This parameter cannot be <see langword="null"/>.</param>
	/// <returns>A <see cref="ModelDefinition"/> object that describes the structure and metadata of the specified model type.</returns>
	public ModelDefinition GetModelDefinition(Type modelType);

	public IEnumerable<ModelDefinition> GetModelDefinitions();

	public IEnumerable<Type> GetModelTypes();

	/// <summary>
	/// Adds a new endpoint to the system.
	/// </summary>
	/// <remarks>This method registers the specified endpoint for use within the system.  Ensure that the provided
	/// endpoint implements the <see cref="IEndpoint"/> interface.</remarks>
	/// <typeparam name="T">The type of the endpoint to add. Must implement the <see cref="IEndpoint"/> interface.</typeparam>
	/// <param name="endpoint">The endpoint instance to add. Cannot be <see langword="null"/>.</param>
	public void AddEndpoint<T>(T endpoint) where T : IEndpoint;

	/// <summary>
	/// Retrieves an endpoint of the specified type by its name.
	/// </summary>
	/// <typeparam name="T">The type of the endpoint to retrieve. Must implement <see cref="IEndpoint"/>.</typeparam>
	/// <param name="name">The name of the endpoint to retrieve. This value cannot be null or empty.</param>
	/// <returns>An instance of the specified endpoint type <typeparamref name="T"/> if found.</returns>
	public T GetEndpoint<T>(SafeString name) where T : IEndpoint;

	public void AddCredential(NetworkCredential credential);

	public NetworkCredential GetCredential(Uri uri);
}
