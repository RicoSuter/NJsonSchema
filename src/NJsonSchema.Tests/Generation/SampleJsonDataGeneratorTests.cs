using Newtonsoft.Json.Linq;
using NJsonSchema.Generation;
using System.ComponentModel;
using System.Threading.Tasks;
using Xunit;

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
            //// Arrange
            var schema = JsonSchema.FromType<Person>();
            var generator = new SampleJsonDataGenerator();

            //// Act
            var token = generator.Generate(schema);
            var obj = token as JObject;

            //// Assert
            Assert.NotNull(obj.Property(nameof(Person.FirstName)));
            Assert.NotNull(obj.Property(nameof(Person.LastName)));
            Assert.NotNull(obj.Property(nameof(Person.MainAddress)));
            Assert.NotNull(obj.Property(nameof(Person.Addresses)));
        }

        [Fact]
        public void When_sample_data_is_generated_from_schema_with_base_then_properties_are_set()
        {
            //// Arrange
            var schema = JsonSchema.FromType<Student>();
            var generator = new SampleJsonDataGenerator();

            //// Act
            var token = generator.Generate(schema);
            var obj = token as JObject;

            //// Assert
            Assert.NotNull(obj.Property(nameof(Student.Course)));
            Assert.NotNull(obj.Property(nameof(Person.FirstName)));
            Assert.NotNull(obj.Property(nameof(Person.LastName)));
            Assert.NotNull(obj.Property(nameof(Person.MainAddress)));
            Assert.NotNull(obj.Property(nameof(Person.Addresses)));
        }

        [Fact]
        public void Default_values_are_set_for_arrays()
        {
            //// Arrange
            var schema = JsonSchema.FromType<Measurements>();
            var generator = new SampleJsonDataGenerator();

            //// Act
            var token = generator.Generate(schema);
            var obj = token as JObject;

            //// Assert
            Assert.Equal(new JArray(new int[] { 1, 2, 3 }), obj.GetValue(nameof(Measurements.Weights)));
        }

        [Fact]
        public async Task When_generateOptionalProperties_is_false_then_optional_properties_are_not_set()
        {
            //// Arrange
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

            //// Act
            var token = generator.Generate(schema);
            var obj = token as JObject;

            //// Assert
            Assert.NotNull(obj.Property("isrequired"));
            Assert.Null(obj.Property("isoptional"));
        }

        [Fact]
        public async Task PropertyWithIntegerMinimumDefiniton()
        {
            //// Arrange
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
            //// Act
            var testJson = generator.Generate(schema);

            //// Assert
            var validationResult = schema.Validate(testJson);
            Assert.NotNull(validationResult);
            Assert.Equal(0, validationResult.Count);
            Assert.Equal(1, testJson.SelectToken("body.numberContent.value").Value<int>());
        }


        [Fact]
        public async Task PropertyWithFloatMinimumDefinition()
        {
            //// Arrange
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
            //// Act
            var testJson = generator.Generate(schema);

            //// Assert
            var validationResult = schema.Validate(testJson);
            Assert.NotNull(validationResult);
            Assert.Equal(0, validationResult.Count);
            Assert.Equal(1.000012, testJson.SelectToken("body.numberContent.value").Value<double>());
        }

        [Fact]
        public async Task PropertyWithDefaultDefiniton()
        {
            //// Arrange
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
            //// Act
            var testJson = generator.Generate(schema);

            //// Assert
            var validationResult = schema.Validate(testJson);
            Assert.NotNull(validationResult);
            Assert.Equal(0, validationResult.Count);
            Assert.Equal(42, testJson.SelectToken("body.numberContent.value").Value<int>());
        }

        //exclusiveMaximum

        [Fact]
        public async Task PropertyExclusiveMinimumDefiniton()
        {
            //// Arrange
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
            //// Act
            var testJson = generator.Generate(schema);

            //// Assert
            var validationResult = schema.Validate(testJson);
            Assert.NotNull(validationResult);
            Assert.Equal(0, validationResult.Count);
            Assert.Equal(1.1, testJson.SelectToken("body.numberContent.value").Value<double>());
        }
    }
}
