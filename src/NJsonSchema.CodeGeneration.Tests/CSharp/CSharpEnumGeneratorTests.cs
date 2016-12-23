using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.CSharp;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    [TestClass]
    public class CSharpEnumGeneratorTests
    {
        [TestMethod]
        public async Task When_enum_has_no_type_then_enum_is_generated()
        {
            //// Arrange
            var json =
            @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""category"" : {
                        ""enum"" : [
                            ""commercial"",
                            ""residential""
                        ]
                    }
                }
            }";
            var schema = await JsonSchema4.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema);

            //// Act
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("public enum MyClassCategory"));
        }
    }
}
