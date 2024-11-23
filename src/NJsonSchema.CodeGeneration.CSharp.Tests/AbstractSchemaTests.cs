using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp.Generation
{
    public class AbstractSchemaTests
    {
        public abstract class AbstractClass
        {
            public string Foo { get; set; }
        }

        [Fact]
        public void When_class_is_abstract_then_is_abstract_CSharp_keyword_is_generated()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AbstractClass>();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("AbstractClass");

            // Assert
            Assert.Contains("public abstract partial class AbstractClass", code);
        }
    }
}
