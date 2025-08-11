namespace nc.Reflection.Tests;

public class TypeServiceFacts
{

    public class GetClass
    {
        [Fact]
        public void Naming()
        {
            var type = this.GetType();
            Assert.NotNull(type.Module.Name);
            AppDomain.CurrentDomain.GetAssemblies();
        }

        [Fact]
        public void BuildsType()
        {
            var typeService = new TypeService();
            var modelDefinition = new ModelDefinition
            {
                ModelName = "BuildsTypeClass",
                Solution = "TestSolution",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition
                    {
                        Name = "Id",
                        ClrType = typeof(int),
                        IsKey = true
                    },
                    new PropertyDefinition
                    {
                        Name = "Name",
                        ClrType = typeof(string)
                    }
                }
            };
            var type = typeService.GetModel(modelDefinition);
            Assert.NotNull(type);
            Assert.Equal("BuildsTypeClass", type.Name);
            Assert.StartsWith("TestSolution", type.Assembly.FullName);
        }

        [Fact]
        public void SegregatesBySolution()
        {
            var typeService = new TypeService();
            var classDefinition1 = new ModelDefinition
            {
                ModelName = "ClassA",
                Solution = "SolutionA"
            };
            var classDefinition2 = new ModelDefinition
            {
                ModelName = "ClassA",
                Solution = "SolutionB"
            };
            var type1 = typeService.GetModel(classDefinition1);
            var type2 = typeService.GetModel(classDefinition2);
            Assert.NotNull(type1);
            Assert.NotNull(type2);
            Assert.NotEqual(type1.Assembly, type2.Assembly);
            Assert.Equal("SolutionA.ClassA", type1.FullName);
            Assert.Equal("SolutionB.ClassA", type2.FullName);
            Assert.Equal("ClassA", type1.Name);
            Assert.Equal("ClassA", type2.Name);
        }


        [Theory]
        [InlineData("AddsInterfaceLong", typeof(long))]
        [InlineData("AddsInterfaceInt", typeof(int))]
        [InlineData("AddsInterfaceGuid", typeof(Guid))]
        public void AddsInterfaces(string name, Type identityType)
        {
            var typeService = new TypeService();
            var interfaceType = typeof(ITestInterface<>).MakeGenericType(identityType);
            var classDefinition = new ModelDefinition
            {
                ModelName = name,
                Solution = "TestSolution",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition
                    {
                        Name = "Id",
                        ClrType = identityType,
                        IsKey = true
                    }
                },
                Interfaces = new HashSet<Type>
                {
                    interfaceType
                }
            };
            var type = typeService.GetModel(classDefinition);
            Assert.NotNull(type);
            Assert.Equal(name, type.Name);
            // Assert.IsAssignableFrom<ITestInterface<long>>(type);
            Assert.True(interfaceType.IsAssignableFrom(type));

            var instance = Activator.CreateInstance(type);
            Assert.True(interfaceType.IsInstanceOfType(instance));

        }

        [Fact]
        public void IgnoresUnsupportedInterfaces()
        {
            var typeService = new TypeService();
            var classDefinition = new ModelDefinition
            {
                ModelName = "IgnoresUnsupportedInterfacesClass",
                Solution = "TestSolution",
                Interfaces = new HashSet<Type>
                {
                    typeof(ITestInterface<string>)
                }
            };

            Assert.Throws<MissingMemberException>(() =>
            {
                var type = typeService.GetModel(classDefinition);
            });
        }

        [Fact]
        public void AddsInterfacesWithDefaultImplementation()
        {
            var typeService = new TypeService();
            var classDefinition = new ModelDefinition
            {
                ModelName = "CalculatorClass",
                Solution = "TestSolution",
                Interfaces = new HashSet<Type>
                {
                    typeof(Calculator)
                }
            };
            var type = typeService.GetModel(classDefinition);
            Assert.NotNull(type);
            Assert.Equal("CalculatorClass", type.Name);
            Assert.True(typeof(Calculator).IsAssignableFrom(type));
            var instance = Activator.CreateInstance(type) as Calculator;
            Assert.NotNull(instance);
            Assert.Equal(3, instance.Add(1, 2));
            Assert.Equal(-1, instance.Subtract(1, 2));
        }

        [Fact]
        public void DerivesFromBaseClass()
        {
            var typeService = new TypeService();
            var classDefinition = new ModelDefinition
            {
                ModelName = "DerivedClass",
                Solution = "TestSolution",
                BaseClass = typeof(SampleBaseClass)
            };
            var type = typeService.GetModel(classDefinition);
            Assert.NotNull(type);
            Assert.Equal("DerivedClass", type.Name);
            Assert.True(typeof(SampleBaseClass).IsAssignableFrom(type));
            var instance = Activator.CreateInstance(type) as SampleBaseClass;
            Assert.NotNull(instance);
            instance.Description = "This is a derived class.";
            Assert.Equal("DerivedClass says: This is a derived class.", instance.Describe());
        }
    }

    public interface ITestInterface<T>
    {
        T Id { get; set; }
    }

    public interface Calculator
    {
        int Add(int a, int b) => a + b;
        int Subtract(int a, int b) => a - b;
    }

    public class SampleBaseClass
    {
        public string? Description { get; set; }

        public string Describe()
        {
            return $"{this.GetType().Name} says: {Description}";
        }
    }

}
