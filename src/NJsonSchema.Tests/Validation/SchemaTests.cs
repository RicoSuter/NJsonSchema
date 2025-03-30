﻿using NJsonSchema.Validation;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class SchemaTests
    {
        [Fact]
        public async Task When_no_additional_properties_are_allowed_then_this_error_is_returned()
        {
            // Arrange
            var schemaData = @"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""type"": ""object"",
  ""typeName"": ""ReportItem"",
  ""additionalProperties"": false,
  ""required"": [
    ""ReportID"",
    ""RequiresFilter"",
    ""MimeType"",
    ""ExternalID"",
    ""CreatedBy"",
    ""ExecutionScript"",
    ""ExecutionParameter"",
    ""Columns""
  ],
  ""properties"": {
    ""ReportID"": {
      ""type"": ""string"",
      ""format"": ""guid""
    },
    ""RequiresFilter"": {
      ""type"": ""boolean""
    },
    ""MimeType"": {
      ""type"": ""string""
    },
    ""ExternalID"": {
      ""type"": ""string""
    },
    ""CreatedBy"": {
      ""type"": ""string"",
      ""format"": ""date-time""
    },
    ""ExecutionScript"": {
      ""type"": ""string""
    },
    ""ExecutionParameter"": {
      ""type"": ""string""
    },
    ""ExecutionOrderBy"": {
      ""type"": [
        ""null"",
        ""string""
      ]
    },
    ""DynamicFilters"": {
      ""type"": [
        ""array"",
        ""null""
      ],
      ""items"": {
        ""type"": ""object"",
        ""typeName"": ""DynamicFilter"",
        ""additionalProperties"": false,
        ""required"": [
          ""ID"",
          ""Script"",
          ""ScriptOrderBy"",
          ""Fields"",
          ""ScriptParams""
        ],
        ""properties"": {
          ""ID"": {
            ""type"": ""string""
          },
          ""Script"": {
            ""type"": ""string""
          },
          ""ScriptOrderBy"": {
            ""type"": ""string""
          },
          ""Fields"": {
            ""type"": ""string""
          },
          ""ScriptParams"": {
            ""type"": ""object"",
            ""additionalProperties"": {
              ""type"": [
                ""array"",
                ""boolean"",
                ""integer"",
                ""null"",
                ""number"",
                ""object"",
                ""string""
              ]
            }
          }
        }
      }
    },
    ""RequiresOrgID"": {
      ""type"": ""boolean""
    },
    ""ReportFilter"": {
      ""oneOf"": [
        {
          ""$ref"": ""#/definitions/QueryFilter""
        },
        {
          ""type"": ""null""
        }
      ]
    },
    ""ReportRules"": {
      ""oneOf"": [
        {
          ""$ref"": ""#/definitions/QueryRule""
        },
        {
          ""type"": ""null""
        }
      ]
    },
    ""Columns"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""typeName"": ""QueryColumn"",
        ""additionalProperties"": false,
        ""required"": [
          ""isrequired"",
          ""column_name""
        ],
        ""properties"": {
          ""isrequired"": {
            ""type"": ""boolean""
          },
          ""column_name"": {
            ""type"": ""string""
          }
        }
      }
    }
  },
  ""definitions"": {
    ""QueryFilter"": {
      ""type"": ""object"",
      ""typeName"": ""QueryFilter"",
      ""additionalProperties"": false,
      ""required"": [
        ""display_errors"",
        ""allow_empty"",
        ""plugins"",
        ""filters""
      ],
      ""properties"": {
        ""display_errors"": {
          ""type"": ""boolean""
        },
        ""allow_empty"": {
          ""type"": ""boolean""
        },
        ""plugins"": {
          ""type"": ""array"",
          ""items"": {
            ""type"": ""string""
          }
        },
        ""filters"": {
          ""type"": ""array"",
          ""items"": {
            ""type"": ""object"",
            ""typeName"": ""Filter"",
            ""additionalProperties"": false,
            ""required"": [
              ""id"",
              ""label"",
              ""type"",
              ""operators""
            ],
            ""properties"": {
              ""id"": {
                ""type"": ""string""
              },
              ""label"": {
                ""type"": ""string""
              },
              ""type"": {
                ""type"": ""string""
              },
              ""operators"": {
                ""type"": ""array"",
                ""items"": {
                  ""type"": ""string""
                }
              },
              ""input"": {
                ""type"": [
                  ""null"",
                  ""string""
                ]
              },
              ""values"": {
                ""type"": [
                  ""null"",
                  ""object""
                ],
                ""additionalProperties"": {
                  ""type"": [
                    ""array"",
                    ""boolean"",
                    ""integer"",
                    ""null"",
                    ""number"",
                    ""object"",
                    ""string""
                  ]
                }
              },
              ""validation"": {
                ""oneOf"": [
                  {
                    ""$ref"": ""#/definitions/Validation""
                  },
                  {
                    ""type"": ""null""
                  }
                ]
              },
              ""unique"": {
                ""type"": ""boolean""
              },
              ""description"": {
                ""type"": [
                  ""null"",
                  ""string""
                ]
              }
            }
          }
        }
      }
    },
    ""QueryRule"": {
      ""type"": ""object"",
      ""typeName"": ""QueryRule"",
      ""additionalProperties"": false,
      ""properties"": {
        ""condition"": {
          ""type"": [
            ""null"",
            ""string""
          ]
        },
        ""rules"": {
          ""type"": [
            ""array"",
            ""null""
          ],
          ""items"": {
            ""type"": ""object"",
            ""typeName"": ""Rule"",
            ""additionalProperties"": false,
            ""properties"": {
              ""id"": {
                ""type"": [
                  ""null"",
                  ""string""
                ]
              },
              ""operator"": {
                ""type"": [
                  ""null"",
                  ""string""
                ]
              },
              ""value"": {
                ""type"": [
                  ""null"",
                  ""object""
                ]
              },
              ""readonly"": {
                ""type"": ""boolean""
              },
              ""condition"": {
                ""type"": [
                  ""null"",
                  ""string""
                ]
              }
            }
          }
        }
      }
    },
    ""Validation"": {
      ""type"": ""object"",
      ""typeName"": ""Validation"",
      ""additionalProperties"": false,
      ""properties"": {
        ""min"": {
          ""type"": ""integer""
        },
        ""step"": {
          ""type"": ""number"",
          ""format"": ""double""
        }
      }
    }
  }
}

