using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.Generation;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class DictionaryTests
    {
        public class AnyDictionary : Dictionary<string, object>
        {
            public string Foo { get; set; }
        }

        public class StringDictionary : Dictionary<string, string>
        {
            public string Foo { get; set; }
        }

        [TestMethod]
        public async Task When_class_inherits_from_any_dictionary_then_interface_has_indexer_property()
        {
            //// Arrange
            var schemaGenerator = new JsonSchemaGenerator(new JsonSchemaGeneratorSettings
            {
                NullHandling = NullHandling.Swagger
            });
            var schema = await schemaGenerator.GenerateAsync(typeof(AnyDictionary));
            var json = schema.ToJson();

            //// Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            var code = codeGenerator.GenerateFile("MetadataDictionary");

            //// Assert
            Assert.IsFalse(code.Contains("extends { [key: string] : any; }"));
            Assert.IsTrue(code.Contains("[key: string]: any; "));
        }

        [TestMethod]
        public async Task When_class_inherits_from_any_dictionary_then_class_has_indexer_property()
        {
            //// Arrange
            var schemaGenerator = new JsonSchemaGenerator(new JsonSchemaGeneratorSettings
            {
                NullHandling = NullHandling.Swagger
            });
            var schema = await schemaGenerator.GenerateAsync(typeof(AnyDictionary));
            var json = schema.ToJson();

            //// Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });
            var code = codeGenerator.GenerateFile("MetadataDictionary");

            //// Assert
            Assert.IsFalse(code.Contains("extends { [key: string] : any; }"));
            Assert.IsFalse(code.Contains("super()"));
            Assert.IsTrue(code.Contains("[key: string]: any; "));
        }

        [TestMethod]
        public async Task When_class_inherits_from_string_dictionary_then_interface_has_indexer_property()
        {
            //// Arrange
            var schemaGenerator = new JsonSchemaGenerator(new JsonSchemaGeneratorSettings
            {
                NullHandling = NullHandling.Swagger
            });
            var schema = await schemaGenerator.GenerateAsync(typeof(StringDictionary));
            var json = schema.ToJson();

            //// Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            var code = codeGenerator.GenerateFile("MetadataDictionary");

            //// Assert
            Assert.IsFalse(code.Contains("extends { [key: string] : string; }"));
            Assert.IsTrue(code.Contains("[key: string]: string; "));
        }

        [TestMethod]
        public async Task When_class_inherits_from_string_dictionary_then_class_has_indexer_property()
        {
            //// Arrange
            var schemaGenerator = new JsonSchemaGenerator(new JsonSchemaGeneratorSettings
            {
                NullHandling = NullHandling.Swagger
            });
            var schema = await schemaGenerator.GenerateAsync(typeof(StringDictionary));
            var json = schema.ToJson();

            //// Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });
            var code = codeGenerator.GenerateFile("MetadataDictionary");

            //// Assert
            Assert.IsFalse(code.Contains("extends { [key: string] : string; }"));
            Assert.IsFalse(code.Contains("super()"));
            Assert.IsTrue(code.Contains("[key: string]: string; "));
        }
    }
}
