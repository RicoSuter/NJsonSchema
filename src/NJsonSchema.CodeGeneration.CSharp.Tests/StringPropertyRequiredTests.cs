using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class StringPropertyRequiredTests
    {
        private class ClassWithRequiredObject
        {
            [Required]
            public string Property { get; set; }

            [Required(AllowEmptyStrings = true)]
            public string Property2 { get; set; }
        }

        [Fact]
        public async Task When_property_is_required_then_required_attribute_is_rendered_in_Swagger_mode()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithRequiredObject>(new JsonSchemaGeneratorSettings());
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Equal(1, schema.Properties["Property"].MinLength);
            Assert.Null(schema.Properties["Property2"].MinLength);

            Assert.Contains("[System.ComponentModel.DataAnnotations.Required]\n" +
                            "        public string Property { get; set; }\n", code);

            Assert.Contains("[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]\n" +
                            "        public string Property2 { get; set; }\n", code);
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
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.JsonSchema
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("[System.ComponentModel.DataAnnotations.Required]\n" +
                            "        public string Property { get; set; }\n", code);

            Assert.Contains("[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]\n" +
                            "        public string Property2 { get; set; }\n", code);
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
            Assert.DoesNotContain("[Required]", code);
            Assert.Contains("public string Property { get; set; }", code);
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
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.JsonSchema
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.DoesNotContain("[Required]", code);
            Assert.Contains("public string Property { get; set; }", code);
        }
    }
}