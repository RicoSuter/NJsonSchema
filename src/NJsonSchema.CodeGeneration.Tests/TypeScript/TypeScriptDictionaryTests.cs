using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.CodeGeneration.TypeScript;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
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

        [TestMethod]
        public void When_dictionary_key_is_enum_then_typescript_has_string_key()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<EnumKeyDictionaryTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema);
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.Contains("Mapping?: { [key: string] : string; };"));
            Assert.IsTrue(code.Contains("Mapping2?: { [key: string] : string; };"));
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

        [TestMethod]
        public void When_dictionary_value_is_enum_then_typescript_has_enum_value()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<EnumValueDictionaryTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema);
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.Contains("Mapping?: { [key: string] : Gender; };"));
            Assert.IsTrue(code.Contains("Mapping2?: { [key: string] : Gender; };"));
        }
    }
}
