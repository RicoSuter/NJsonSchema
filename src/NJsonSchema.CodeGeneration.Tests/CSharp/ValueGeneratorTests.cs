using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.CSharp;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    [TestClass]
    public class ValueGeneratorTests
    {
        public class RangeClass
        {
            [Range(2, Double.MaxValue)]
            public int Foo { get; set; }
        }

        [TestMethod]
        public async Task When_schema_contains_range_then_code_is_correctly_generated()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<RangeClass>();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("[System.ComponentModel.DataAnnotations.Range(2, int.MaxValue)]"));
        }
    }
}
