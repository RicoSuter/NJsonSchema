using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Converters;
using NJsonSchema.Infrastructure;
using System;
using System.Collections.Generic;
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
            var schema = JsonSchema.FromType<MyContainer>();
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
                Assert.Contains("customDictionary: { [key: string] : any; } | undefined;", code);
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
            var schema = JsonSchema.FromType<ExceptionContainer>();
            var data = schema.ToJson();

            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("Foobar.", data);
            Assert.Contains("Foobar.", code);

            Assert.Contains("class ExceptionBase extends Exception", code);
            Assert.Contains("class MyException extends ExceptionBase", code);
        }

        [Theory]
        [InlineData(SchemaType.JsonSchema)]
        [InlineData(SchemaType.Swagger2)]
        [InlineData(SchemaType.OpenApi3)]
        public async Task When_schema_with_inheritance_and_references_is_generated_then_there_are_no_duplicates(SchemaType schemaType)
        {
            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            var schema = await JsonSchemaSerialization.FromJsonAsync(@"
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
", schemaType, null, factory, new DefaultContractResolver());
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m, SchemaType = schemaType });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.DoesNotContain("SkillLevel2", code);
        }
    }
}
