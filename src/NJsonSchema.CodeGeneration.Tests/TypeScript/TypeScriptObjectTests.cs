using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.TypeScript;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class TypeScriptObjectTests
    {
        public class ObjectTest
        {
            public object Test { get; set; }
        }

        [TestMethod]
        public async Task When_property_is_object_then_jsonProperty_has_no_reference_and_is_any()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ObjectTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("Test: any;"));
        }

        public class DictionaryObjectTest
        {
            public IDictionary<string, object> Test { get; set; }
        }

        [TestMethod]
        public async Task When_dictionary_value_is_object_then_typescript_uses_any()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<DictionaryObjectTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("Test: { [key: string] : any; };"));
        }
    }
}
