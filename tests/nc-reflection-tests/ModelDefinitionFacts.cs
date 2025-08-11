using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Reflection.Tests
{
    public class ModelDefinitionFacts
    {
        public class Constructor : ModelDefinitionFacts
        {
            [Fact]
            public void DefaultConstructor()
            {
                var modelDefinition = new ModelDefinition();
                Assert.NotNull(modelDefinition.Properties);
                Assert.Empty(modelDefinition.Properties);
                Assert.NotNull(modelDefinition.Interfaces);
                Assert.Empty(modelDefinition.Interfaces);
            }

            [Fact]
            public void WrapsConcreteTypes()
            {

                var modelDefinition = new ModelDefinition<ConcreteType>();
                Assert.Equal("ConcreteType", modelDefinition.ModelName.Value);
                Assert.Equal("nc.Reflection.Tests", modelDefinition.Solution.Value);
                Assert.Equal(2, modelDefinition.Properties.Count());
                Assert.Contains("Id", modelDefinition.Properties.Select(p => p.Name));
                Assert.Contains("Name", modelDefinition.Properties.Select(p => p.Name));
            }

            [Fact]
            public void WrapsAnonumouseType()
            {
                var poco = new { Id = 1, Name = "Test" };
                var modelDefinition = new ModelDefinition(poco);
                Assert.Contains("Anonymous", modelDefinition.Solution.Value);
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
