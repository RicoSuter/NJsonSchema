using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Infrastructure;
using NJsonSchema.NewtonsoftJson.Converters;
using NJsonSchema.NewtonsoftJson.Generation;
using System.Runtime.Serialization;
using NJsonSchema.CodeGeneration.Tests;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class InheritanceTests
    {
        public class MyContainer
        {
            public EmptyClassInheritingDictionary CustomDictionary { get; set; }
        }

        /// <summary>
        /// Foobar.
        /// </summary>
        public sealed class EmptyClassInheritingDictionary : Dictionary<string, object>
        {
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(true, false)]
        public async Task When_empty_class_inherits_from_dictionary_then_allOf_inheritance_still_works(bool inline, bool convert)
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyContainer>();
            var data = schema.ToJson();

            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeScriptVersion = 2.0m,
                InlineNamedDictionaries = inline,
                ConvertConstructorInterfaceData = convert
            });

            // Act
            var code = generator.GenerateFile("ContainerClass");

            // Assert
            var dschema = schema.Definitions["EmptyClassInheritingDictionary"];

            Assert.Empty(dschema.AllOf);
            Assert.True(dschema.IsDictionary);

            await VerifyHelper.Verify(code).UseParameters(inline, convert);
            TypeScriptCompiler.AssertCompile(code);
        }

        [KnownType(typeof(MyException))]
        [JsonConverter(typeof(JsonInheritanceConverter), "kind")]
        public class ExceptionBase : Exception
        {
            public string Foo { get; set; }
        }

        /// <summary>
        /// Foobar.
        /// </summary>
        public class MyException : ExceptionBase
        {
            public string Bar { get; set; }
        }

        public class ExceptionContainer
        {
            public ExceptionBase Exception { get; set; }
        }

