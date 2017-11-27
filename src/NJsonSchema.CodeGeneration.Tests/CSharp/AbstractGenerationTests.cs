using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.CSharp;
using System.Threading.Tasks;

namespace NJsonSchema.CodeGeneration.Tests.CSharp.Generation
{
    [TestClass]
    public class AbstractGenerationTests
    {
        public abstract class AbstractClass
        {
            public string Foo { get; set; }
        }

        [TestMethod]
        public async Task When_class_is_abstract_then_is_abstract_CSharp_keyword_is_generated()
        {
            /// Arrange
            var schema = await JsonSchema4.FromTypeAsync<AbstractClass>();

            /// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("AbstractClass");

            /// Assert
            Assert.IsTrue(code.Contains("public partial abstract class AbstractClass"));
        }
    }
}
