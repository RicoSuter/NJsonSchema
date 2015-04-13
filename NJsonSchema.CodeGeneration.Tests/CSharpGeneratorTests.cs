using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Tests.Conversion;

namespace NJsonSchema.CodeGeneration.Tests
{
    [TestClass]
    public class CSharpGeneratorTests
    {
        [TestMethod]
        public void When_namespace_is_set_then_it_should_appear_in_file()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<MyType>();

            var generator = new CSharpGenerator(schema);
            generator.Namespace = "Foobar";
            
            //// Act
            var output = generator.Generate();
            
            //// Assert
            Assert.IsTrue(output.Contains("namespace Foobar"));
        }
    }
}
