using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Reflection.Tests
{
    public class ClassDefinitionFacts
    {
        public class Constructor : ClassDefinitionFacts
        {
            [Fact]
            public void DefaultConstructor()
            {
                var classDefinition = new ClassDefinition();
                Assert.NotNull(classDefinition.Properties);
                Assert.Empty(classDefinition.Properties);
                Assert.NotNull(classDefinition.Interfaces);
                Assert.Empty(classDefinition.Interfaces);
            }

            [Fact]
            public void WrapsConcreteTypes()
            {

                var classDefinition = new ClassDefinition<ConcreteType>();
                Assert.Equal("ConcreteType", classDefinition.ClassName.Value);
                Assert.Equal("nc.Reflection.Tests", classDefinition.Solution.Value);
                Assert.Equal(2, classDefinition.Properties.Count());
                Assert.Contains("Id", classDefinition.Properties.Select(p => p.Name));
                Assert.Contains("Name", classDefinition.Properties.Select(p => p.Name));
            }

            [Fact]
            public void WrapsAnonumouseType()
            {
                var poco = new { Id = 1, Name = "Test" };
                var classDefinition = new ClassDefinition(poco);
                Assert.Contains("Anonymous", classDefinition.Solution.Value);
                Assert.Contains("Anonymous", classDefinition.ClassName.Value);
                Assert.Equal(2, classDefinition.Properties.Count());
            }
        }
    }

    public class ConcreteType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
