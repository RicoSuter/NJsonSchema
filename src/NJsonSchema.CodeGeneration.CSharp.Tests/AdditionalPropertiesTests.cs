using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp.Tests;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class AdditionalPropertiesTests
    {
        [Fact]
        public async Task When_additionalProperties_schema_is_set_for_object_then_special_property_is_rendered()
        {
            // Arrange
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

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("Person");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_using_SystemTextJson_additionalProperties_schema_is_set_for_object_then_special_property_is_rendered()
        {
            // Arrange
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

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings()
            {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson
            });
            var code = generator.GenerateFile("Person");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
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

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings()
            {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson
            });
            var code = generator.GenerateFile("Person");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
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

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings()
            {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson
            });            
            
            var code = generator.GenerateFile("SommeDummyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
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
        public async Task When_AlwaysAllowAdditionalObjectProperties_is_set_then_dictionary_and_no_object_are_not_same()
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
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_AlwaysAllowAdditionalObjectProperties_is_set_then_any_page_has_additional_properties()
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
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }
    }
}