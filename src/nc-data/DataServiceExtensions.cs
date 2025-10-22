using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace nc.Data.Extensions;

public static class DataServiceExtensions
{
	/// <summary>
	/// Represents a cached reference to the generic <see
	/// cref="EntityFrameworkServiceCollectionExtensions.AddDbContext{TContext}(IServiceCollection,
	/// Action{DbContextOptionsBuilder}, ServiceLifetime, ServiceLifetime)"/> method.
	/// </summary>
	/// <remarks>This field holds a reference to the generic <c>AddDbContext</c> method defined in the <see
	/// cref="EntityFrameworkServiceCollectionExtensions"/> class.  It is resolved using reflection to match the method
	/// signature with four parameters, where the second parameter is of type <see
	/// cref="Action{DbContextOptionsBuilder}"/>.</remarks>
	private static MethodInfo? _genericAddDbContextMethod = typeof(EntityFrameworkServiceCollectionExtensions)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.FirstOrDefault(m =>
				m.Name == "AddDbContext" &&
				m.IsGenericMethod &&
				m.GetParameters().Length == 4 &&
				m.GetParameters()[1].ParameterType == typeof(Action<DbContextOptionsBuilder>));

	/// <summary>
	/// Registers the specified DbContext type with the dependency injection container.
	/// </summary>
	/// <remarks>This method dynamically resolves and invokes the generic AddDbContext method for the specified
	/// DbContext type. It is typically used in scenarios where the DbContext type is determined at runtime.</remarks>
	/// <param name="services">The <see cref="IServiceCollection"/> to which the DbContext will be added.</param>
	/// <param name="dbContextType">The type of the DbContext to register.</param>
	/// <param name="optionsAction">An optional action to configure the <see cref="DbContextOptionsBuilder"/> for the DbContext. This can be used to
	/// specify database providers, connection strings, or other options.</param>
	/// <param name="contextLifetime">The lifetime with which the DbContext instances will be registered in the container.  The default is <see
	/// cref="ServiceLifetime.Scoped"/>.</param>
	/// <param name="optionsLifetime">The lifetime with which the <see cref="DbContextOptions"/> will be registered in the container.  The default is
	/// <see cref="ServiceLifetime.Scoped"/>.</param>
	/// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the underlying generic AddDbContext method cannot be found.</exception>
	public static IServiceCollection AddDbContext(this IServiceCollection services, 
		Type dbContextType, Action<DbContextOptionsBuilder>? optionsAction = null,
		ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
		ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
	{
		if (_genericAddDbContextMethod is null)
			throw new InvalidOperationException("Could not find the AddDbContext extension method.");

		// Create a closed generic method using the provided DbContext type.
		var closedGenericMethod = _genericAddDbContextMethod.MakeGenericMethod(dbContextType);

		// Define the parameters for the method invocation.
		object?[] parameters = new object?[]
		{
			services,
			optionsAction,
			contextLifetime,
			optionsLifetime
		};

		// Invoke the method. Since it's a static extension method, the first argument is null.
		closedGenericMethod.Invoke(null, parameters);

		return services;
	}
}
