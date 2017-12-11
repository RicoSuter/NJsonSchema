using System.Collections.Generic;
using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.TypeScript;
using Xunit;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class TypeScriptObjectTests
    {
        public class ObjectTest
        {
            public object Test { get; set; }
        }

        [Fact]
        public async Task When_property_is_object_then_jsonProperty_has_no_reference_and_is_any()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ObjectTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("Test: any;", code);
        }

        public class DictionaryObjectTest
        {
            public IDictionary<string, object> Test { get; set; }
        }

        [Fact]
        public async Task When_dictionary_value_is_object_then_typescript_uses_any()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<DictionaryObjectTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("Test: { [key: string] : any; };", code);
        }
    }
}
