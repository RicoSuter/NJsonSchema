using NJsonSchema.Annotations;
using NJsonSchema.NewtonsoftJson.Generation;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class JsonSchemaExtensionDataAttributeGeneratorTests
    {
        public class MyType
        {
            public int Id { get; set; }
        }

        public class ClassWithExtensionData
        {
            [JsonSchemaExtensionData("x-other", "barfoo")]
            public string TextFieldExtension { get; set; }

            [JsonSchemaExtensionData("x-data", "foobar")]
            public MyType SubDataExtension { get; set; }
        }

        public class ClassWithoutExtensionData
        {
            public string TextFieldNoExtension { get; set; }

            public MyType SubDataNoExtension { get; set; }
        }

        public class RootType
        {
            public ClassWithoutExtensionData Extension { get; set; }

            public ClassWithExtensionData NoExtension { get; set; }
        }

        [Fact]
        public async Task When_class_has_property_with_JsonSchemaExtensionDataAttribute_on_property_then_extensiondata_schema_is_set_on_property_level()
        {
            //// Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<RootType>(new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();

            var expectedJSON = @"{
              ""$schema"": ""http://json-schema.org/draft-04/schema#"",
              ""title"": ""RootType"",
              ""type"": ""object"",
              ""additionalProperties"": false,
              ""properties"": {
                ""Extension"": {
                  ""x-nullable"": true,
                  ""oneOf"": [
                    {
                      ""$ref"": ""#/definitions/ClassWithoutExtensionData""
                    }
                  ]
                },
                ""NoExtension"": {
                  ""x-nullable"": true,
                  ""oneOf"": [
                    {
                      ""$ref"": ""#/definitions/ClassWithExtensionData""
                    }
                  ]
                }
              },
              ""definitions"": {
                ""ClassWithoutExtensionData"": {
                  ""type"": ""object"",
                  ""additionalProperties"": false,
                  ""properties"": {
                    ""TextFieldNoExtension"": {
                      ""type"": ""string"",
                      ""x-nullable"": true
                    },
                    ""SubDataNoExtension"": {
                      ""x-nullable"": true,
                      ""oneOf"": [
                        {
                          ""$ref"": ""#/definitions/MyType""
                        }
                      ]
                    }
                  }
                },
                ""MyType"": {
                  ""type"": ""object"",
                  ""additionalProperties"": false,
                  ""properties"": {
                    ""Id"": {
                      ""type"": ""integer"",
                      ""format"": ""int32""
                    }
                  }
                },
                ""ClassWithExtensionData"": {
                  ""type"": ""object"",
                  ""additionalProperties"": false,
                  ""properties"": {
                    ""TextFieldExtension"": {
                      ""type"": ""string"",
                      ""x-nullable"": true,
                      ""x-other"": ""barfoo""
                    },
                    ""SubDataExtension"": {
                      ""x-nullable"": true,
                      ""oneOf"": [
                        {
                          ""$ref"": ""#/definitions/MyType""
                        }
                      ],
                      ""x-data"": ""foobar""
                    }
                  }
                }
              }
            }";

            //// Assert generated JSON matches schema, ignores whitespace and spaces
            var result = String.Compare(json, expectedJSON, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols);

            Assert.Equal(0, result);
        }

        [Fact]
        public async Task When_class_has_property_with_JsonSchemaExtensionDataAttribute_on_property_then_extensiondata_is_set_to_property()
        {
            //// Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithExtensionData>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3
            });

            var json = schema.ToJson();

            var expectedJSON = @"{
                ""$schema"": ""http://json-schema.org/draft-04/schema#"",
                ""title"": ""ClassWithExtensionData"",
                ""type"": ""object"",
                ""additionalProperties"": false,
                ""properties"": {
                    ""TextFieldExtension"": {
                        ""type"": ""string"",
                        ""x-nullable"": true,
                        ""x-other"": ""barfoo""
                    },
                    ""SubDataExtension"": {
                        ""x-nullable"": true,
                        ""oneOf"": [{
                                ""$ref"": ""#/definitions/MyType""
                            }
                        ],
                        ""x-data"": ""foobar"",
                    }
                },
                ""definitions"": {
                    ""MyType"": {
                        ""type"": ""object"",
                        ""additionalProperties"": false,
                        ""properties"": {
                            ""Id"": {
                                ""type"": ""integer"",
                                ""format"": ""int32""
                            }
                        }
                    }
                }
            }";

            //// Assert generated JSON matches schema, ignores whitespace and spaces
            var result = string.Compare(json, expectedJSON, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols);

            Assert.Equal(0, result);
        }
    }
}
