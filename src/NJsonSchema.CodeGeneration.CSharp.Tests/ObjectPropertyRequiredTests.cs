using System.ComponentModel.DataAnnotations;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.NewtonsoftJson.Generation;
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
        public void When_property_is_required_then_required_attribute_is_rendered_in_Swagger_mode()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithRequiredObject>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            });
            var schemaData = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("[System.ComponentModel.DataAnnotations.Required]", code);
            Assert.Contains("public object Property { get; set; }", code);
        }

        [Fact]
        public void When_property_is_required_then_required_attribute_is_rendered()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithRequiredObject>();
            var schemaData = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("[System.ComponentModel.DataAnnotations.Required]", code);
            Assert.Contains("public object Property { get; set; }", code);
        }

        private class ClassWithoutRequiredObject
        {
            public object Property { get; set; }
        }

        [Fact]
        public void When_property_is_not_required_then_required_attribute_is_not_rendered_in_Swagger_mode()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithoutRequiredObject>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            });
            var schemaData = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.DoesNotContain("[Required]", code);
            Assert.Contains("public object Property { get; set; }", code);
        }


        [Fact]
        public void When_property_is_not_required_then_required_attribute_is_not_rendered()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithoutRequiredObject>();
            var schemaData = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.DoesNotContain("[Required]", code);
            Assert.Contains("public object Property { get; set; }", code);
        }
    }
}