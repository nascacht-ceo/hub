using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace nc.Hub.Tests
{
    public class ModelDefinitionFacts
    {
		private readonly Solution _solution;

		public ModelDefinitionFacts() 
        {
            _solution = new Solution() { Name = "ModelDefinitionFacts" };
        }


		public class Constructor : ModelDefinitionFacts
        {
            [Fact]
            public void DefaultConstructor()
            {
                var modelDefinition = new ModelDefinition()
                {
                    Solution = _solution
				};
                Assert.NotNull(modelDefinition.Properties);
                Assert.Empty(modelDefinition.Properties);
                Assert.NotNull(modelDefinition.Interfaces);
                Assert.Empty(modelDefinition.Interfaces);
            }

            [Fact]
            public void WrapsConcreteTypes()
            {

                var modelDefinition = new ModelDefinition<ConcreteType>()
                {
                    Solution = _solution
				};
                Assert.Equal("ConcreteType", modelDefinition.ModelName.Value);
                // Assert.Equal("nc.Reflection.Tests", modelDefinition.Solution.Name);
                Assert.Equal(2, modelDefinition.Properties.Count());
                Assert.Contains("Id", modelDefinition.Properties.Select(p => p.Name));
                Assert.Contains("Name", modelDefinition.Properties.Select(p => p.Name));
            }

            [Fact]
            public void WrapsAnonumouseType()
            {
                var poco = new { Id = 1, Name = "Test" };
                var modelDefinition = new ModelDefinition(poco) { Solution = _solution };
                // Assert.Contains("Anonymous", modelDefinition.Solution.Name);
                Assert.Contains("Anonymous", modelDefinition.ModelName.Value);
                Assert.Equal(2, modelDefinition.Properties.Count());
            }

        }
    }

    public class ConcreteType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
