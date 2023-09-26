using System.Collections.Generic;
using NJsonSchema.CodeGeneration.TypeScript.Tests.Models;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class TypeScriptGeneratorTests
    {
        [Fact]
        public async Task When_more_properties_are_defined_in_allOf_and_type_none_then_all_of_contains_all_properties_in_generated_code()
        {
            //// Arrange
            var json = @"{
                '$schema': 'http://json-schema.org/draft-04/schema#',
                'type': 'object',
                'properties': { 
                    'prop1' : { 'type' : 'string' } 
                },
                'allOf': [
                    {
                        'type': 'object', 
                        'properties': { 
                            'baseProperty' : { 'type' : 'string' } 
                        }
                    },
                    {
                        'properties': { 
                            'prop2' : { 'type' : 'string' } 
                        }
                    }
                ]
            }";

            //// Act
            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                TypeScriptVersion = 1.8m
            });
            var code = generator.GenerateFile("Foo").Replace("\r\n", "\n");

            //// Assert
            Assert.Contains(@"export class Foo extends Anonymous implements IFoo {
    prop1: string;
    prop2: string;
".Replace("\r", string.Empty), code);
            Assert.Contains("class Anonymous", code);
        }

        [Fact]
        public async Task When_allOf_schema_is_object_type_then_it_is_an_inherited_class_in_generated_code()
        {
            //// Arrange
            var json = @"{
                '$schema': 'http://json-schema.org/draft-04/schema#',
                'type': 'object',
                'properties': { 
                    'prop1' : { 'type' : 'string' } 
                },
                'allOf': [
                    {
                        '$ref': '#/definitions/Bar'
                    }
                ], 
                'definitions': {
                    'Bar':  {
                        'type': 'object', 
                        'properties': { 
                            'prop2' : { 'type' : 'string' } 
                        }
                    }
                }
            }";

            //// Act
            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });
            var code = generator.GenerateFile("Foo");

            //// Assert
            Assert.Contains("class Foo extends Bar", code);
        }

        [Fact]
        public async Task When_property_name_does_not_match_property_name_then_casing_is_correct_in_output()
        {
            //// Arrange
            var generator = await CreateGeneratorAsync();

            //// Act
            var output = generator.GenerateFile("Teacher");

            //// Assert
            Assert.Contains(@"lastName: string;", output);
            Assert.Contains(@"Dictionary: { [key: string]: number; };", output);
        }

        [Fact]
        public async Task When_property_is_required_name_then_TypeScript_property_is_not_optional()
        {
            //// Arrange
            var generator = await CreateGeneratorAsync();

            //// Act
            var output = generator.GenerateFile("Teacher");

            //// Assert
            Assert.Contains(@"FirstName: string;", output);
        }

        [Fact]
        public async Task When_allOf_contains_one_schema_then_csharp_inheritance_is_generated()
        {
            //// Arrange
            var generator = await CreateGeneratorAsync();

            //// Act
            var output = generator.GenerateFile("Teacher");

            //// Assert
            Assert.Contains(@"interface Teacher extends Person", output);
        }

        [Fact]
        public async Task When_enum_has_description_then_typescript_has_comment()
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Teacher>();
            schema.AllOf.First().ActualSchema.Properties["Gender"].Description = "EnumDesc.";
            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains(@"/** EnumDesc. *", output);
        }

        /// <summary>
        /// This test asserts the fix for issue #1618
        /// </summary>
        [Fact]
        public async Task When_enum_has_default_and_using_enumstyle_stringliteral_it_defaults_to_stringliteral()
        {
            //// Arrange
            var jsonSchema = @"
                {
                    ""$schema"": ""http://json-schema.org/draft-04/schema#"",
                    ""openapi"": ""3.1.0"",
                    ""title"": ""TShirt"",
                    ""type"": ""object"",
                    ""properties"": {
                        ""color"": {
                            ""type"": ""string"",
                            ""default"": ""green"",
                            ""enum"": [""red"", ""green"", ""blue"", ""black""]
                        }
                    }
                }";

            var schema = await JsonSchema.FromJsonAsync(jsonSchema);
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                EnumStyle = TypeScriptEnumStyle.StringLiteral,
                TypeScriptVersion = 5m,
            });

            //// Act
            var code = generator.GenerateFile("MyFile");

            //// Assert
            Assert.Contains("export type MyFileColor = \"red\" | \"green\" | \"blue\" | \"black\";", code);
            Assert.Contains("this.color = _data[\"color\"] !== undefined ? _data[\"color\"] : \"green\";", code);
            Assert.Contains("this.color = \"green\";", code);

            // This is the old code gen that used the enum prior to the fix for #1618
            Assert.DoesNotContain("Color.Green", code);
        }

        [Fact]
        public async Task When_class_has_description_then_typescript_has_comment()
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Teacher>();
            schema.Description = "ClassDesc.";
            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains(@"/** ClassDesc. *", output);
        }

        [Fact]
        public async Task When_property_has_description_then_csharp_has_xml_comment()
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Teacher>();
            schema.ActualProperties["Class"].Description = "PropertyDesc.";
            var json = schema.ToJson();

            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains(@"/** PropertyDesc. *", output);
        }

        [Fact]
        public async Task When_property_is_readonly_then_ts_property_is_also_readonly()
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Teacher>();
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                TypeScriptVersion = 2.0m
            });

            //// Act
            var output = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains(@"readonly Birthday", output);
        }

        [Fact]
        public void When_name_contains_dash_then_it_is_converted_to_upper_case()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Properties["foo-bar"] = new JsonSchemaProperty
            {
                Type = JsonObjectType.String
            };

            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });

            //// Act
            var output = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains(@"""foo-bar"": string;", output);
        }

        [Fact]
        public void When_type_name_is_missing_then_anonymous_name_is_generated()
        {
            //// Arrange
            var schema = new JsonSchema();

            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile("MyClass");

            //// Assert
            Assert.DoesNotContain(@"interface  {", output);
        }

        private static async Task<TypeScriptGenerator> CreateGeneratorAsync()
        {
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Teacher>();
            var schemaData = schema.ToJson();
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                TypeScriptVersion = 1.8m
            });
            return generator;
        }

        [Fact]
        public async Task When_patternProperties_is_set_with_string_value_type_then_correct_dictionary_is_generated()
        {
            //// Arrange
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

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("dict: { [key: string]: string; };", code); // property not nullable
            Assert.Contains("this.dict = {};", code); // must be initialized with {}
        }

        [Fact]
        public async Task When_default_is_generated_then_no_liquid_error_is_in_output()
        {
            var json = @"{
""type"": ""object"",
""properties"": {
  ""foo"": {
    ""$ref"": ""#/definitions/Person""
  }
},
""definitions"": {
    ""Person"": {
      ""type"": ""object"",
      ""discriminator"": ""discriminator"",
      ""additionalProperties"": false,
      ""required"": [
        ""Id"",
        ""FirstName"",
        ""LastName"",
        ""Gender"",
        ""DateOfBirth"",
        ""Weight"",
        ""Height"",
        ""Age"",
        ""AverageSleepTime"",
        ""Address"",
        ""Children"",
        ""discriminator""
      ],
      ""properties"": {
        ""Id"": {
          ""type"": ""string"",
          ""format"": ""guid""
        },
        ""FirstName"": {
          ""type"": ""string"",
          ""description"": ""Gets or sets the first name."",
          ""minLength"": 2
        },
        ""LastName"": {
          ""type"": ""string"",
          ""description"": ""Gets or sets the last name."",
          ""minLength"": 1
        },
        ""DateOfBirth"": {
          ""type"": ""string"",
          ""format"": ""date-time""
        },
        ""Weight"": {
          ""type"": ""number"",
          ""format"": ""decimal""
        },
        ""Height"": {
          ""type"": ""number"",
          ""format"": ""double""
        },
        ""Age"": {
          ""type"": ""integer"",
          ""format"": ""int32"",
          ""maximum"": 99.0,
          ""minimum"": 5.0
        },
        ""AverageSleepTime"": {
          ""type"": ""string"",
          ""format"": ""time-span""
        },
        ""Children"": {
          ""type"": ""array"",
          ""items"": {
            ""$ref"": ""#/definitions/Person""
          }
        },
        ""discriminator"": {
          ""type"": ""string""
        }
      }
    }
  }
}";

            var schema = await JsonSchema.FromJsonAsync(json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.DoesNotContain("Liquid error: ", code);
        }
        
        [Fact]
        public async Task When_a_nullable_array_property_exists_and_typestyle_is_null_then_init_should_assign_null()
        {
            //// Arrange
            var schema = new JsonSchema
            {
                Properties =
                {
                    { "Prop", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Array,
                            Item = new JsonSchema
                            {
                                Type = JsonObjectType.String
                            },
                            IsRequired = true,
                            IsNullableRaw = true
                        }
                    },
                }
            };

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                NullValue = TypeScriptNullValue.Null,
                TypeScriptVersion = 4,
                MarkOptionalProperties = false,
            });
            var code = generator.GenerateFile("Foo").Replace("\r\n", "\n");

            //// Assert
            Assert.Matches(new Regex(
                @"init\(.*\)\s{.*}\s*else\s{\s*this\.prop\s=\s<any>null;\s*}", RegexOptions.Singleline),
                code);
        }
        
        [Fact]
        public async Task When_a_nullable_dict_property_exists_and_typestyle_is_null_then_init_should_assign_null()
        {
            //// Arrange
            var schema = new JsonSchema
            {
                Properties =
                {
                    { "Prop", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Object,
                            AdditionalPropertiesSchema = new JsonSchema
                            {
                                Properties = { }
                            },
                            IsRequired = true,
                            IsNullableRaw = true
                        }
                    },
                }
            };

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                NullValue = TypeScriptNullValue.Null,
                TypeScriptVersion = 4,
                MarkOptionalProperties = false,
            });
            var code = generator.GenerateFile("Foo").Replace("\r\n", "\n");

            //// Assert
            Assert.Matches(new Regex(
                    @"init\(.*\)\s{.*}\s*else\s{\s*this\.prop\s=\s<any>null;\s*}", RegexOptions.Singleline),
                code);
        }
    }
}
