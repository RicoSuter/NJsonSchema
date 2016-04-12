using System.Collections.Generic;
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
        public void When_property_is_object_then_jsonProperty_has_no_reference_and_is_any()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<ObjectTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema);
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.Contains("Test?: any;"));
        }

        public class DictionaryObjectTest
        {
            public IDictionary<string, object> Test { get; set; }
        }

        [TestMethod]
        public void When_dictionary_value_is_object_then_typescript_uses_any()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<DictionaryObjectTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema);
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.Contains("Test?: { [key: string] : any; };"));
        }
    }
}
