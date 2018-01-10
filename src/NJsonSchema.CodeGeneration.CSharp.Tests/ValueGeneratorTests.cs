using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using Xunit;

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
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<RangeClass>();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("[System.ComponentModel.DataAnnotations.Range(2, int.MaxValue)]", code);
        }

        [Fact]
        public async Task When_property_is_integer_and_no_format_is_available_then_default_value_is_int32()
        {
            /// Arrange
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
            var schema = await JsonSchema4.FromJsonAsync(json);

            /// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            /// Assert
            Assert.Contains("public int? PageSize { get; set; } = 10;", code);
        }
    }
}
