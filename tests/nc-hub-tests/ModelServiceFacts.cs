using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace nc.Hub.Tests
{
	public class ModelServiceFacts
	{
		private readonly ModelService _modelService;

		public ModelServiceFacts()
		{
			_modelService = new ModelService();
		}

		public class  GetModelDefinition: ModelServiceFacts
		{
			[Fact]
			public void NullModelDefinition_ThrowsArgumentNullException()
			{

			}
		}

		public class GetModelType : ModelServiceFacts
		{
		}
	}
}
