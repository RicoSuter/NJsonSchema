using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class StringPropertyRequiredTests
    {
        private class ClassWithRequiredObject
        {
            [Required]
            public string Property { get; set; }
        }

        [Fact]
        public async Task When_property_is_required_then_required_attribute_is_rendered_in_Swagger_mode()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithRequiredObject>(new JsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            });
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.True(code.Contains("[System.ComponentModel.DataAnnotations.Required]"));
            Assert.True(code.Contains("public string Property { get; set; }"));
        }

        [Fact]
        public async Task When_property_is_required_then_required_attribute_is_rendered()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithRequiredObject>();
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.True(code.Contains("[System.ComponentModel.DataAnnotations.Required]"));
            Assert.True(code.Contains("public string Property { get; set; }"));
        }

        public class ClassWithoutRequiredObject
        {
            public string Property { get; set; }
        }

        [Fact]
        public async Task When_property_is_not_required_then_required_attribute_is_not_rendered_in_Swagger_mode()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithoutRequiredObject>(new JsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            });
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.False(code.Contains("[Required]"));
            Assert.True(code.Contains("public string Property { get; set; }"));
        }

        [Fact]
        public async Task When_property_is_not_required_then_required_attribute_is_not_rendered()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithoutRequiredObject>();
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.False(code.Contains("[Required]"));
            Assert.True(code.Contains("public string Property { get; set; }"));
        }
    }
}