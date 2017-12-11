using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class AdditionalPropertiesTests
    {
        [Fact]
        public async Task When_additionalProperties_schema_is_set_for_object_then_special_property_is_rendered()
        {
            //// Arrange
            var json =
@"{ 
    ""properties"": {
        ""Pet"": {
            ""type"": ""object"",
            ""properties"": {
                ""id"": {
                    ""type"": ""integer"",
                    ""format"": ""int64""
                },
                ""category"": {
                    ""type"": ""string""
                }
            },
            ""additionalProperties"": {
                ""type"": ""string""
            }
        }
    }
}";
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("Person");

            //// Assert
            Assert.True(code.Contains("[Newtonsoft.Json.JsonExtensionData]"));
            Assert.True(code.Contains("public System.Collections.Generic.IDictionary<string, string> AdditionalProperties"));
        }
    }
}