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
        public async Task When_()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Person>(new JsonSchemaGeneratorSettings());

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                GenerateConstructorInterface = true
            });

            var output = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(output.Contains(@"export interface IMyClass {
    address: IAddress;
    cars: ICar[];
    skills: { [key: string] : ISkill; };
    foo: Car[][];
    bar: { [key: string] : Skill[]; };
}"));
        }
    }
}
