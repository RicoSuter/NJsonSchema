using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.TypeScript;

namespace NJsonSchema.CodeGeneration.Tests
{
    [TestClass]
    public class TypeScriptGeneratorTests
    {
        [TestMethod]
        public void When_property_name_does_not_match_property_name_then_casing_is_correct_in_output()
        {
            //// Arrange
            var generator = CreateGenerator();

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"lastName?: string;"));
            Assert.IsTrue(output.Contains(@"Dictionary?: { [key: string] : string; };"));
        }

        [TestMethod]
        public void When_property_is_required_name_then_TypeScript_property_is_not_optional()
        {
            //// Arrange
            var generator = CreateGenerator();

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"FirstName: string;"));
        }

        private static TypeScriptInterfaceGenerator CreateGenerator()
        {
            var schema = JsonSchema4.FromType<Person>();
            var schemaData = schema.ToJson();
            var generator = new TypeScriptInterfaceGenerator(schema);
            return generator;
        }
    }
}
