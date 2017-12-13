using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class ObjectPropertyRequiredTests
    {
        private class ClassWithRequiredObject
        {
            [Required]
            public object Property { get; set; }
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
            Assert.Contains("[System.ComponentModel.DataAnnotations.Required]", code);
            Assert.Contains("public object Property { get; set; }", code);
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
            Assert.Contains("[System.ComponentModel.DataAnnotations.Required]", code);
            Assert.Contains("public object Property { get; set; }", code);
        }

        private class ClassWithoutRequiredObject
        {
            public object Property { get; set; }
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
            Assert.DoesNotContain("[Required]", code);
            Assert.Contains("public object Property { get; set; }", code);
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
            Assert.DoesNotContain("[Required]", code);
            Assert.Contains("public object Property { get; set; }", code);
        }
    }
}