using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.Generation;

namespace NJsonSchema.CodeGeneration.Tests
{
    [TestClass]
    public class DefaultGenerationTests
    {
        public class DefaultPropertyGenerationClass
        {
            [DefaultValue("foo")]
            public string Test { get; set; }
        }

        [TestMethod]
        public void When_property_has_default_attribute_then_default_is_in_schema()
        {
            //// Arrange
            
            //// Act
            var schema = JsonSchema4.FromType<DefaultPropertyGenerationClass>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });

            //// Assert
            Assert.AreEqual("foo", schema.Properties["Test"].Default); 
        }

        [TestMethod]
        public void When_property_has_default_attribute_then_default_value_is_set_in_generated_INPC_CSharp_code()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<DefaultPropertyGenerationClass>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });

            //// Act
            var generator = new CSharpGenerator(schema);
            generator.Settings.ClassStyle = CSharpClassStyle.Inpc;
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("private string _test = \"foo\";"));
        }

        [TestMethod]
        public void When_property_has_default_attribute_then_default_value_is_set_in_generated_Poco_CSharp_code()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<DefaultPropertyGenerationClass>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });

            //// Act
            var generator = new CSharpGenerator(schema);
            generator.Settings.ClassStyle = CSharpClassStyle.Poco;
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("public string Test { get; set; } = \"foo\";"));
        }
    }
}