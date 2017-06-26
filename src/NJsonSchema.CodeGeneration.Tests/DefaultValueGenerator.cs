using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.Generation;

namespace NJsonSchema.CodeGeneration.Tests
{
    [TestClass]
    public class DefaultValueGenerator
    {
        private CSharpDefaultValueGenerator csharpGenerator;
        private TypeScriptDefaultValueGenerator typescriptGenerator;

        [TestInitialize]
        public void Init()
        {
            var csharpSettings = new CSharpGeneratorSettings();
            csharpGenerator = new CSharpDefaultValueGenerator(new CSharpTypeResolver(csharpSettings, new object()), csharpSettings);

            var typescriptSettings = new TypeScriptGeneratorSettings();
            typescriptGenerator = new TypeScriptDefaultValueGenerator(new TypeScriptTypeResolver(typescriptSettings, new object()));
        }

        [TestMethod]
        public void When_schema_has_default_value_of_int_it_is_generated_in_CSharp_and_TypeScript_correctly()
        {
            //// Arrange
            
            //// Act
            var schema = new JsonSchema4()
            {
                Type = JsonObjectType.Integer,
                Default = (int)6
            };
            var csharpValue = csharpGenerator.GetDefaultValue(schema, true, "int", "int", true);
            var typescriptValue = typescriptGenerator.GetDefaultValue(schema, true, "int", "int", true);

            //// Assert
            Assert.AreEqual("6", csharpValue);
            Assert.AreEqual("6", typescriptValue);
        }

        [TestMethod]
        public void When_schema_has_default_value_of_long_it_is_generated_in_CSharp_and_TypeScript_correctly()
        {
            //// Arrange

            //// Act
            var schema = new JsonSchema4()
            {
                Type = JsonObjectType.Integer,
                Default = 6000000000L
            };
            var csharpValue = csharpGenerator.GetDefaultValue(schema, true, "long", "long", true);
            var typescriptValue = typescriptGenerator.GetDefaultValue(schema, true, "long", "long", true);

            //// Assert
            Assert.AreEqual("6000000000L", csharpValue);
            Assert.AreEqual("6000000000", typescriptValue);
        }


        [TestMethod]
        public void When_schema_has_default_value_of_float_it_is_generated_in_CSharp_and_TypeScript_correctly()
        {
            //// Arrange

            //// Act
            var schema = new JsonSchema4()
            {
                Type = JsonObjectType.Number,
                Default = 1234.567F
            };
            var csharpValue = csharpGenerator.GetDefaultValue(schema, true, "float", "float", true);
            var typescriptValue = typescriptGenerator.GetDefaultValue(schema, true, "float", "float", true);

            //// Assert
            Assert.AreEqual("1234.567F", csharpValue);
            Assert.AreEqual("1234.567", typescriptValue);
        }


        [TestMethod]
        public void When_schema_has_default_value_of_bool_it_is_generated_in_CSharp_and_TypeScript_correctly()
        {
            //// Arrange

            //// Act
            var schema = new JsonSchema4()
            {
                Type = JsonObjectType.Boolean,
                Default = true
            };
            var csharpValue = csharpGenerator.GetDefaultValue(schema, true, "bool", "bool", true);
            var typescriptValue = typescriptGenerator.GetDefaultValue(schema, true, "bool", "bool", true);

            //// Assert
            Assert.AreEqual("true", csharpValue);
            Assert.AreEqual("true", typescriptValue);
        }


        [TestMethod]
        public void When_schema_has_default_value_of_string_it_is_generated_in_CSharp_and_TypeScript_correctly()
        {
            //// Arrange

            //// Act
            var schema = new JsonSchema4()
            {
                Type = JsonObjectType.String,
                Default = "test\\test\"test\r\ntest"
            };
            var csharpValue = csharpGenerator.GetDefaultValue(schema, true, "string", "string", true);
            var typescriptValue = typescriptGenerator.GetDefaultValue(schema, true, "string", "string", true);

            //// Assert
            Assert.AreEqual("\"test\\\\test\\\"test\\r\\ntest\"", csharpValue);
            Assert.AreEqual("\"test\\\\test\\\"test\\r\\ntest\"", typescriptValue);
        }

    }
}
