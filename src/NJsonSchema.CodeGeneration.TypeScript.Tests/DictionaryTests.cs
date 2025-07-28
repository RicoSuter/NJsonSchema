using NJsonSchema.CodeGeneration.Tests;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;

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
            // Arrange
            var schemaGenerator = new JsonSchemaGenerator(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            });

            var schema = schemaGenerator.Generate(typeof(AnyDictionary));
            var json = schema.ToJson();

            // Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            var code = codeGenerator.GenerateFile("MetadataDictionary");

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_class_inherits_from_any_dictionary_then_class_has_indexer_property()
        {
            // Arrange
            var schemaGenerator = new JsonSchemaGenerator(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            });

            var schema = schemaGenerator.Generate(typeof(AnyDictionary));
            var json = schema.ToJson();

            // Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });
            var code = codeGenerator.GenerateFile("MetadataDictionary");

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_class_inherits_from_string_dictionary_then_interface_has_indexer_property()
        {
            // Arrange
            var schemaGenerator = new JsonSchemaGenerator(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            });

            var schema = schemaGenerator.Generate(typeof(StringDictionary));
            var json = schema.ToJson();

            // Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            var code = codeGenerator.GenerateFile("MetadataDictionary");

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_class_inherits_from_string_dictionary_then_class_has_indexer_property()
        {
            // Arrange
            var schemaGenerator = new JsonSchemaGenerator(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            });

            var schema = schemaGenerator.Generate(typeof(StringDictionary));
            var json = schema.ToJson();

            // Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });
            var code = codeGenerator.GenerateFile("MetadataDictionary");

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_property_is_dto_dictionary_then_assignment_may_create_new_instance()
        {
            // Arrange
            var json = @"{
    ""required"": [ ""resource"" ],
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
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                NullValue = TypeScriptNullValue.Null,
                TypeScriptVersion = 1.8m
            });
            var code = codeGenerator.GenerateFile("Test");

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_property_is_object_and_not_dictionary_it_should_be_assigned_in_init_method()
        {
            // Arrange
            var json = @"{
    ""properties"": {
        ""resource"": {
            ""type"": ""object""
        }
    }
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                NullValue = TypeScriptNullValue.Null
            });
            var code = codeGenerator.GenerateFile("Test");

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_property_is_string_dictionary_then_assignment_is_correct()
        {
            // Arrange
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
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                NullValue = TypeScriptNullValue.Undefined,
                TypeScriptVersion = 1.8m
            });
            var code = codeGenerator.GenerateFile("Test");

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }

        public class DictionaryContainer
        {
            public DisplayValueDictionary Foo { get; set; }
        }

        public class DisplayValueDictionary : Dictionary<string, string>
        {
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(true, false)]
        public async Task When_property_uses_custom_dictionary_class_then_class_is_generated(bool inlineNamedDictionaries, bool convertConstructorInterfaceData)
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DictionaryContainer>();
            var json = schema.ToJson();

            // Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                NullValue = TypeScriptNullValue.Undefined,
                ConvertConstructorInterfaceData = convertConstructorInterfaceData,
                InlineNamedDictionaries = inlineNamedDictionaries,
                TypeScriptVersion = 1.8m
            });
            var code = codeGenerator.GenerateFile("Test");

            // Assert
            await VerifyHelper.Verify(code).UseParameters(inlineNamedDictionaries, convertConstructorInterfaceData);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_dictionary_has_arbitrary_nonenum_key_then_generated_typescript_uses_plain_string_key()
        {
            // Arrange
            var json = @"{
    ""properties"": {
        ""myDict"": {
            ""type"": ""object"",
            ""additionalProperties"": {
                ""$ref"": ""#/definitions/myItem""
            }
        }
    },
    ""definitions"": {
        ""myItem"": {
            ""type"": ""object"",
            ""additionalProperties"": false,
            ""properties"": {
                ""name"": {
                    ""type"": ""string""
                },
                ""age"": {
                    ""type"": ""integer""
                }
            }
        }
    }
}";

            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var codeGenerator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                ConvertConstructorInterfaceData = true,
                TypeStyle = TypeScriptTypeStyle.Class,
                TypeScriptVersion = 2.7m
            });

            var code = codeGenerator.GenerateFile("Test");

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }
    }
}
