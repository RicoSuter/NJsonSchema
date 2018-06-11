using System.Collections.Generic;
using System.Threading.Tasks;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
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

        [Fact]
        public async Task When_class_inherits_from_any_dictionary_then_interface_has_indexer_property()
        {
            //// Arrange
            var schemaGenerator = new JsonSchemaGenerator(new JsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            });
            var schema = await schemaGenerator.GenerateAsync(typeof(AnyDictionary));
            var json = schema.ToJson();

            //// Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            var code = codeGenerator.GenerateFile("MetadataDictionary");

            //// Assert
            Assert.DoesNotContain("extends { [key: string] : any; }", code);
            Assert.Contains("[key: string]: any; ", code);
        }

        [Fact]
        public async Task When_class_inherits_from_any_dictionary_then_class_has_indexer_property()
        {
            //// Arrange
            var schemaGenerator = new JsonSchemaGenerator(new JsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            });
            var schema = await schemaGenerator.GenerateAsync(typeof(AnyDictionary));
            var json = schema.ToJson();

            //// Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });
            var code = codeGenerator.GenerateFile("MetadataDictionary");

            //// Assert
            Assert.DoesNotContain("extends { [key: string] : any; }", code);
            Assert.DoesNotContain("super()", code);
            Assert.Contains("[key: string]: any; ", code);
        }

        [Fact]
        public async Task When_class_inherits_from_string_dictionary_then_interface_has_indexer_property()
        {
            //// Arrange
            var schemaGenerator = new JsonSchemaGenerator(new JsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            });
            var schema = await schemaGenerator.GenerateAsync(typeof(StringDictionary));
            var json = schema.ToJson();

            //// Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            var code = codeGenerator.GenerateFile("MetadataDictionary");

            //// Assert
            Assert.DoesNotContain("extends { [key: string] : string; }", code);
            Assert.Contains("[key: string]: string | any; ", code);
        }

        [Fact]
        public async Task When_class_inherits_from_string_dictionary_then_class_has_indexer_property()
        {
            //// Arrange
            var schemaGenerator = new JsonSchemaGenerator(new JsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            });
            var schema = await schemaGenerator.GenerateAsync(typeof(StringDictionary));
            var json = schema.ToJson();

            //// Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });
            var code = codeGenerator.GenerateFile("MetadataDictionary");

            //// Assert
            Assert.DoesNotContain("extends { [key: string] : string; }", code);
            Assert.DoesNotContain("super()", code);
            Assert.Contains("[key: string]: string | any; ", code);
        }

        [Fact]
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
            Assert.Contains("this.resource[key] = data[\"resource\"][key] ? MyItem.fromJS(data[\"resource\"][key]) : new MyItem();", code);
        }

        [Fact]
        public async Task When_property_is_object_and_not_dictionary_it_should_be_assigned_in_init_method()
        {
            //// Arrange
            var json = @"{
    ""properties"": {
        ""resource"": {
            ""type"": ""object""
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
            Assert.Contains("resource: any;", code);
            Assert.DoesNotContain("this.resource[key] = data[\"resource\"][key];", code);
            Assert.DoesNotContain(" : new any();", code);
        }

        [Fact]
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
            Assert.Contains("this.resource[key] = data[\"resource\"][key];", code);
        }
    }
}
