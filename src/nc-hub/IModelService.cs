using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace nc.Hub;

public interface IModelService
{
	public Type GetModelType(ModelDefinition modelDefinition);

	public ModelDefinition? GetModelDefinition(Type type);
}
