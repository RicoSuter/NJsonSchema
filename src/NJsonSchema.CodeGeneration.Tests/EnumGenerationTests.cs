using System;
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
    public class EnumGenerationTests
    {
        public class StringAndIntegerEnumTestClass
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public Bar Bar1 { get; set; }

            public Bar Bar2 { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public Bar Bar3 { get; set; }

        }
        
        public enum Bar
        {
            A = 0,
            B = 5,
            C = 6,
        }
        
        [TestMethod]
        public void When_string_and_integer_enum_used_then_two_enums_are_generated_in_typescript()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<StringAndIntegerEnumTestClass>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema);
            var code = generator.GenerateFile();

            //// Assert
            Assert.AreEqual(3, code.Split(new[] { "export enum " }, StringSplitOptions.None).Count()); // two found
        }

        [TestMethod]
        public void When_string_and_integer_enum_used_then_one_enum_is_generated_in_CSharp()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<StringAndIntegerEnumTestClass>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var data = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema);
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.IndexOf("B = 5,") > 0); // B must be 5 even if B = 1 is first defined
            Assert.AreEqual(2, code.Split(new[] { "public enum " }, StringSplitOptions.None).Count()); // one found
            Assert.AreEqual(3, code.Split(new[] { "[JsonConverter(typeof(StringEnumConverter))]" }, StringSplitOptions.None).Count()); // two found
        }

        [TestMethod]
        public void When_byte_enum_is_generated_then_no_exception_occurs()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<DifferentEnumTypeTestClass>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var data = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema);
            var code = generator.GenerateFile();

            //// Assert
            // No exception
        }

        public class DifferentEnumTypeTestClass
        {
            public ByteBar Bar1 { get; set; }
        }

        public enum ByteBar : byte
        {
            A = 0,
            B = 5,
            C = 6,
        }
    }
}