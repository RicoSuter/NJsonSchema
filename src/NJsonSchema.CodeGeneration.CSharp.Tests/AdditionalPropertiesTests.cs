using System.Text.RegularExpressions;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class AdditionalPropertiesTests
    {
        [Fact]
        public async Task When_additionalProperties_schema_is_set_for_object_then_special_property_is_rendered()
        {
            //// Arrange
            var json =
@"{ 
    ""properties"": {
        ""Pet"": {
            ""type"": ""object"",
            ""properties"": {
                ""id"": {
                    ""type"": ""integer"",
                    ""format"": ""int64""
                },
                ""category"": {
                    ""type"": ""string""
                }
            },
            ""additionalProperties"": {
                ""type"": ""string""
            }
        }
    }
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("Person");

            //// Assert
            Assert.Contains("[Newtonsoft.Json.JsonExtensionData]", code);
            Assert.Contains("public System.Collections.Generic.IDictionary<string, object> AdditionalProperties", code);
            Assert.Contains("get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }", code);
        }

        [Fact]
        public async Task When_using_SystemTextJson_additionalProperties_schema_is_set_for_object_then_special_property_is_rendered()
        {
            //// Arrange
            var json =
                @"{ 
    ""properties"": {
        ""Pet"": {
            ""type"": ""object"",
            ""properties"": {
                ""id"": {
                    ""type"": ""integer"",
                    ""format"": ""int64""
                },
                ""category"": {
                    ""type"": ""string""
                }
            },
            ""additionalProperties"": {
                ""type"": ""string""
            }
        }
    }
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings()
            {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson
            });
            var code = generator.GenerateFile("Person");

            //// Assert
            Assert.Contains("[System.Text.Json.Serialization.JsonExtensionData]", code);
            Assert.Contains("public System.Collections.Generic.IDictionary<string, object> AdditionalProperties", code);
            Assert.Contains("get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }", code);
        }

        [Fact]
        public async Task When_using_SystemTextJson_additionalProperties_schema_is_set_for_object_then_special_property_is_rendered_only_for_base_class()
        {
            var json =
                @"{ 
    ""properties"": {
        ""dog"": {
            ""allOf"": [
                {
                    ""$ref"": ""#/components/Pet""
                },
                {
                    ""description"": ""Dog""
                }
            ]
        },
        ""cat"": {
            ""allOf"": [
                {
                    ""$ref"": ""#/components/Pet""
                },
                {
                    ""description"": ""Cat""
                }
            ]
        }
    },
    ""components"": {
        ""Pet"": {
            ""type"": ""object"",
            ""description"": ""Pet"",
            ""properties"": {
                ""id"": {
                    ""type"": ""integer"",
                    ""format"": ""int64""
                },
                ""category"": {
                    ""type"": ""string""
                }
            }
        }
    }
}";

            var schema = await JsonSchema.FromJsonAsync(json);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings()
            {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson
            });
            var code = generator.GenerateFile("Person");

            //// Assert
            var matches = Regex.Matches(code, @"(\[System\.Text\.Json\.Serialization\.JsonExtensionData\])");
            
            // There are two matches, the Person class and the Pet class
            Assert.Equal(2, matches.Count);
        }
        
        [Fact]
        public async Task When_using_SystemTextJson_additionalProperties_schema_is_set_for_object_then_special_property_is_rendered_only_for_lowest_base_class()
        {
            var json =
                @"{  
  ""properties"": {
        ""Name"": {
            ""type"": ""string""            
        }
    },
  ""definitions"": {    
      ""Cat"": {
        ""allOf"": [
          {
            ""$ref"": ""#/definitions/Pet""
          },
          {
            ""type"": ""object"",
            ""additionalProperties"": {
              ""nullable"": true
            },
            ""properties"": {
              ""whiskers"": {
                ""type"": ""string""
              }
            }
          }
        ]
      },
      ""Pet"": {
        ""allOf"": [
          {
            ""$ref"": ""#/definitions/Animal""
          },
          {
            ""type"": ""object"",
            ""additionalProperties"": {
              ""nullable"": true
            },
            ""properties"": {
                ""id"": {
                    ""type"": ""integer"",
                    ""format"": ""int64""
                }
            }  
          }
        ]
      },
      ""Animal"": {
        ""type"": ""object"",        
        ""properties"": {
          ""category"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        }      
    }
  }
}";

            var schema = await JsonSchema.FromJsonAsync(json);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings()
            {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson
            });            
            
            var code = generator.GenerateFile("SommeDummyClass");

            //// Assert
            var matches = Regex.Matches(code, @"(\[System\.Text\.Json\.Serialization\.JsonExtensionData\])");
            
            // There are two matches, the SommeDummyClass class and the Animal class
            Assert.Equal(2, matches.Count);
        }
        
        public class Page
        {
        }

        public class Book
        {
            public Dictionary<string, string> Index { get; set; }

            public Page Page { get; set; }
        }

        [Fact]
        public void When_AlwaysAllowAdditionalObjectProperties_is_set_then_dictionary_and_no_object_are_not_same()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Book>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                AlwaysAllowAdditionalObjectProperties = true
            });
            var json = schema.ToJson();

            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns",
                InlineNamedAny = true
            };
            var generator = new CSharpGenerator(schema, settings);

            // Act
            var code = generator.GenerateFile("Library");

            // Assert
            Assert.Contains("public object Page", code);
            Assert.Contains("public System.Collections.Generic.IDictionary<string, string> Index", code);
        }

        [Fact]
        public void When_AlwaysAllowAdditionalObjectProperties_is_set_then_any_page_has_additional_properties()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Book>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                AlwaysAllowAdditionalObjectProperties = true
            });
            var json = schema.ToJson();

            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns"
            };
            var generator = new CSharpGenerator(schema, settings);

            // Act
            var code = generator.GenerateFile("Library");

            // Assert
            Assert.Contains("public Page Page", code);
            Assert.Contains("IDictionary<string, object> AdditionalProperties", code);
            Assert.Contains("public System.Collections.Generic.IDictionary<string, string> Index", code);
        }
    }
}