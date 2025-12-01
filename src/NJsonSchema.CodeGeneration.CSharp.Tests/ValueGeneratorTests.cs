using System.ComponentModel.DataAnnotations;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp.Tests;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class ValueGeneratorTests
    {
        public class RangeClass
        {
            [Range(2, Double.MaxValue)]
            public int Foo { get; set; }
        }

        [Fact]
        public async Task When_schema_contains_range_then_code_is_correctly_generated()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<RangeClass>();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Theory]
        [InlineData("integer", JsonFormatStrings.Integer)]
        [InlineData("integer", JsonFormatStrings.Long)]
        [InlineData("integer", JsonFormatStrings.ULong)]
        [InlineData("number", JsonFormatStrings.Float)]
        [InlineData("number", JsonFormatStrings.Double)]
        [InlineData("number", JsonFormatStrings.Decimal)]
        public async Task When_schema_contains_range_and_format_then_code_is_correctly_generated(string propertyType, string propertyFormat)
        {
            // Arrange
            var json = $$"""
                         {
                             "type": "object",
                             "properties": {
                                "pageSize": {
                                 	"type": "{{propertyType}}",
                                 	"format": "{{propertyFormat}}",
                                 	"minimum": 1
                                },
                                 }
                             }
                         """;
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code).UseParameters(propertyType, propertyFormat);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_property_is_integer_and_no_format_is_available_then_default_value_is_int32()
        {
            // Arrange
            var json = @"{
                ""type"": ""object"",
                ""properties"": {
	                ""pageSize"": {
		                ""type"": ""integer"",
		                ""default"": 10,
		                ""minimum"": 1
	                },
	                ""pagingSize"": {
		                ""type"": ""integer"",
		                ""default"": 5,
		                ""minimum"": 1
	                }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_property_is_string_and_format_is_date_time_then_assign_default_value()
        {
            // Arrange
            var json = @"{
                ""type"": ""object"",
                ""properties"": {
	                ""dateTime"": {
		                ""type"": ""string"",
		                ""format"": ""date-time"",
		                ""default"": ""31.12.9999 23:59:59""
	                }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                DateTimeType = "System.DateTime"
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }
    }
}
