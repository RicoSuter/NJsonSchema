using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Xunit;

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
            //// Arrange
            var schema = JsonSchema.FromType<EnumKeyDictionaryTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                TypeScriptVersion = 1.8m
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("Mapping: { [key: string]: string; };", code);
            Assert.Contains("Mapping2: { [key: string]: string; };", code);
        }

        [Fact]
        public async Task When_dictionary_key_is_enum_then_typescript_has_enum_key_ts_2_1()
        {
            //// Arrange
            var schema = JsonSchema.FromType<EnumKeyDictionaryTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface, TypeScriptVersion = 2.1m });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("export enum PropertyName {\n    Name = 0,\n    Gender = 1,\n}", code);
            Assert.Contains("Mapping: { [key in keyof typeof PropertyName]?: string; } | undefined;", code);
            Assert.Contains("Mapping2: { [key in keyof typeof PropertyName]?: string; } | undefined;", code);
        }
        
        [Fact]
        public async Task When_dictionary_key_is_string_literal_then_typescript_has_string_literal_key_ts_2_1()
        {
            //// Arrange
            var schema = JsonSchema.FromType<EnumKeyDictionaryTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                EnumNameGenerator = new DefaultEnumNameGenerator(),
                EnumStyle = TypeScriptEnumStyle.StringLiteral, 
                TypeScriptVersion = 2.1m,
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("export type PropertyName = 0 | 1;", code);
            Assert.Contains("Mapping: { [key in PropertyName]?: string; } | undefined;", code);
            Assert.Contains("Mapping2: { [key in PropertyName]?: string; } | undefined;", code);
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
            //// Arrange
            var schema = JsonSchema.FromType<EnumValueDictionaryTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                TypeScriptVersion = 1.8m
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("Mapping: { [key: string]: Gender; };", code);
            Assert.Contains("Mapping2: { [key: string]: Gender; };", code);
        }
    }
}
