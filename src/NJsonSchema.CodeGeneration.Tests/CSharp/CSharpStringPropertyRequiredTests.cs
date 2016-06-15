using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    [TestClass]
    public class CSharpStringPropertyRequiredTests
    {
        private class ClassWithRequiredObject
        {
            [Required]
            public string Property { get; set; }
        }

        [TestMethod]
        public void When_property_is_required_then_required_attribute_is_rendered_in_Swagger_mode()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<ClassWithRequiredObject>(new JsonSchemaGeneratorSettings
            {
                NullHandling = NullHandling.Swagger
            });
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                NullHandling = NullHandling.Swagger
            });
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.Contains("[Required]"));
            Assert.IsTrue(code.Contains("public string Property { get; set; }"));
        }

        [TestMethod]
        public void When_property_is_required_then_required_attribute_is_rendered()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<ClassWithRequiredObject>();
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco
            });
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.Contains("[Required]"));
            Assert.IsTrue(code.Contains("public string Property { get; set; }"));
        }

        public class ClassWithoutRequiredObject
        {
            public string Property { get; set; }
        }

        [TestMethod]
        public void When_property_is_not_required_then_required_attribute_is_not_rendered_in_Swagger_mode()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<ClassWithoutRequiredObject>(new JsonSchemaGeneratorSettings
            {
                NullHandling = NullHandling.Swagger
            });
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                NullHandling = NullHandling.Swagger
            });
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsFalse(code.Contains("[Required]"));
            Assert.IsTrue(code.Contains("public string Property { get; set; }"));
        }


        [TestMethod]
        public void When_property_is_not_required_then_required_attribute_is_not_rendered()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<ClassWithoutRequiredObject>();
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco
            });
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsFalse(code.Contains("[Required]"));
            Assert.IsTrue(code.Contains("public string Property { get; set; }"));
        }
    }
}