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
        public Type GetClass(ClassDefinition classDefinition);
    }
}
