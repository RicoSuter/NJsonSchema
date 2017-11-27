using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.TypeScript;
using System.Threading.Tasks;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript.Generation
{
    [TestClass]
    public class AbstractGenerationTests
    {
        public abstract class AbstractClass
        {
            public string Foo { get; set; }
        }

        [TestMethod]
        public async Task When_class_is_abstract_then_is_abstract_TypeScript_keyword_is_generated()
        {
            /// Arrange
            var schema = await JsonSchema4.FromTypeAsync<AbstractClass>();

            /// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m });
            var code = generator.GenerateFile("AbstractClass");

            /// Assert
            Assert.IsTrue(code.Contains("export abstract class AbstractClass"));
        }
    }
}