#if !NETFRAMEWORK
        [Fact]
        public async Task When_class_with_discriminator_has_base_class_then_csharp_is_generated_correctly()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ExceptionContainer>();
            var data = schema.ToJson();

            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m });

            // Act
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_interfaces_are_generated_with_inheritance_then_type_check_methods_are_generated()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ExceptionContainer>();
            var data = schema.ToJson();

            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeScriptVersion = 2.0m,
                TypeStyle = TypeScriptTypeStyle.Interface,
                GenerateTypeCheckFunctions = true
            });

            // Act
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_discriminator_does_not_match_typename_then_TypeScript_is_correct()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ExceptionContainer>();

            schema.Definitions["ExceptionBase"].AllOf.Last().DiscriminatorObject.Mapping["FooBar"] =
                schema.Definitions["ExceptionBase"].AllOf.Last().DiscriminatorObject.Mapping["MyException"];
            schema.Definitions["ExceptionBase"].AllOf.Last().DiscriminatorObject.Mapping.Remove("MyException");

            var data = schema.ToJson();
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m });

            // Act
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }
#endif

        [Theory]
        [InlineData(SchemaType.JsonSchema)]
        [InlineData(SchemaType.Swagger2)]
        [InlineData(SchemaType.OpenApi3)]
        public async Task When_schema_with_inheritance_to_object_type_is_generated_then_the_object_type_is_generated(SchemaType schemaType)
        {
            var json = @"{
    ""type"": ""object"",
    ""properties"": {
        ""request1"": {
            ""$ref"": ""#/definitions/GenericRequest1""
        },
        ""request2"": {
            ""$ref"": ""#/definitions/GenericRequest2""
        }
    },
    ""definitions"": {
        ""GenericRequest1"": {
            ""allOf"": [
                {
                    ""$ref"": ""#/definitions/GenericRequestBaseOfRequestBodyBase""
                },
                {
                    ""type"": ""object""
                }
            ]
        },
        ""GenericRequestBaseOfRequestBodyBase"": {
            ""type"": ""object"",
            ""required"": [
                ""Request""
            ],
            ""properties"": {
                ""Request"": {
                    ""$ref"": ""#/definitions/RequestBodyBase""
                }
            }
        },
        ""RequestBodyBase"": {
            ""type"": ""object""
        },
        ""GenericRequest2"": {
            ""allOf"": [
                {
                    ""$ref"": ""#/definitions/GenericRequestBaseOfRequestBody""
                },
                {
                    ""type"": ""object""
                }
            ]
        },
        ""GenericRequestBaseOfRequestBody"": {
            ""type"": ""object"",
            ""required"": [
                ""Request""
            ],
            ""properties"": {
                ""Request"": {
                    ""$ref"": ""#/definitions/RequestBody""
                }
            }
        },
        ""RequestBody"": {
            ""allOf"": [
                {
                    ""$ref"": ""#/definitions/RequestBodyBase""
                },
                {
                    ""type"": ""object""
                }
            ]
        }
    }
}";

            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            var schema = await JsonSchemaSerialization.FromJsonAsync(json, schemaType, null, factory, new DefaultContractResolver(), CancellationToken.None);
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m, SchemaType = schemaType });

            // Act
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code).UseParameters(schemaType);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Theory]
        [InlineData(SchemaType.JsonSchema)]
        [InlineData(SchemaType.Swagger2)]
        [InlineData(SchemaType.OpenApi3)]
        public async Task When_schema_with_inheritance_and_references_is_generated_then_there_are_no_duplicates(SchemaType schemaType)
        {
            var json = @"
{
    ""type"": ""object"",
    ""properties"": {
        ""foo"": {
            ""$ref"": ""#/definitions/Teacher""
        }
    },
    ""definitions"": {
        ""Person"": {
            ""type"": ""object"",
            ""discriminator"": ""discriminator"",
            ""required"": [
                ""discriminator""
            ],
            ""properties"": {
                ""Skills"": {
                    ""type"": ""object"",
                    ""additionalProperties"": {
                        ""$ref"": ""#/definitions/SkillLevel""
                    }
                },
                ""discriminator"": {
                    ""type"": ""string""
                }
            }
        },
        ""SkillLevel"": {
            ""type"": ""integer"",
            ""description"": """",
            ""x-enumNames"": [
                ""Low"",
                ""Medium"",
                ""Height""
            ],
            ""enum"": [
                0,
                1,
                2
            ]
        },
        ""Teacher"": {
            ""allOf"": [
                {
                    ""$ref"": ""#/definitions/Person""
                },
                {
                    ""type"": ""object"",
                    ""required"": [
                        ""SkillLevel""
                    ],
                    ""properties"": {
                        ""Course"": {
                            ""type"": ""string""
                        },
                        ""SkillLevel"": {
                            ""default"": 1,
                            ""allOf"": [
                                {
                                    ""$ref"": ""#/definitions/SkillLevel""
                                }
                            ]
                        }
                    }
                }
            ]
        }
    }
}
";

            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            var schema = await JsonSchemaSerialization.FromJsonAsync(json, schemaType, null, factory, new DefaultContractResolver(), CancellationToken.None);
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m, SchemaType = schemaType });

            // Act
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code).UseParameters(schemaType);
            TypeScriptCompiler.AssertCompile(code);
        }


        [Fact]
        public void When_class_has_baseclass_and_extension_code_baseclass_is_preserved()
        {
            // Arrange
            string extensionCode = @"
import * as generated from ""./generated"";

export class ExceptionBase extends generated.ExceptionBase {
    xyz: boolean; 
}
            ";

            var schema = NewtonsoftJsonSchemaGenerator.FromType<ExceptionContainer>();
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings 
            {
                ExtensionCode = extensionCode,
                ExtendedClasses = ["ExceptionBase"],
            });

            // Act
            var output = generator.GenerateTypes(schema, null);

            // Assert
            var outputArray = output.ToArray();
            Assert.Equal(4, outputArray.Length);

            Assert.Equal("Exception", outputArray[0].BaseTypeName);
            Assert.Contains("xyz: boolean", outputArray[0].Code);
        }
    }
}
