using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class AbstractGenerationTests
    {
        public abstract class AbstractClass
        {
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_class_is_abstract_then_is_abstract_TypeScript_keyword_is_generated()
        {
            /// Arrange
            var schema = await JsonSchema4.FromTypeAsync<AbstractClass>();

            /// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m });
            var code = generator.GenerateFile("AbstractClass");

            /// Assert
            Assert.Contains("export abstract class AbstractClass", code);
        }
    }
}
