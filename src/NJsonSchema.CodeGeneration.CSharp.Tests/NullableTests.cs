using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class NullableTests
    {
        private class ClassWithRequiredObject
        {
            public int Property { get; set; }

            [Required]
            public int Property2 { get; set; }
        }

        [Fact]
        public async Task When_property_is_optional_and_GenerateNullableOptionalProperties_is_not_set_then_CSharp_property_is_not_nullable()
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithRequiredObject>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3
            });
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                //GenerateNullableOptionalProperties = false
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public int Property { get; set; }", code);
            Assert.Contains("public int Property2 { get; set; }", code);
        }

        [Fact]
        public async Task When_property_is_optional_and_GenerateNullableOptionalProperties_is_set_then_CSharp_property_is_nullable()
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithRequiredObject>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3
            });
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                GenerateOptionalPropertiesAsNullable = true
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public int? Property { get; set; }", code);
            Assert.Contains("public int Property2 { get; set; }", code);
        }
    }
}
