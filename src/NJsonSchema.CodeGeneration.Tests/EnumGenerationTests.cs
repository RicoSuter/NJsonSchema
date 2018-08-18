using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests
{
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
        
        [Fact]
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
            Assert.Equal(3, code.Split(new[] { "export enum " }, StringSplitOptions.None).Count()); // two found
        }

        [Fact]
        public async Task When_export_types_is_true_add_export_before_enum_in_typescript()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<StringAndIntegerEnumTestClass>(new JsonSchemaGeneratorSettings());
            var data = schema.ToJson();

            TypeScriptGeneratorSettings typeScriptGeneratorSettings = new TypeScriptGeneratorSettings()
            {
                ExportTypes = true
            };

            //// Act
            var generator = new TypeScriptGenerator(schema, typeScriptGeneratorSettings);
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("export enum", code);
        }

        [Fact]
        public async Task When_add_export_keyword_is_false_dont_add_export_before_enum_in_typescript()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<StringAndIntegerEnumTestClass>(new JsonSchemaGeneratorSettings());
            var data = schema.ToJson();

            TypeScriptGeneratorSettings typeScriptGeneratorSettings = new TypeScriptGeneratorSettings()
            {
                ExportTypes = false
            };

            //// Act
            var generator = new TypeScriptGenerator(schema, typeScriptGeneratorSettings);
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.DoesNotContain("export enum", code);
        }

        [Fact]
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
            var code = generator.GenerateFile("MyClass").Replace("\r", "");

            //// Assert
            Assert.DoesNotContain("Ref_", code);
            Assert.Contains("public enum Bar\n", code);
            Assert.Contains("public enum Bar2\n", code);

            Assert.Contains(" B = 5,", code); // B must be 5 even if B = 1 is first defined
            Assert.Equal(3, code.Split(new[] { "public enum " }, StringSplitOptions.None).Count()); // two found (one string and one integer based enum)
            Assert.Equal(3, code.Split(new[] { "[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]" }, StringSplitOptions.None).Count()); // two found
        }

        [Fact]
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
        
        [Fact]
        public async Task When_enum_has_string_value_then_CS_code_has_EnumMember_attribute()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithStringEnum>();
            var schemaData = schema.ToJson();
            
            //// Act
            var generator = new CSharpGenerator(schema);
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("[System.Runtime.Serialization.EnumMember(Value = \"0562\")]", code);
            Assert.Contains("_0562 = 0,", code);
            Assert.Contains("[System.Runtime.Serialization.EnumMember(Value = \"0532\")]", code);
            Assert.Contains("_0532 = 1,", code);
        }

        [Fact]
        public async Task When_enum_has_string_value_then_TS_code_has_string_value()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithStringEnum>();
            var schemaData = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema);
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("_0562 = <any>\"0562\", ", code);
            Assert.Contains("_0532 = <any>\"0532\", ", code);
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

        [Fact]
        public async Task When_enum_has_integer_value_then_CS_code_has_EnumMember_attribute()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithIntegerEnum>();
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema);
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.DoesNotContain("[EnumMember(Value = \"0562\")]", code);
            Assert.Contains("_0562 = 10,", code);
            Assert.DoesNotContain("[EnumMember(Value = \"0532\")]", code);
            Assert.Contains("_0532 = 15,", code);
        }

        [Fact]
        public async Task When_enum_has_integer_value_then_TS_code_has_string_value()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithIntegerEnum>();
            var schemaData = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema);
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("_0562 = 10, ", code);
            Assert.Contains("_0532 = 15, ", code);
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

        [Fact]
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
            Assert.Contains("[System.Runtime.Serialization.EnumMember(Value = \"0562\")]", code);
            Assert.Contains("_0562 = 0,", code);
            Assert.Contains("[System.Runtime.Serialization.EnumMember(Value = \"0532\")]", code);
            Assert.Contains("_0532 = 1,", code);
        }

        [Fact]
        public async Task When_property_is_nullable_and_enum_allows_null_then_no_exception_is_thrown()
        {
            //// Arrange
            var json = @"{  
   ""type"":""object"",
   ""properties"":{  
      ""paataenktHandling"":{  
         ""title"":""paataenktHandling"",
         ""description"":""EAID_D38C4D27_B57C_4356_89E1_05E8DA0250B6"",
         ""type"":[  
            ""string"",
            ""null""
         ],
         ""enum"":[  
            ""Ændring"",
            ""Nyoprettelse"",
            ""Udgår"",
            null
         ]
      }
   }
}";
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("Foo");

            //// Assert
            Assert.NotNull(code);
        }
    }
}