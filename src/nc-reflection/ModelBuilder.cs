using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace nc.Reflection
{
    public class ModelBuilder
    {
        public readonly ModuleBuilder ModuleBuilder;
        public readonly ModelDefinition ClassDefinition;
        public readonly TypeBuilder TypeBuilder;
        public readonly IDictionary<string, ClassProperty> PropertyMap;

        public ModelBuilder(ModuleBuilder moduleBuilder, ModelDefinition classDefinition)
        {
            ModuleBuilder = moduleBuilder;
            ClassDefinition = classDefinition;
            TypeBuilder = moduleBuilder.DefineType($"{classDefinition.FullName}", TypeAttributes.Public | TypeAttributes.Class, classDefinition.BaseClass);
            PropertyMap = new Dictionary<string, ClassProperty>();
        }

        public Type CreateTypeInfo()
        {
            BuildProperties();
            BuildInterfaces();
            return TypeBuilder.CreateTypeInfo()!;
        }

        public void BuildProperties()
        {
            foreach (var property in ClassDefinition.Properties.Where(p => p.DeclaringType == null))
            {
                var classProperty = new ClassProperty(property, TypeBuilder);
                PropertyMap[property.Name] = classProperty;
            }
        }

        public void BuildInterfaces()
        {
            foreach (var interfaceType in ClassDefinition.Interfaces)
            {
                TypeBuilder.AddInterfaceImplementation(interfaceType);

                foreach (var interfaceProp in interfaceType.GetProperties())
                {
                    if (!PropertyMap.TryGetValue(interfaceProp.Name, out var classProperty))
                        throw new InvalidOperationException($"Property '{interfaceProp.Name}' required by interface '{interfaceType.FullName}' is not defined in ClassDefinition.");

                    if (interfaceProp.GetGetMethod() is MethodInfo interfaceGetter)
                        TypeBuilder.DefineMethodOverride(classProperty.Getter, interfaceGetter);

                    if (interfaceProp.GetSetMethod() is MethodInfo interfaceSetter)
                        TypeBuilder.DefineMethodOverride(classProperty.Setter, interfaceSetter);
                }
            }
        }


    }

    public class ClassProperty
    {
        /// <summary>
        /// Represents a dynamically generated property for a class, including its backing field, getter, and setter
        /// methods.
        /// </summary>
        /// <remarks>This constructor creates a property for a dynamically generated type using the
        /// provided <see cref="PropertyDefinition"/> and <see cref="TypeBuilder"/>. It defines a private backing field,
        /// a public getter method, and a public setter method for the property. The property is then associated with
        /// these methods to enable standard property access.</remarks>
        /// <param name="property">The definition of the property, including its name and CLR type.</param>
        /// <param name="typeBuilder">The <see cref="TypeBuilder"/> used to define the property and its associated methods within the dynamic
        /// type.</param>
        public ClassProperty(PropertyDefinition property, TypeBuilder typeBuilder)
        {
            FieldBuilder = typeBuilder.DefineField($"_{property.Name}", property.ClrType, FieldAttributes.Private);

            PropertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.HasDefault, property.ClrType, null);

            // Define the getter for the property
            Getter = typeBuilder.DefineMethod($"get_{property.Name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                property.ClrType, Type.EmptyTypes);
            var getIL = Getter.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, FieldBuilder);
            getIL.Emit(OpCodes.Ret);

            // Define the setter for the property
            Setter = typeBuilder.DefineMethod($"set_{property.Name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                null, new[] { property.ClrType });
            var setIL = Setter.GetILGenerator();
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, FieldBuilder);
            setIL.Emit(OpCodes.Ret);

            PropertyBuilder.SetGetMethod(Getter);
            PropertyBuilder.SetSetMethod(Setter);
        }

        /// <summary>
        /// Gets or sets the <see cref="PropertyBuilder"/> instance used to define and configure properties in a dynamic
        /// type.
        /// </summary>
        /// <remarks>Use this property to access or modify the <see cref="PropertyBuilder"/> associated
        /// with a property when working with reflection emit.</remarks>
        public PropertyBuilder PropertyBuilder { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="FieldBuilder"/> instance used to define and represent a field in a dynamically
        /// created type.
        /// </summary>
        public FieldBuilder FieldBuilder { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="MethodBuilder"/> instance that represents the getter method for a property.
        /// </summary>
        public MethodBuilder Getter { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="MethodBuilder"/> instance that represents the setter method of a property.
        /// </summary>
        public MethodBuilder Setter { get; set; }
    }
}
