using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.CodeGeneration.Tests;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class TypeScriptDictionaryTests
    {
        public enum PropertyName
        {
            Name,
            Gender
        }

        public class EnumKeyDictionaryTest
        {
            public Dictionary<PropertyName, string> Mapping { get; set; }

            public IDictionary<PropertyName, string> Mapping2 { get; set; }
        }

        [Fact]
        public async Task When_dictionary_key_is_enum_then_typescript_has_string_key()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumKeyDictionaryTest>();
            var data = schema.ToJson();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                TypeScriptVersion = 1.8m
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_dictionary_key_is_enum_then_typescript_has_enum_key_ts_2_1()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumKeyDictionaryTest>();
            var data = schema.ToJson();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface, TypeScriptVersion = 2.1m });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }
        
        [Fact]
        public async Task When_dictionary_key_is_string_literal_then_typescript_has_string_literal_key_ts_2_1()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumKeyDictionaryTest>();
            var data = schema.ToJson();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                EnumNameGenerator = new DefaultEnumNameGenerator(),
                EnumStyle = TypeScriptEnumStyle.StringLiteral, 
                TypeScriptVersion = 2.1m,
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum Gender
        {
            Male,
            Female
        }

        public class EnumValueDictionaryTest
        {
            public Dictionary<string, Gender> Mapping { get; set; }

            public IDictionary<string, Gender> Mapping2 { get; set; }
        }

        [Fact]
        public async Task When_dictionary_value_is_enum_then_typescript_has_enum_value()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumValueDictionaryTest>();
            var data = schema.ToJson();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                TypeScriptVersion = 1.8m
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }

        public class ObjectValueDictionaryTest
        {
            public Dictionary<string, object> Mapping { get; set; }

            public IDictionary<string, object> Mapping2 { get; set; }
        }

        [Fact]
        public async Task When_dictionary_value_is_object_then_typescript_has_any_value()
        {
            // Arrange
            var json = @"
{
   ""required"": [ ""resource"" ],
    ""properties"": {
        ""resource"": {
            ""$ref"": ""#/definitions/myItem""
        }
    },
    ""definitions"": {
      ""myItem"": {
        ""type"": ""object"",
        ""properties"": {
          ""extensions"": {
            ""type"": ""object"",
            ""additionalProperties"": {
              ""type"": ""object"",
              ""additionalProperties"": false
            },
            ""nullable"": true,
            ""readOnly"": true
          }
        },
        ""additionalProperties"": false
      }
    }
}";
            var schema = await JsonSchema.FromJsonAsync(json);
            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                TypeScriptVersion = 2.7m,
                ConvertConstructorInterfaceData = true,
                GenerateConstructorInterface = true
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }
    }
}
