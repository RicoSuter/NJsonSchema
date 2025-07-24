using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.Annotations;
using NJsonSchema.CodeGeneration.Tests;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class GeneralGeneratorTests
    {
        [Fact]
        public async Task When_type_is_array_and_items_and_item_is_not_defined_then_any_array_is_generated()
        {
            // Arrange
            var json = @"{
                'required': [ 'emptySchema' ],
                'properties': {
                    'emptySchema': { 'type': 'array' }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var settings = new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco, Namespace = "ns", };
            var generator = new CSharpGenerator(schema, settings);
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_code_is_generated_then_toolchain_version_is_printed()
        {
            // Arrange
            var json = @"{
                'required': [ 'emptySchema' ],
                'properties': {
                    'emptySchema': { 'type': 'array' }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var settings = new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco, Namespace = "ns", };
            var generator = new CSharpGenerator(schema, settings);
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        private class CustomPropertyNameGenerator : IPropertyNameGenerator
        {
            public string Generate(JsonSchemaProperty property)
            {
                return "MyCustom" + ConversionUtilities.ConvertToUpperCamelCase(property.Name, true);
            }
        }

        private class CustomTypeNameGenerator : ITypeNameGenerator
        {
            public string Generate(JsonSchema schema, string typeNameHint, IEnumerable<string> reservedTypeNames)
            {
                return "MyCustomType" + ConversionUtilities.ConvertToUpperCamelCase(typeNameHint, true);
            }
        }

        public class Teacher : Person
        {
            public string Class { get; set; }
        }

        public class Person
        {
            [Required]
            public string FirstName { get; set; }

            [JsonProperty("lastName")]
            public string LastName { get; set; }

            [ReadOnly(true)]
            public DateTime Birthday { get; set; }

            public TimeSpan TimeSpan { get; set; }

            public TimeSpan? TimeSpanOrNull { get; set; }

            public Gender Gender { get; set; }

            public Gender? GenderOrNull { get; set; }

            public Address Address { get; set; }

            [CanBeNull]
            public Address AddressOrNull { get; set; }

            public List<string> Array { get; set; }

            public Dictionary<string, int> Dictionary { get; set; }
        }

        public enum Gender
        {
            Male,
            Female
        }

        public class Address
        {
            public string Street { get; set; }

            public string City { get; set; }
        }

        [Fact]
        public async Task When_property_name_is_created_by_custom_fun_then_attribute_is_correct()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Teacher>();
            var schemaData = schema.ToJson();
            var settings = new CSharpGeneratorSettings
            {
                TypeNameGenerator = new CustomTypeNameGenerator(),
                PropertyNameGenerator = new CustomPropertyNameGenerator()
            };

            var generator = new CSharpGenerator(schema, settings);

            // Act
            var output = generator.GenerateFile("Teacher");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_property_name_is_created_by_custom_fun_then_parameter_name_is_correct_for_record()
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Address>();
            var schemaData = schema.ToJson();
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Record,
                PropertyNameGenerator = new CustomPropertyNameGenerator(),
            };
            var generator = new CSharpGenerator(schema, settings);

            //// Act
            var output = generator.GenerateFile("Address");

            //// Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_schema_contains_ref_to_definition_that_refs_another_definition_then_result_should_contain_correct_target_ref_type()
        {
            // Arrange
            var schemaJson =
@"{
	'x-typeName': 'foo',
	'type': 'object',
	'definitions': {
		'pRef': {
			'type': 'object',
			'properties': {
				'pRef2': {
					'$ref': '#/definitions/pRef2'
				},
				
			}
		},
		'pRef2': {
			'type': 'string'
		}
	},
	'properties': {
		'pRefs': {
			'type': 'array',
			'items': {
				'$ref': '#/definitions/pRef'
			}
		}
	}
}";

            var schema = await JsonSchema.FromJsonAsync(schemaJson);
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco
            };
            var gen = new CSharpGenerator(schema, settings);

            // Act
            var output = gen.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_namespace_is_set_then_it_should_appear_in_output()
        {
            // Arrange
            var generator = await CreateGeneratorAsync();

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_POCO_is_set_then_auto_properties_are_generated()
        {
            // Arrange
            var generator = await CreateGeneratorAsync();
            generator.Settings.ClassStyle = CSharpClassStyle.Poco;

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_property_name_does_not_match_property_name_then_attribute_is_correct()
        {
            // Arrange
            var generator = await CreateGeneratorAsync();

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_property_is_timespan_than_csharp_timespan_is_used()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Person>();
            var data = schema.ToJson();
            var generator = new CSharpGenerator(schema);

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_allOf_contains_one_schema_then_csharp_inheritance_is_generated()
        {
            // Arrange
            var generator = await CreateGeneratorAsync();

            // Act
            var output = generator.GenerateFile("Teacher");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_enum_has_description_then_csharp_has_xml_comment()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Teacher>();
            schema.AllOf.First().ActualSchema.Properties["Gender"].Description = "EnumDesc.";
            var generator = new CSharpGenerator(schema);

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_class_has_description_then_csharp_has_xml_comment()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Teacher>();
            schema.ActualSchema.Description = "ClassDesc.";
            var generator = new CSharpGenerator(schema);

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_property_has_description_then_csharp_has_xml_comment()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Teacher>();
            schema.ActualProperties["Class"].Description = "PropertyDesc.";
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        public class File
        {
            public byte[] Content { get; set; }
        }

        [Fact]
        public async Task Can_generate_type_from_string_property_with_byte_format()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<File>();
            var generator = new CSharpGenerator(schema);

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task Can_generate_type_from_string_property_with_base64_format()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<File>();
            schema.Properties["Content"].Format = "base64";
            var generator = new CSharpGenerator(schema);

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_name_contains_dash_then_it_is_converted_to_upper_case()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Properties["foo-bar"] = new JsonSchemaProperty
            {
                Type = JsonObjectType.String
            };

            var generator = new CSharpGenerator(schema);

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Theory]
        [InlineData("foo@bar", "Foobar1")]
        [InlineData("foo$bar", "Foobar2")]
        [InlineData("foobars[]", "Foobars")]
        [InlineData("foo.bar", "FooBar3")]
        [InlineData("foo=bar", "FooBar4")]
        [InlineData("foo+bar", "Fooplusbar")]
        [InlineData("foo*bar", "FooStarbar")]
        [InlineData("foo:bar", "Foo_bar")]
        public async Task When_name_contains_unallowed_characters_then_they_are_converted_to_valid_csharp(string jsonPropertyName, string testCaseName)
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Properties[jsonPropertyName] = new JsonSchemaProperty
            {
                Type = JsonObjectType.String
            };

            var generator = new CSharpGenerator(schema);

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output).UseParameters(testCaseName);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_type_name_is_missing_then_anonymous_name_is_generated()
        {
            // Arrange
            var schema = new JsonSchema();
            var generator = new CSharpGenerator(schema);

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        private static Task<CSharpGenerator> CreateGeneratorAsync()
        {
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Teacher>();
            var schemaData = schema.ToJson();
            var settings = new CSharpGeneratorSettings
            {
                Namespace = "MyNamespace"
            };
            var generator = new CSharpGenerator(schema, settings);
            return Task.FromResult(generator);
        }


        private class ObjectTestClass
        {
            public object Foo { get; set; }
        }

        [Fact]
        public void When_property_is_object_then_any_type_is_generated()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ObjectTestClass>();

            // Assert
            Assert.Equal(
@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#""
}".Replace("\r", string.Empty), schema.Properties["Foo"].ActualTypeSchema.ToJson().Replace("\r", string.Empty));
        }

        [Fact]
        public async Task When_property_is_object_then_object_property_is_generated()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ObjectTestClass>();
            var json = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        public enum ConstructionCode
        {
            FIRE_RSTV = 0,
            FRAME = 1,
            JOIST_MAS = 2,
            NON_CBST = 3,
        }

        public class ClassWithDefaultEnumProperty
        {
            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(GeneralGeneratorTests.ConstructionCode.NON_CBST)]
            public ConstructionCode ConstructionCode { get; set; }
        }

        [Fact]
        public async Task When_enum_property_has_default_and_int_serialization_then_correct_csharp_code_generated()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithDefaultEnumProperty>();
            var schemaJson = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "Foo"
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_enum_property_has_default_and_string_serialization_then_correct_csharp_code_generated()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithDefaultEnumProperty>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SerializerSettings =
                {
                    Converters = { new StringEnumConverter() }
                }
            });

            var schemaJson = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "Foo"
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_enum_type_name_is_missing_then_default_value_is_still_correctly_set()
        {
            // Arrange
            var schemaJson = @"{
  ""type"": ""object"",
  ""additionalProperties"": false,
  ""properties"": {
    ""ConstructionCode"": {
      ""type"": ""integer"",
      ""x-enumNames"": [
        ""FIRE_RSTV"",
        ""FRAME"",
        ""JOIST_MAS"",
        ""NON_CBST""
      ],
      ""enum"": [
        ""FIRE_RSTV"",
        ""FRAME"",
        ""JOIST_MAS"",
        ""NON_CBST""
      ],
      ""default"": ""JOIST_MAS""
    }
  }
}";
            var schema = await JsonSchema.FromJsonAsync(schemaJson);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "Foo"
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
        }

        [Fact]
        public async Task When_property_has_same_name_as_class_then_it_is_renamed()
        {
            // Arrange
            var schemaJson = @"{
  ""type"": ""object"",
  ""properties"": {
    ""Foo"": {
      ""type"": ""string""
    }
  }
}";
            var schema = await JsonSchema.FromJsonAsync(schemaJson);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("Foo");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_patternProperties_is_set_with_string_value_type_then_correct_dictionary_is_generated()
        {
            // Arrange
            var schemaJson = @"{
                ""required"": [ ""dict"" ],
                ""properties"": {
                    ""dict"": {
                        ""type"": ""object"",
                        ""additionalProperties"": false,
                        ""patternProperties"": {
                            ""^[a-zA-Z_$][a-zA-Z_$0-9]*$"": {
                                ""type"": ""string""
                            }
                        }
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(schemaJson);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_object_has_generic_name_then_it_is_transformed()
        {
            // Arrange
            var schema = new JsonSchema
            {
                Type = JsonObjectType.Object,
                Properties =
                {
                    ["foo"] = new JsonSchemaProperty
                    {
                        Type = JsonObjectType.Number
                    }
                }
            };

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("Foo[Bar[Inner]]");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [JsonObject(MemberSerialization.OptIn)]
        [GeneratedCode("NJsonSchema", "3.4.6065.33501")]
        public partial class Person2
        {
            [JsonProperty("FirstName", Required = Required.Always)]
            [Required]
            public string FirstName { get; set; }

            [JsonProperty("MiddleName", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
            public string MiddleName { get; set; }

            [JsonProperty("Age", Required = Required.AllowNull)]
            public int? Age { get; set; }
        }

        [Fact]
        public async Task When_property_is_required_then_CSharp_code_is_correct()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Person2>();
            var schemaJson = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains(@"  ""required"": [
    ""FirstName"",
    ""Age""
  ],
  ""properties"": {
    ""FirstName"": {
      ""type"": ""string"",
      ""minLength"": 1
    },
    ""MiddleName"": {
      ""type"": ""string""
    },
    ""Age"": {
      ""type"": [
        ""integer"",
        ""null""
      ],
      ""format"": ""int32""
    }
  }".Replace("\r", string.Empty), schemaJson.Replace("\r", string.Empty));

            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_array_property_is_required_or_not_then_the_code_has_correct_initializer()
        {
            // Arrange
            var schema = new JsonSchema
            {
                Properties =
                {
                    { "A", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Array,
                            Item = new JsonSchema
                            {
                                Type = JsonObjectType.String
                            },
                            IsRequired = true
                        }
                    },
                    { "B", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Array,
                            Item = new JsonSchema
                            {
                                Type = JsonObjectType.String
                            },
                            IsRequired = false
                        }
                    },
                }
            };

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_dictionary_property_is_required_or_not_then_the_code_has_correct_initializer()
        {
            // Arrange
            var schema = new JsonSchema
            {
                Properties =
                {
                    { "A", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Object,
                            AdditionalPropertiesSchema = new JsonSchema
                            {
                                Type = JsonObjectType.String
                            },
                            IsRequired = true
                        }
                    },
                    { "B", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Object,
                            AdditionalPropertiesSchema = new JsonSchema
                            {
                                Type = JsonObjectType.String
                            },
                            IsRequired = false
                        }
                    },
                }
            };

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_object_property_is_required_or_not_then_the_code_has_correct_initializer()
        {
            // Arrange
            var schema = new JsonSchema
            {
                Properties =
                {
                    { "A", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Object,
                            Properties =
                            {
                                {"A", new JsonSchemaProperty
                                    {
                                        Type = JsonObjectType.String
                                    }
                                }
                            },
                            IsRequired = true
                        }
                    },
                    { "B", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Object,
                            Properties =
                            {
                                {"A", new JsonSchemaProperty
                                    {
                                        Type = JsonObjectType.String
                                    }
                                }
                            },
                            IsRequired = false
                        }
                    },
                }
            };

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_is_named_Object_then_JObject_is_generated()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""foo"": {
			""$ref"": ""#/definitions/Object""
		}
	},
	""definitions"": {
		""Object"": {
			""type"": ""object"",
			""properties"": {}
		}
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                InlineNamedAny = true
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        public class ObsClass
        {
            public ObservableCollection<string> Test { get; set; }
        }

        [Fact]
        public async Task When_property_is_ObservableCollection_then_generated_code_uses_the_same_class()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ObsClass>();
            var settings = new CSharpGeneratorSettings { ArrayType = "ObservableCollection" };
            var generator = new CSharpGenerator(schema, settings);

            // Act
            var output = generator.GenerateFile("MyClass");
            //Console.WriteLine(output);

            // Assert
            await VerifyHelper.Verify(output);
        }

        [Fact]
        public async Task When_enum_has_special_chars_then_they_should_be_converted()
        {
            // Arrange
            var schemaJson = @"{ ""type"": ""string"", ""enum"": [""application/json"",""application/vnd.ms-excel""] }";
            var schema = await JsonSchema.FromJsonAsync(schemaJson);

            var settings = new CSharpGeneratorSettings();
            var generator = new CSharpGenerator(schema, settings);

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_enum_has_special_char_questionmark_then_it_should_be_converted()
        {
            // Arrange
            var schemaJson = @"{ ""type"": ""string"", ""enum"": [""application/json"",""application/vnd.ms-excel?2""] }";
            var schema = await JsonSchema.FromJsonAsync(schemaJson);

            var settings = new CSharpGeneratorSettings();
            var generator = new CSharpGenerator(schema, settings);

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_property_has_not_supported_characters_then_they_are_removed()
        {
            // Arrange
            var schemaJson =
@"{
  ""type"": ""object"",
  ""properties"": {
    ""@odata.context"": { ""type"": ""string"" }
  }
}";
            var schema = await JsonSchema.FromJsonAsync(schemaJson);

            var settings = new CSharpGeneratorSettings();
            var generator = new CSharpGenerator(schema, settings);

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_definition_contains_minimum_a_range_attribute_is_added_with_minimum_and_max_double_maximum()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""foo"": {
		  ""type"": ""integer"",
		  ""minimum"": ""1""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_maximum_a_range_attribute_is_added_with_min_double_minimum_and_maximum()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""foo"": {
		  ""type"": ""integer"",
		  ""maximum"": ""10""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_both_minimum_and_maximum_a_range_attribute_is_added()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""foo"": {
		  ""type"": ""integer"",
		  ""minimum"": ""1"",
		  ""maximum"": ""10""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_maximum_a_range_attribute_is_not_added_for_anything_but_type_number_or_integer()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""foo"": {
		  ""type"": ""string"",
		  ""maximum"": ""10""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_min_length_a_string_length_attribute_is_added()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""foo"": {
		  ""type"": ""string"",
		  ""minLength"": ""10""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_max_length_a_string_length_attribute_is_added()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""foo"": {
		  ""type"": ""string"",
		  ""maxLength"": ""20""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_both_min_and_max_length_a_string_length_attribute_is_added()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""foo"": {
		  ""type"": ""string"",
		  ""minLength"": ""10"",
		  ""maxLength"": ""20""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_both_min_length_a_string_length_attribute_is_added_only_for_type_string()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""foo"": {
		  ""type"": ""number"",
		  ""minLength"": ""10"",
		  ""maxLength"": ""20""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_both_min_items_and_max_items_a_min_length_and_max_length_attributes_are_added_only_for_type_array()
        {
            // Arrange
            var json =
                @"{
	""type"": ""object"",
	""properties"": {
		""foo"": {
		  ""type"": ""array"",
		  ""minItems"": ""10"",
		  ""maxItems"": ""20""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_pattern_a_regular_expression_attribute_is_added()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""foo"": {
		  ""type"": ""string"",
		  ""pattern"": ""^[a-zA-Z''-'\\s]{1,40}$""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_pattern_but_type_is_not_string_a_regular_expression_should_not_be_added()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""foo"": {
		  ""type"": ""number"",
		  ""pattern"": ""^[a-zA-Z''-'\\s]{1,40}$""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_restrictions_but_render_data_annotations_is_set_to_false_they_should_not_be_included()
        {
            // Arrange
            var json =
                @"{
	""type"": ""object"",
	""properties"": {
		""a"": {
		  ""type"": ""integer"",
		  ""minimum"": ""1"",
		  ""maximum"": ""10""
        },
		""b"": {
		  ""type"": ""string"",
		  ""minLength"": ""10"",
		  ""maxLength"": ""20""
        },
		""c"": {
		  ""type"": ""string"",
		  ""pattern"": ""^[a-zA-Z''-'\\s]{1,40}$""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                // define that no data annotations should be included
                GenerateDataAnnotations = false
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        public class MyByteTest
        {
            public byte? Cell { get; set; }
        }

        [Fact]
        public async Task When_property_is_byte_then_its_type_is_preserved()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyByteTest>();
            var json = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        public class MyRequiredNullableTest
        {
            [Required]
            public int? Foo { get; set; }
        }

        [Fact]
        public async Task When_nullable_property_is_required_then_it_is_not_nullable_in_generated_csharp_code()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyRequiredNullableTest>();
            var json = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_date_converter_should_be_added_for_datetime()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""a"": {
    		""type"": ""string"",
            ""format"": ""date""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                DateType = "System.DateTime"
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_date_and_use_system_text_json_then_converter_should_be_added_for_datetime()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""a"": {
    		""type"": ""string"",
            ""format"": ""date""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                DateType = "System.DateTime",
                JsonLibrary = CSharpJsonLibrary.SystemTextJson
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_no_typeNameHint_is_available_then_title_is_used_as_class_name()
        {
            // Arrange
            var json = @"{
  ""type"": ""object"",
  ""title"": ""MyTestClass"",
  ""properties"": {
    ""Endpoints"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""title"": ""Endpoint"",
        ""properties"": {
          ""url"": {
            ""type"": ""string"",
            ""retries"": ""integer""
          }
        }
      }
    }
  }
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                DateType = "System.DateTime"
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task When_tuple_types_has_ints_then_it_is_generated_correctly(bool inlineNamedTuples)
        {
            // Arrange
            var json = @"
{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""title"": ""MySchema"",
  ""type"": ""object"",
  ""required"": [
    ""OuterList""
  ],
  ""properties"": {
    ""OuterList"": {
      ""$ref"": ""#/definitions/OuterList""
    }
  },
  ""additionalProperties"": false,
  ""definitions"": {
    ""InnerList"": {
      ""description"": ""Only ever has 2 items"",
      ""type"": ""array"",
      ""minItems"": 2,
      ""maxItems"": 2,
      ""items"": [
        {
          ""type"": ""integer""
        },
        {
          ""type"": ""integer""
        }
      ],
      ""additionalItems"": false
    },
    ""OuterList"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/definitions/InnerList""
      }
    }
  }
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                DateType = "System.DateTime",
                InlineNamedTuples = inlineNamedTuples
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code).UseParameters(inlineNamedTuples);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_date_converter_should_be_added_for_datetimeoffset()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""a"": {
    		""type"": ""string"",
            ""format"": ""date""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                DateType = "System.DateTimeOffset"
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_date_and_use_system_text_json_then_converter_should_be_added_for_datetimeoffset()
        {
            // Arrange
            var json =
@"{
	""type"": ""object"",
	""properties"": {
		""a"": {
    		""type"": ""string"",
            ""format"": ""date""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                DateType = "System.DateTimeOffset",
                JsonLibrary = CSharpJsonLibrary.SystemTextJson
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_datetime_converter_should_not_be_added()
        {
            // Arrange
            var json =
                @"{
	""type"": ""object"",
	""properties"": {
		""a"": {
    		""type"": ""string"",
            ""format"": ""date-time""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                DateType = "System.DateTime"
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_definition_contains_datetime_and_use_system_text_json_then_converter_should_not_be_added()
        {
            // Arrange
            var json =
                @"{
	""type"": ""object"",
	""properties"": {
		""a"": {
    		""type"": ""string"",
            ""format"": ""date-time""
        }
	}
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                DateType = "System.DateTime",
                JsonLibrary = CSharpJsonLibrary.SystemTextJson
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_record_no_setter_in_class_and_constructor_provided()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Address>();
            var data = schema.ToJson();
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Record
            });

            // Act
            var output = generator.GenerateFile("Address");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_native_record_no_setter_in_class_and_constructor_provided()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Address>();
            var data = schema.ToJson();
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Record,
                GenerateNativeRecords = true
            });

            // Act
            var output = generator.GenerateFile("Address");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        public abstract class AbstractAddress
        {
            [JsonProperty("city")]
            [DefaultValue("Innsmouth")]
            public string CityName { get; set; }

            [JsonProperty("streetName")]
            public string StreetName { get; set; }
        }

        public class PostAddress : AbstractAddress
        {
            public string Zip { get; set; }
            public int HouseNumber { get; set; }
        }

        public class PersonAddress : PostAddress
        {
            public string Addressee { get; set; }
        }

        [Fact]
        public async Task When_class_is_abstract_constructor_is_protected_for_record()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AbstractAddress>();
            var data = schema.ToJson();
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Record,
            });

            // Act
            var output = generator.GenerateFile("AbstractAddress");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_record_has_inheritance()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<PersonAddress>();
            var data = schema.ToJson();
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Record
            });

            // Act
            var output = generator.GenerateFile("PersonAddress");

            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        public class ClassWithExtensionData
        {
            public string Foo { get; set; }

            [JsonExtensionData]
            public IDictionary<string, object> ExtensionData { get; set; }
        }

        [Fact]
        public async Task When_schema_has_AdditionProperties_schema_then_JsonExtensionDataAttribute_is_generated()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithExtensionData>(new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();

            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco
            });

            // Act
            var output = generator.GenerateFile("PersonAddress");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public void When_schema_has_negative_value_of_enum_it_is_generated_in_CSharp_and_TypeScript_correctly()
        {
            // Arrange
            var settings = new CSharpGeneratorSettings { EnumNameGenerator = new DefaultEnumNameGenerator() };
            var generator = new CSharpGenerator(null, settings);

            // Act
            var schema = new JsonSchema()
            {
                Type = JsonObjectType.Integer,
                Enumeration =
                {
                    0,
                    1,
                    2,
                    -1,
                },
                Default = "-1"
            };

            var types = generator.GenerateTypes(schema, "MyEnum");

            // Assert
            Assert.Contains("_1 = 1", types.First().Code);
            Assert.Contains("__1 = -1", types.First().Code);
        }

        [Fact]
        public async Task When_using_SystemTextJson_without_JsonConverters_generates_FromJson_and_ToJson_correctly()
        {
            // Arrange
            var generator = await CreateGeneratorAsync();
            generator.Settings.JsonLibrary = CSharpJsonLibrary.SystemTextJson;
            generator.Settings.GenerateJsonMethods = true;

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_using_SystemTextJson_with_JsonConverters_generates_FromJson_and_ToJson_correctly()
        {
            // Arrange
            var generator = await CreateGeneratorAsync();
            generator.Settings.JsonLibrary = CSharpJsonLibrary.SystemTextJson;
            generator.Settings.GenerateJsonMethods = true;
            generator.Settings.JsonConverters = ["CustomConverter1", "CustomConverter2"];

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
        }

        [Fact]
        public async Task When_using_NewtonsoftJson_without_JsonConverters_generates_FromJson_and_ToJson_correctly()
        {
            // Arrange
            var generator = await CreateGeneratorAsync();
            generator.Settings.JsonLibrary = CSharpJsonLibrary.NewtonsoftJson;
            generator.Settings.GenerateJsonMethods = true;

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_using_NewtonsoftJson_with_JsonConverters_generates_FromJson_and_ToJson_correctly()
        {
            // Arrange
            var generator = await CreateGeneratorAsync();
            generator.Settings.JsonLibrary = CSharpJsonLibrary.NewtonsoftJson;
            generator.Settings.GenerateJsonMethods = true;
            generator.Settings.JsonConverters = ["CustomConverter1", "CustomConverter2"];

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
        }

        public class DocumentationTest
        {
            /// <summary>
            /// Summary is here
            ///
            /// spanning multiple lines
            ///
            /// like this.
            ///
            /// </summary>
            public string HelloMessage { get; set; }
        }

        [Fact]
        public async Task When_documentation_present_produces_valid_xml_documentation_syntax()
        {
            // Arrange
            var schema = JsonSchema.FromType<DocumentationTest>();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco
            });

            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }
    }
}
