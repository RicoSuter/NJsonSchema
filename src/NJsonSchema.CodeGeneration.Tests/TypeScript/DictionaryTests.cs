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
            Assert.IsTrue(code.Contains("[key: string]: string | any; "));
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
            Assert.IsTrue(code.Contains("[key: string]: string | any; "));
        }

        [TestMethod]
        public async Task When_property_is_dto_dictionary_then_assignment_may_create_new_instance()
        {
            //// Arrange
            var json = @"{
    ""properties"": {
        ""resource"": {
            ""type"": ""object"",
            ""additionalProperties"": {
                ""$ref"": ""#/definitions/myItem""
            }
        }
    },
    ""definitions"": {
        ""myItem"": {
            ""type"": ""object"",
            ""properties"": {
                ""x"": { ""type"": ""number"" }
            }
        }
    }
}";
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                NullValue = TypeScriptNullValue.Null
            });
            var code = codeGenerator.GenerateFile("Test");

            //// Assert
            Assert.IsTrue(code.Contains("this.resource[key] = data[\"resource\"][key] ? MyItem.fromJS(data[\"resource\"][key]) : new MyItem();"));
        }

        [TestMethod]
        public async Task When_property_is_string_dictionary_then_assignment_is_correct()
        {
            //// Arrange
            var json = @"{
    ""properties"": {
        ""resource"": {
            ""type"": ""object"",
            ""additionalProperties"": {
                ""$ref"": ""#/definitions/myItem""
            }
        }
    },
    ""definitions"": {
        ""myItem"": {
            ""type"": ""string""
        }
    }
}";
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                NullValue = TypeScriptNullValue.Undefined
            });
            var code = codeGenerator.GenerateFile("Test");

            //// Assert
            Assert.IsTrue(code.Contains("this.resource[key] = data[\"resource\"][key];"));
        }
    }
}
