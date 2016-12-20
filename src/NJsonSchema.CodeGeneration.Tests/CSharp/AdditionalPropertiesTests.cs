using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.CSharp;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    [TestClass]
    public class AdditionalPropertiesTests
    {
        [TestMethod]
        public void When_additionalProperties_schema_is_set_for_object_then_special_property_is_rendered()
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
            var schema = JsonSchema4.FromJson(json);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("Person");

            //// Assert
            Assert.IsTrue(code.Contains("[Newtonsoft.Json.JsonExtensionData]"));
            Assert.IsTrue(code.Contains("public System.Collections.Generic.IDictionary<string, string> AdditionalProperties"));
        }
    }
}