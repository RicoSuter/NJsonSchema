using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using NJsonSchema.Infrastructure;
using NJsonSchema.NewtonsoftJson.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Xunit;

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
        public async Task When_empty_class_inherits_from_dictionary_then_allOf_inheritance_still_works(bool inlineNamedDictionaries, bool convertConstructorInterfaceData)
        {
            //// Arrange
            var schema = JsonSchemaGenerator.FromType<MyContainer>();
            var data = schema.ToJson();

            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeScriptVersion = 2.0m,
                InlineNamedDictionaries = inlineNamedDictionaries,
                ConvertConstructorInterfaceData = convertConstructorInterfaceData
            });

            //// Act
            var code = generator.GenerateFile("ContainerClass");

            //// Assert
            var dschema = schema.Definitions["EmptyClassInheritingDictionary"];

            Assert.Equal(0, dschema.AllOf.Count);
            Assert.True(dschema.IsDictionary);

            if (inlineNamedDictionaries)
            {
                Assert.Contains("customDictionary: { [key: string]: any; } | undefined;", code);
                Assert.DoesNotContain("EmptyClassInheritingDictionary", code);
            }
            else
            {
                Assert.Contains("Foobar.", data);
                Assert.Contains("Foobar.", code);

                Assert.Contains("customDictionary: EmptyClassInheritingDictionary", code);
                Assert.Contains("[key: string]: any;", code);

                if (convertConstructorInterfaceData)
                {
                    Assert.Contains("this.customDictionary = data.customDictionary && !(<any>data.customDictionary).toJSON ? new EmptyClassInheritingDictionary(data.customDictionary) : <EmptyClassInheritingDictionary>this.customDictionary;", code);
                }
                else
                {
                    Assert.DoesNotContain("new EmptyClassInheritingDictionary(data.customDictionary)", code);
                }
            }
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

        [Fact]
        public async Task When_class_with_discriminator_has_base_class_then_csharp_is_generated_correctly()
        {
            //// Arrange
            var schema = JsonSchemaGenerator.FromType<ExceptionContainer>();
            var data = schema.ToJson();

            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("Foobar.", data);
            Assert.Contains("Foobar.", code);

            Assert.Contains("class ExceptionBase extends Exception", code);
            Assert.Contains("class MyException extends ExceptionBase", code);

            Assert.Contains("this._discriminator = \"MyException\";", code);
            Assert.Contains("if (data[\"kind\"] === \"MyException\") {", code);
        }

        [Fact]
        public async Task When_discriminator_does_not_match_typename_then_TypeScript_is_correct()
        {
            //// Arrange
            var schema = JsonSchemaGenerator.FromType<ExceptionContainer>();

            schema.Definitions["ExceptionBase"].AllOf.Last().DiscriminatorObject.Mapping["FooBar"] =
                schema.Definitions["ExceptionBase"].AllOf.Last().DiscriminatorObject.Mapping["MyException"];
            schema.Definitions["ExceptionBase"].AllOf.Last().DiscriminatorObject.Mapping.Remove("MyException");

            var data = schema.ToJson();
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("class ExceptionBase extends Exception", code);
            Assert.Contains("class MyException extends ExceptionBase", code);

            Assert.Contains("this._discriminator = \"FooBar\";", code);
            Assert.Contains("if (data[\"kind\"] === \"FooBar\") {", code);
        }

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
            var schema = await JsonSchemaSerialization.FromJsonAsync(json, schemaType, null, factory, new DefaultContractResolver());
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m, SchemaType = schemaType });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.DoesNotContain("request!: any;", code);
            Assert.DoesNotContain("request: any;", code);
            Assert.Contains("this.request = new RequestBodyBase()", code);
            Assert.Contains("this.request = new RequestBody()", code);
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
            var schema = await JsonSchemaSerialization.FromJsonAsync(json, schemaType, null, factory, new DefaultContractResolver());
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m, SchemaType = schemaType });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.DoesNotContain("SkillLevel2", code);
        }


        [Fact]
        public async Task When_class_has_baseclass_and_extension_code_baseclass_is_preserved() {
            //// Arrange
            string extensionCode = @"
import * as generated from ""./generated"";

export class ExceptionBase extends generated.ExceptionBase {
    xyz: boolean; 
}
            ";

            var schema = JsonSchemaGenerator.FromType<ExceptionContainer>();
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings {
                ExtensionCode = extensionCode,
                ExtendedClasses = new[] { "ExceptionBase" },
            });

            //// Act
            var output = generator.GenerateTypes(schema, null);

            //// Assert
            var outputArray = output.ToArray();
            Assert.Equal(4, outputArray.Length);

            Assert.Equal("Exception", outputArray[0].BaseTypeName);
            Assert.Contains("xyz: boolean", outputArray[0].Code);
        }
    }
}
