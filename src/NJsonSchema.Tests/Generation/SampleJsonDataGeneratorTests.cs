using System.Text.Json;
using System.Text.Json.Nodes;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;
using System.ComponentModel;

namespace NJsonSchema.Tests.Generation
{
    public class SampleJsonDataGeneratorTests
    {
        public class Person
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public Address MainAddress { get; set; }

            public Address[] Addresses { get; set; }

        }

        public class Address
        {
            public string Street { get; set; }
        }

        public class Student : Person
        {
            public string Course { get; set; }
        }

        public class Measurements
        {
            [DefaultValue(new int[] {1,2,3})]
            public int[] Weights;
        }

        [Fact]
        public void When_sample_data_is_generated_from_schema_then_properties_are_set()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Person>();
            var generator = new SampleJsonDataGenerator();

            // Act
            var token = generator.Generate(schema);
            var obj = token as JsonObject;

            // Assert
            Assert.NotNull(obj[nameof(Person.FirstName)]);
            Assert.NotNull(obj[nameof(Person.LastName)]);
            Assert.NotNull(obj[nameof(Person.MainAddress)]);
            Assert.NotNull(obj[nameof(Person.Addresses)]);
        }

        [Fact]
        public void When_sample_data_is_generated_from_schema_with_base_then_properties_are_set()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Student>();
            var generator = new SampleJsonDataGenerator();

            // Act
            var token = generator.Generate(schema);
            var obj = token as JsonObject;

            // Assert
            Assert.NotNull(obj[nameof(Student.Course)]);
            Assert.NotNull(obj[nameof(Person.FirstName)]);
            Assert.NotNull(obj[nameof(Person.LastName)]);
            Assert.NotNull(obj[nameof(Person.MainAddress)]);
            Assert.NotNull(obj[nameof(Person.Addresses)]);
        }

        [Fact]
        public void Default_values_are_set_for_arrays()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Measurements>();
            var generator = new SampleJsonDataGenerator();

            // Act
            var token = generator.Generate(schema);
            var obj = token as JsonObject;

            // Assert
            var weightsNode = obj![nameof(Measurements.Weights)] as JsonArray;
            Assert.NotNull(weightsNode);
            var actual = weightsNode!.Select(x => x!.GetValue<int>()).ToArray();
            Assert.Equal(new int[] { 1, 2, 3 }, actual);
        }

        [Fact]
        public async Task When_generateOptionalProperties_is_false_then_optional_properties_are_not_set()
        {
            // Arrange
            var data = @"{
                ""$schema"": ""http://json-schema.org/draft-04/schema#"",
                ""title"": ""test schema"",
                ""type"": ""object"",
                ""required"": [
                  ""isrequired""
                ],
                ""properties"": {
                  ""isrequired"": {
                    ""type"": ""object"",
                    ""properties"": {
                      ""value"": {
                        ""type"": ""integer""
                      }
                    }
                  },
                  ""isoptional"": {
                    ""type"": ""object"",
                    ""properties"": {
                      ""value"": {
                        ""type"": ""integer""
                      }
                    }
                  }
                }
              }";

            var schema = await JsonSchema.FromJsonAsync(data);
            var generator = new SampleJsonDataGenerator(new SampleJsonDataGeneratorSettings
            {
                GenerateOptionalProperties = false
            });

            // Act
            var token = generator.Generate(schema);
            var obj = token as JsonObject;

            // Assert
            Assert.NotNull(obj!["isrequired"]);
            Assert.Null(obj["isoptional"]);
        }

        [Fact]
        public async Task PropertyWithIntegerMinimumDefiniton()
        {
            // Arrange
            var data = @"{
                ""$schema"": ""http://json-schema.org/draft-04/schema#"",
                ""title"": ""test schema"",
                ""type"": ""object"",
                ""required"": [
                  ""body""
                ],
                ""properties"": {
                  ""body"": {
                    ""$ref"": ""#/definitions/body""
                  }
                },
                ""definitions"": {
                  ""body"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                      ""numberContent"": {
                        ""$ref"": ""#/definitions/numberContent""
                      }
                    }
                  },
                  ""numberContent"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                      ""value"": {
                        ""type"": ""integer"",
                        ""maximum"": 5,
                        ""minimum"": 1
                      }
                    }
                  }
                }
              }";
            var generator = new SampleJsonDataGenerator();
            var schema = await JsonSchema.FromJsonAsync(data);
            // Act
            var testJson = generator.Generate(schema);

            // Assert
            var validationResult = schema.Validate(testJson!.ToJsonString());
            Assert.NotNull(validationResult);
            Assert.Empty(validationResult);
            Assert.Equal(1, testJson!["body"]!["numberContent"]!["value"]!.GetValue<int>());
        }

        [Fact]
        public async Task SchemaWithRecursiveDefinition()
        {
            // Arrange
            var data = @"{
                ""$schema"": ""http://json-schema.org/draft-04/schema#"",
                ""title"": ""test schema"",
                ""type"": ""object"",
                ""required"": [
                  ""body"", ""footer""
                ],
                ""properties"": {
                  ""body"": {
                    ""$ref"": ""#/definitions/body""
                  },
                ""footer"": {
                    ""$ref"": ""#/definitions/numberContent""
                  }
                },
                ""definitions"": {
                  ""body"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                      ""numberContent"": {
                        ""$ref"": ""#/definitions/numberContent""
                      }
                    }
                  },
                  ""numberContent"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                      ""value"": {
                        ""type"": ""number"",
                        ""maximum"": 5.00001,
                        ""minimum"": 1.000012
                      },
                      ""data"": {
                        ""$ref"": ""#/definitions/body""
                      }
                    }
                  }
                }
              }";
            var generator = new SampleJsonDataGenerator();
            var schema = await JsonSchema.FromJsonAsync(data);
            // Act
            var testJson = generator.Generate(schema);

            // Assert
            var footerToken = testJson!["body"]!["numberContent"]!["data"]!["numberContent"]!["value"];
            Assert.NotNull(footerToken);

            var validationResult = schema.Validate(testJson!.ToJsonString());
            Assert.NotNull(validationResult);
            Assert.Equal(1.000012m, testJson!["footer"]!["value"]!.GetValue<decimal>());
            Assert.True(validationResult.Count > 0); // It is expected to fail validating the recursive properties (because of max recursion level)
        }

        [Fact]
        public async Task GeneratorAdheresToMaxRecursionLevel()
        {
            // Arrange
            var data = @"{
                ""$schema"": ""http://json-schema.org/draft-04/schema#"",
                ""title"": ""test schema"",
                ""type"": ""object"",
                ""required"": [
                  ""body"", ""footer""
                ],
                ""properties"": {
                  ""body"": {
                    ""$ref"": ""#/definitions/body""
                  }
                },
                ""definitions"": {
                  ""body"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                      ""text"": { ""type"": ""string"", ""enum"": [""my_string""] },
                      ""body"": {
                        ""$ref"": ""#/definitions/body""
                      }
                    }
                  }
                }
              }";
            var generator = new SampleJsonDataGenerator(new SampleJsonDataGeneratorSettings() { MaxRecursionLevel = 2 });
            var schema = await JsonSchema.FromJsonAsync(data);
            // Act
            var testJson = generator.Generate(schema);

            // Assert
            var secondBodyToken = testJson!["body"]!["body"];
            Assert.NotNull(secondBodyToken);

            var thirdBodyToken = testJson!["body"]!["body"]!["body"];
            Assert.NotNull(thirdBodyToken);
            Assert.True(thirdBodyToken is JsonValue jsonVal && jsonVal.GetValueKind() == JsonValueKind.Null
                      || thirdBodyToken is null);

            var validationResult = schema.Validate(testJson!.ToJsonString());
            Assert.NotNull(validationResult);
            Assert.True(validationResult.Count > 0); // It is expected to fail validating the recursive properties (because of max recursion level)
        }

        [Fact]
        public async Task SchemaWithDefinitionUseMultipleTimes()
        {
            // Arrange
            var data = @"{
                ""$schema"": ""http://json-schema.org/draft-04/schema#"",
                ""title"": ""test schema"",
                ""type"": ""object"",
                ""required"": [
                  ""body"", ""footer""
                ],
                ""properties"": {
                  ""body"": {
                    ""$ref"": ""#/definitions/body""
                  },
                ""footer"": {
                    ""$ref"": ""#/definitions/numberContent""
                  }
                },
                ""definitions"": {
                  ""body"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                      ""numberContent"": {
                        ""$ref"": ""#/definitions/numberContent""
                      }
                    }
                  },
                  ""numberContent"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                      ""value"": {
                        ""type"": ""number"",
                        ""maximum"": 5.00001,
                        ""minimum"": 1.000012
                      }
                    }
                  }
                }
              }";
            var generator = new SampleJsonDataGenerator();
            var schema = await JsonSchema.FromJsonAsync(data);

            // Act
            var testJson = generator.Generate(schema);

            // Assert
            var footerToken = testJson!["footer"]!["value"];
            Assert.NotNull(footerToken);

            var validationResult = schema.Validate(testJson!.ToJsonString());
            Assert.NotNull(validationResult);
            Assert.Empty(validationResult);
            Assert.Equal(1.000012m, testJson!["body"]!["numberContent"]!["value"]!.GetValue<decimal>());
        }

        [Fact]
        public async Task PropertyWithFloatMinimumDefinition()
        {
            // Arrange
            var data = @"{
                ""$schema"": ""http://json-schema.org/draft-04/schema#"",
                ""title"": ""test schema"",
                ""type"": ""object"",
                ""required"": [
                  ""body""
                ],
                ""properties"": {
                  ""body"": {
                    ""$ref"": ""#/definitions/body""
                  }
                },
                ""definitions"": {
                  ""body"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                      ""numberContent"": {
                        ""$ref"": ""#/definitions/numberContent""
                      }
                    }
                  },
                  ""numberContent"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                      ""value"": {
                        ""type"": ""number"",
                        ""maximum"": 5.00001,
                        ""minimum"": 1.000012
                      }
                    }
                  }
                }
              }";
            var generator = new SampleJsonDataGenerator();
            var schema = await JsonSchema.FromJsonAsync(data);
            // Act
            var testJson = generator.Generate(schema);

            // Assert
            var validationResult = schema.Validate(testJson!.ToJsonString());
            Assert.NotNull(validationResult);
            Assert.Empty(validationResult);
            Assert.Equal(1.000012m, testJson!["body"]!["numberContent"]!["value"]!.GetValue<decimal>());
        }

        [Fact]
        public async Task PropertyWithDefaultDefiniton()
        {
            // Arrange
            var data = @"{
                ""$schema"": ""http://json-schema.org/draft-04/schema#"",
                ""title"": ""test schema"",
                ""type"": ""object"",
                ""required"": [
                  ""body""
                ],
                ""properties"": {
                  ""body"": {
                    ""$ref"": ""#/definitions/body""
                  }
                },
                ""definitions"": {
                  ""body"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                      ""numberContent"": {
                        ""$ref"": ""#/definitions/numberContent""
                      }
                    }
                  },
                  ""numberContent"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                      ""value"": {
                        ""type"": ""number"",
                        ""default"": 42,
                      }
                    }
                  }
                }
              }";
            var generator = new SampleJsonDataGenerator();
            var schema = await JsonSchema.FromJsonAsync(data);
            // Act
            var testJson = generator.Generate(schema);

            // Assert
            var validationResult = schema.Validate(testJson!.ToJsonString());
            Assert.NotNull(validationResult);
            Assert.Empty(validationResult);
            Assert.Equal(42, testJson!["body"]!["numberContent"]!["value"]!.GetValue<int>());
        }

        //exclusiveMaximum

        [Theory]
        [InlineData("uuid")]
        [InlineData("guid")]
        public async Task PropertyWithGuidOrUuidFormatGeneratesGuid(string format)
        {
            // Arrange
            var data = $@"{{
                ""$schema"": ""http://json-schema.org/draft-04/schema#"",
                ""title"": ""test schema"",
                ""type"": ""object"",
                ""required"": [
                  ""identifier""
                ],
                ""properties"": {{
                  ""identifier"": {{
                    ""type"": ""string"",
                    ""format"": ""{format}""
                  }}
                }}
              }}";
            var generator = new SampleJsonDataGenerator();
            var schema = await JsonSchema.FromJsonAsync(data);

            // Act
            var testJson = generator.Generate(schema);

            // Assert
            var identifierValue = testJson!["identifier"]?.GetValue<string>();
            Assert.NotNull(identifierValue);
            Assert.True(System.Guid.TryParse(identifierValue, out _), $"Expected a GUID but got: {identifierValue}");
        }

        [Fact]
        public async Task PropertyExclusiveMinimumDefiniton()
        {
            // Arrange
            var data = @"{
                ""$schema"": ""http://json-schema.org/draft-04/schema#"",
                ""title"": ""test schema"",
                ""type"": ""object"",
                ""required"": [
                  ""body""
                ],
                ""properties"": {
                  ""body"": {
                    ""$ref"": ""#/definitions/body""
                  }
                },
                ""definitions"": {
                  ""body"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                      ""numberContent"": {
                        ""$ref"": ""#/definitions/numberContent""
                      }
                    }
                  },
                  ""numberContent"": {
                    ""type"": ""object"",
                    ""additionalProperties"": false,
                    ""properties"": {
                      ""value"": {
                        ""type"": ""number"",
                        ""maximum"": 5.0,
                        ""minimum"": 1.0,
                        ""exclusiveMinimum"" : true
                      }
                    }
                  }
                }
              }";
            var generator = new SampleJsonDataGenerator();
            var schema = await JsonSchema.FromJsonAsync(data);
            // Act
            var testJson = generator.Generate(schema);

            // Assert
            var validationResult = schema.Validate(testJson!.ToJsonString());
            Assert.NotNull(validationResult);
            Assert.Empty(validationResult);
            Assert.Equal(1.1m, testJson!["body"]!["numberContent"]!["value"]!.GetValue<decimal>());
        }

        [Fact]
        public async Task When_integer_has_long_minimum_then_sample_does_not_overflow()
        {
            // Arrange
            var data = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""bigInt"": {
                        ""type"": ""integer"",
                        ""minimum"": 3000000000
                    }
                },
                ""required"": [""bigInt""]
            }";
            var schema = await JsonSchema.FromJsonAsync(data);
            var generator = new SampleJsonDataGenerator();

            // Act
            var testJson = generator.Generate(schema);

            // Assert
            Assert.Equal(3000000000L, testJson!["bigInt"]!.GetValue<long>());
        }

        [Fact]
        public async Task When_integer_has_long_exclusive_minimum_then_sample_does_not_overflow()
        {
            // Arrange
            var data = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""bigInt"": {
                        ""type"": ""integer"",
                        ""exclusiveMinimum"": 5000000000
                    }
                },
                ""required"": [""bigInt""]
            }";
            var schema = await JsonSchema.FromJsonAsync(data);
            var generator = new SampleJsonDataGenerator();

            // Act
            var testJson = generator.Generate(schema);

            // Assert
            Assert.Equal(5000000000L, testJson!["bigInt"]!.GetValue<long>());
        }

        [Fact]
        public async Task When_integer_has_negative_long_minimum_then_sample_does_not_overflow()
        {
            // Arrange
            var data = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""bigNegative"": {
                        ""type"": ""integer"",
                        ""minimum"": -9223372036854775808
                    }
                },
                ""required"": [""bigNegative""]
            }";
            var schema = await JsonSchema.FromJsonAsync(data);
            var generator = new SampleJsonDataGenerator();

            // Act
            var testJson = generator.Generate(schema);

            // Assert
            Assert.Equal(long.MinValue, testJson!["bigNegative"]!.GetValue<long>());
        }
    }
}
