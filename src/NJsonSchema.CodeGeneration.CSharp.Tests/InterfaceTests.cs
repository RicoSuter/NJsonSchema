using System.Threading.Tasks;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class InterfaceTests
    {
        public interface IPerson
        {
            string LastName { get; set; }
            string FirstName { get; set; }
        }

        public class Person : IPerson
        {
            public string LastName { get; set; }
            public string FirstName { get; set; }
        }

        [Fact]
        public async Task When_interface_has_properties_then_properties_are_included_in_schema()
        {
            //// Arrange
            var schema = JsonSchema.FromType<Person>(new JsonSchemaGeneratorSettings());

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                PropertyNamingStyle = CSharpNamingStyle.PascalCase
            });
            var code = generator.GenerateFile("Person");

            //// Assert
            Assert.Equal(2, schema.Properties.Count);
            Assert.Contains("public string LastName { get; set; }\n", code);
            Assert.Contains("public string FirstName { get; set; }\n", code);
        }

        [Fact]
        public async Task When_class_implements_interface_then_properties_are_included_in_schema()
        {
            //// Arrange
            var schema = JsonSchema.FromType<Person>(new JsonSchemaGeneratorSettings());

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                PropertyNamingStyle = CSharpNamingStyle.PascalCase
            });
            var code = generator.GenerateFile("Person");

            //// Assert
            Assert.Equal(2, schema.Properties.Count);
            Assert.Contains("public string LastName { get; set; }\n", code);
            Assert.Contains("public string FirstName { get; set; }\n", code);
        }
    }

}
