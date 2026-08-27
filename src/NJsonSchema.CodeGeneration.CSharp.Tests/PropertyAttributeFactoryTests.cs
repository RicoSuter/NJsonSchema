using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp.Models;
using NJsonSchema.CodeGeneration.CSharp.Tests;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class PropertyAttributeFactoryTests
    {
        private const string Json =
            """
            {
              "type": "object",
              "properties": {
                "name": { "type": "string" },
                "age": { "type": "integer" }
              }
            }
            """;

        private sealed class TestPropertyAttributeFactory : IPropertyAttributeFactory
        {
            public List<(string PropertyName, string SchemaName)> Calls { get; } = [];

            public IEnumerable<string> CreateAttributes(PropertyModel property, JsonSchemaProperty schema)
            {
                Calls.Add((property.PropertyName, schema.Name));

                yield return "[System.Runtime.Serialization.DataMember]";
                yield return $"[My.Custom.Attribute(\"{schema.Name}\")]";
            }
        }

        [Fact]
        public async Task When_PropertyAttributeFactory_is_set_then_attributes_are_rendered()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);
            var factory = new TestPropertyAttributeFactory();
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "TestNs",
                PropertyAttributeFactory = factory
            });

            // Act
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("[System.Runtime.Serialization.DataMember]\n        [My.Custom.Attribute(\"name\")]\n        public string Name", code);
            Assert.Contains("[System.Runtime.Serialization.DataMember]\n        [My.Custom.Attribute(\"age\")]\n        public int Age", code);

            Assert.Equal([("Name", "name"), ("Age", "age")], factory.Calls);
        }

        [Fact]
        public async Task When_PropertyAttributeFactory_is_not_set_then_no_additional_attributes_are_rendered()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "TestNs"
            });

            // Act
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.DoesNotContain("My.Custom.Attribute", code);
            CSharpCompiler.AssertCompile(code);
        }
    }
}