";
            var schema = await JsonSchema.FromJsonAsync(schemaData);

            // Act
            var errors = schema.Validate(@"{""Key"": ""Value""}");
            var error = errors.SingleOrDefault(e => e.Kind == ValidationErrorKind.NoAdditionalPropertiesAllowed);

            // Assert
            Assert.NotNull(error);
            Assert.Equal("#/Key", error.Path);
            Assert.Same(schema, error.Schema);
        }

        [Fact]
        public async Task When_multiple_types_fail_with_errors_take_the_best_group()
        {
            // Arrange
            var schemaJson = @"{
        ""$schema"": ""http://json-schema.org/schema#"",
        ""type"": ""object"",
        ""properties"": {
         ""name"": {
           ""type"": ""string"",
           ""maxLength"": 40
         },
         ""settings"": {
           ""type"": [ ""object"", ""null"" ],
           ""properties"": {
            ""security"": {
              ""type"": [ ""object"", ""null"" ],
              ""properties"": {
               ""timeout"": {
                 ""type"": [ ""integer"", ""null"" ],
                 ""minimum"": 1,
                 ""maximum"": 10
               }
              },
              ""additionalProperties"": false
            }
           },
           ""additionalProperties"": false
         }
        },
        ""required"": [ ""name"" ],
        ""additionalProperties"": false
      }";

            var json = @"{
   ""name"":""abc"",
   ""settings"": {
     ""security"":{
      ""timeout"": 0
     }
   }
}";

            // Act
            var schema = await JsonSchema.FromJsonAsync(schemaJson);
            var errors = schema.Validate(json);

            // Assert
            Assert.Single(errors);
            Assert.Contains(errors, e => e.Kind == ValidationErrorKind.NoTypeValidates);
        }

        [Fact]
        public async Task When_datetime_with_regex_validation_then_datetime_is_not_altered()
        {
            // Arrange
            var schemaJson = @"
            {
              ""$schema"": ""http://json-schema.org/draft-07/schema#"",
              ""type"": ""object"",
              ""required"": [
                ""my_datetime""
              ],
              ""properties"": {
                ""my_datetime"": {
                  ""type"": ""string"",
                  ""pattern"": ""^\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z$""
                }
              }
            }";

            var json = @"
            {
              ""my_datetime"": ""2018-12-19T16:58:07.270Z""
            }";

            // Act
            var schema = await JsonSchema.FromJsonAsync(schemaJson);
            var errors = schema.Validate(json);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task When_property_name_is_ref_then_validation_works()
        {
            // Arrange
            var jsonSchema = @"{
    ""$schema"": ""http://json-schema.org/draft-07/schema"",  
    ""$ref"": ""#/definitions/reference_to_other_object"",
    ""definitions"": {
        ""reference_to_other_object"": {
            ""type"": ""object"",
            ""required"": [
                ""$ref""
            ],
            ""additionalProperties"": false,
            ""properties"": {
                ""$ref"": {
                    ""type"": ""string"",
                    ""allOf"": [
                        {
                            ""format"": ""uri-reference""
                        },
                        {
                            ""pattern"": ""^.*#/datatypes/.*$""
                        }
                    ]
                }
            }
        }
    }
}";

            // Act
            var jsonContent = @"{
  ""$ref"": ""#/datatypes/MyCustomDataType""
}";

            // Arrange
            var validator = new JsonSchemaValidator(new JsonSchemaValidatorSettings());
            var schema = await JsonSchema.FromJsonAsync(jsonSchema);
            var result = validator.Validate(jsonContent, schema);

            // Assert
            Assert.Empty(result);
        }
    }
}
