using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.CSharp;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    [TestClass]
    public class AllOfTests
    {
        [TestMethod]
        public async Task When_allOf_has_two_schemas_then_referenced_schema_is_inherited()
        {
            //// Arrange
            var json =
@"{
    ""allOf"": [
        {
            ""$ref"": ""#/definitions/B""
        },
        {
            ""type"": ""object"", 
            ""required"": [
                ""prop""
            ],
            ""properties"": {
                ""prop"": {
                    ""type"": ""string"",
                    ""minLength"": 1,
                    ""maxLength"": 30
                },
                ""prop2"": {
                    ""type"": ""string""
                }
            }
        }
    ],
    ""definitions"": {
        ""B"": {
            ""properties"": {
                ""foo"": {
                    ""type"": ""string""
                }
            }
        }
    }
}";
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("A");

            //// Assert
            Assert.IsFalse(code.Contains("Anonymous"));
            Assert.IsTrue(code.Contains("A : B"));
        }

        [TestMethod]
        public async Task When_allOf_has_one_schema_then_it_is_inherited()
        {
            //// Arrange
            var json =
@"{
    ""type"": ""object"",
    ""discriminator"": ""type"",
    ""required"": [
        ""prop"", 
        ""type""
    ],
    ""properties"": {
        ""prop"": {
            ""type"": ""string"",
            ""minLength"": 1,
            ""maxLength"": 30
        },
        ""prop2"": {
            ""type"": ""string""
        },
        ""type"": {
            ""type"": ""string""
        }
    },
    ""allOf"": [
        {
            ""$ref"": ""#/definitions/B""
        }
    ],
    ""definitions"": {
        ""B"": {
            ""type"": ""object"",
            ""properties"": {
                ""foo"": {
                    ""type"": ""string""
                }
            }
        }
    }
}";
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("A");

            //// Assert
            Assert.IsFalse(code.Contains("Anonymous"));
            Assert.IsTrue(code.Contains("A : B"));
        }
    }
}