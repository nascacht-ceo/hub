using nc.Hub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Reflection
{
    /// <summary>
    /// Provides functionality for retrieving type information based on class definitions.
    /// </summary>
    /// <remarks>This interface defines a contract for services that map class definitions to their
    /// corresponding <see cref="Type"/> representations. Implementations of this interface may be used in scenarios
    /// where runtime type resolution is required.</remarks>
    public interface ITypeService
    {
        /// <summary>
        /// Retrieves the <see cref="Type"/> associated with the specified class definition.
        /// </summary>
        /// <param name="classDefinition">The definition of the class for which to retrieve the type.</param>
        /// <returns>The <see cref="Type"/> that corresponds to the provided class definition.</returns>
        public Type GetModel(ModelDefinition classDefinition);

        /// <summary>
        /// Retrieves a collection of solutions available in the current context.
        /// </summary>
        /// <remarks>This method does not guarantee the order of the solutions in the returned
        /// collection.</remarks>
        /// <returns>An <see cref="IEnumerable{T}"/> of strings, where each string represents a solution. The collection will be
        /// empty if no solutions are available.</returns>
        public IEnumerable<string> GetSolutions();

        /// <summary>
        /// Retrieves a collection of all types available in the current context.
        /// </summary>
        /// <remarks>This method provides access to all types that are discoverable in the current
        /// context. The caller can enumerate the returned collection to inspect or process the types.</remarks>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="Type"/> objects representing the types available. The
        /// collection will be empty if no types are found.</returns>
        public IEnumerable<Type> GetTypes();

        /// <summary>
        /// Retrieves a collection of types associated with the specified solution.
        /// </summary>
        /// <param name="solution">Name of solution containing the types.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="Type"/> objects representing the types available. The
        /// collection will be empty if no types are found.</returns>
        public IEnumerable<Type> GetTypes(SafeString solution);


        /// <summary>
        /// Retrieves a collection of class definitions from the specified solution.
        /// </summary>
        /// <param name="solution">A <see cref="SafeString"/> representing the solution from which to extract class definitions.  This
        /// parameter cannot be null or empty.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ModelDefinition"/> objects representing the classes  defined
        /// in the provided solution. Returns an empty collection if no class definitions are found.</returns>
        public IEnumerable<ModelDefinition> GetModelDefinitions(SafeString solution);

        /// <summary>
        /// Retrieves the <see cref="ModelDefinition"/> associated with the specified type.
        /// </summary>
        /// <param name="type">The type for which to retrieve the model definition. This parameter cannot be <see langword="null"/>.</param>
        /// <returns>The <see cref="ModelDefinition"/> associated with the specified type, or <see langword="null"/> if no
        /// definition exists for the type.</returns>
        public ModelDefinition? GetModelDefinition(Type type);

        /// <summary>
        /// Retrieves the <see cref="ModelDefinition"/> associated with the specified type.
        /// </summary>
        /// <typeparam name="T">The type for which to retrieve the model definition.</typeparam>
        /// <returns>The <see cref="ModelDefinition"/> for the specified type <typeparamref name="T"/>,  or <see
        /// langword="null"/> if no definition is found.</returns>
        public ModelDefinition? GetModelDefinition<T>() => GetModelDefinition(typeof(T));



	}
}
