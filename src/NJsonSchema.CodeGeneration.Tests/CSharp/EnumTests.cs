using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.CSharp;
using System.Threading.Tasks;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    [TestClass]
    public class EnumTests
    {
        [TestMethod]
        public async Task When_type_name_hint_has_generics_then_they_are_converted()
        {
            /// Arrange
            var json = @"
{
    ""properties"": {
        ""foo"": {
            ""$ref"": ""#/definitions/FirstMetdod<MetValue>""
        }
    },
    ""definitions"": {
        ""FirstMetdod<MetValue>"": {
            ""type"": ""object"",
            ""properties"": {
            ""GroupChar"": {
                ""type"": ""string"",
                    ""enum"": [
                    ""A"",
                    ""B"",
                    ""C"",
                    ""D""
                    ]
                }
            }
        }
    }
}";
            /// Act
            var schema = await JsonSchema4.FromJsonAsync(json);

            var settings = new CSharpGeneratorSettings();
            var generator = new CSharpGenerator(schema, settings);

            var code = generator.GenerateFile("Foo");

            /// Assert
            Assert.IsTrue(code.Contains("public enum FirstMetdodOfMetValueGroupChar"));
        }
    }
}
