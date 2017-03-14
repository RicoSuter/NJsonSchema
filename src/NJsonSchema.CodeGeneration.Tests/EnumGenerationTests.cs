using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
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
        public async Task When_string_and_integer_enum_used_then_two_enums_are_generated_in_typescript()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<StringAndIntegerEnumTestClass>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema);
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.AreEqual(3, code.Split(new[] { "export enum " }, StringSplitOptions.None).Count()); // two found
        }

        [TestMethod]
        public async Task When_string_and_integer_enum_used_then_one_enum_is_generated_in_CSharp()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<StringAndIntegerEnumTestClass>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var data = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema);
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsFalse(code.Contains("Ref_"));
            Assert.IsTrue(code.Contains("public enum Bar\r\n"));
            Assert.IsTrue(code.Contains("public enum Bar2\r\n"));

            Assert.IsTrue(code.Contains(" B = 5,")); // B must be 5 even if B = 1 is first defined
            Assert.AreEqual(3, code.Split(new[] { "public enum " }, StringSplitOptions.None).Count()); // two found (one string and one integer based enum)
            Assert.AreEqual(3, code.Split(new[] { "[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]" }, StringSplitOptions.None).Count()); // two found
        }

        [TestMethod]
        public async Task When_byte_enum_is_generated_then_no_exception_occurs()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<DifferentEnumTypeTestClass>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var data = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema);
            var code = generator.GenerateFile("MyClass");

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
        
        [TestMethod]
        public async Task When_enum_has_string_value_then_CS_code_has_EnumMember_attribute()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithStringEnum>();
            var schemaData = schema.ToJson();
            
            //// Act
            var generator = new CSharpGenerator(schema);
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("[System.Runtime.Serialization.EnumMember(Value = \"0562\")]"));
            Assert.IsTrue(code.Contains("_0562 = 0,"));
            Assert.IsTrue(code.Contains("[System.Runtime.Serialization.EnumMember(Value = \"0532\")]"));
            Assert.IsTrue(code.Contains("_0532 = 1,"));
        }

        [TestMethod]
        public async Task When_enum_has_string_value_then_TS_code_has_string_value()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithStringEnum>();
            var schemaData = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema);
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("_0562 = <any>\"0562\", "));
            Assert.IsTrue(code.Contains("_0532 = <any>\"0532\", "));
        }

        public class ClassWithStringEnum
        {
            public StringEnum Bar { get; set; }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum StringEnum
        {

            [EnumMember(Value = "0562")]
            _0562,

            [EnumMember(Value = "0532")]
            _0532
        }

        [TestMethod]
        public async Task When_enum_has_integer_value_then_CS_code_has_EnumMember_attribute()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithIntegerEnum>();
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema);
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsFalse(code.Contains("[EnumMember(Value = \"0562\")]"));
            Assert.IsTrue(code.Contains("_0562 = 10,"));
            Assert.IsFalse(code.Contains("[EnumMember(Value = \"0532\")]"));
            Assert.IsTrue(code.Contains("_0532 = 15,"));
        }

        [TestMethod]
        public async Task When_enum_has_integer_value_then_TS_code_has_string_value()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithIntegerEnum>();
            var schemaData = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema);
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("_0562 = 10, "));
            Assert.IsTrue(code.Contains("_0532 = 15, "));
        }

        public class ClassWithIntegerEnum
        {
            public NumberEnum Bar { get; set; }
        }

        public enum NumberEnum
        {
            _0562 = 10,
            _0532 = 15
        }




        [TestMethod]
        public async Task When_enum_has_no_names_and_string_value_starts_with_number_then_underline_is_generated()
        {
            //// Arrange
            var schemaData = @"{
  ""type"": ""object"",
  ""properties"": {
    ""Bar"": {
      ""oneOf"": [
        {
          ""$ref"": ""#/definitions/StringEnum""
        }
      ]
    }
  },
  ""definitions"": {
    ""StringEnum"": {
      ""type"": ""string"",
      ""enum"": [
        ""0562"",
        ""0532""
      ],
      ""description"": """"
    }
  }
}";
            var schema = await JsonSchema4.FromJsonAsync(schemaData);

            //// Act
            var generator = new CSharpGenerator(schema);
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("[System.Runtime.Serialization.EnumMember(Value = \"0562\")]"));
            Assert.IsTrue(code.Contains("_0562 = 0,"));
            Assert.IsTrue(code.Contains("[System.Runtime.Serialization.EnumMember(Value = \"0532\")]"));
            Assert.IsTrue(code.Contains("_0532 = 1,"));
        }
    }
}