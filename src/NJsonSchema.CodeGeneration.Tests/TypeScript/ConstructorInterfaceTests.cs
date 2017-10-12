using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.Generation;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class ConstructorInterfaceTests
    {
        public class Person
        {
            public Address Address { get; set; }

            public Car[] Cars { get; set; }

            public Dictionary<string, Skill> Skills { get; set; }

            // not supported

            public List<Car[]> Foo { get; set; }

            public Dictionary<string, Skill[]> Bar { get; set; }

        }

        public class Car
        {
            public string Foo { get; set; }
        }

        public class Address
        {
            public string Foo { get; set; }
        }

        public class Skill
        {
            public string Foo { get; set; }
        }

        [TestMethod]
        public async Task When_constructor_interface_and_conversion_code_is_generated_then_it_is_correct()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Person>(new JsonSchemaGeneratorSettings());

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                GenerateConstructorInterface = true,
                ConvertConstructorInterfaceData = true

            });

            var output = generator.GenerateFile("MyClass");

            //// Assert

            // address property is converted:
            Assert.IsTrue(output.Contains("this.address = data.address && !(<any>data.address).toJSON ? new Address(data.address) : <Address>this.address;"));
            // cars items are converted:
            Assert.IsTrue(output.Contains("this.cars[i] = item && !(<any>item).toJSON ? new Car(item) : <Car>item;"));
            // skills values are converted:
            Assert.IsTrue(output.Contains("this.skills[key] = item && !(<any>item).toJSON ? new Skill(item) : <Skill>item;"));

            // interface is correct
            Assert.IsTrue(output.Replace("\r", "").Replace("\n", "").Contains(@"export interface IMyClass {
    address: IAddress;
    cars: ICar[];
    skills: { [key: string] : ISkill; };
    foo: Car[][];
    bar: { [key: string] : Skill[]; };
}".Replace("\r", "").Replace("\n", "")));
        }

        [TestMethod]
        public async Task When_array_of_string_dictionary_is_used_with_ConvertConstructorInterfaceData_then_it_should_be_ignored()
        {
            //// Arrange
            var json = @"
{
    ""type"": ""object"",
    ""properties"": {
        ""custom4"": {
            ""type"": ""array"",
            ""items"": {
                ""type"": ""object"",
                ""additionalProperties"": {
                    ""type"": ""string""
                }
            }
        }
    }
}";
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                GenerateConstructorInterface = true,
                ConvertConstructorInterfaceData = true

            });

            var output = generator.GenerateFile("MyClass");
            
            //// Assert
            Assert.IsTrue(output.Contains("custom4: { [key: string] : string; }[];"));
        }
    }
}
