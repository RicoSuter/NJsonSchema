using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.CodeGeneration.TypeScript;
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
            var schema = await JsonSchema4.FromTypeAsync<EnumKeyDictionaryTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("Mapping: { [key: string] : string; };", code);
            Assert.Contains("Mapping2: { [key: string] : string; };", code);
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
            var schema = await JsonSchema4.FromTypeAsync<EnumValueDictionaryTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("Mapping: { [key: string] : Gender; };", code);
            Assert.Contains("Mapping2: { [key: string] : Gender; };", code);
        }
    }
}
