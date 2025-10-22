using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Hub;

public static class SolutionExtensions
{
	public static ISolution AddModel<T>(this ISolution solution)
	{
		if (solution == null) throw new ArgumentNullException(nameof(solution));
		var modelDefinition = new ModelDefinition(typeof(T))
		{
			Solution = solution
		};
		solution.AddModel(modelDefinition);
		return solution;
	}

	public static ModelDefinition GetModelDefinition<T>(this ISolution solution) where T : class
	{
		if (solution == null) throw new ArgumentNullException(nameof(solution));
		return solution.GetModelDefinition(typeof(T));
	}
}
