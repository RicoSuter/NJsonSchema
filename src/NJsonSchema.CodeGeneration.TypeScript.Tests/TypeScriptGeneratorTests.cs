using NJsonSchema.CodeGeneration.TypeScript.Tests.Models;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

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
            var schema = await JsonSchema4.FromJsonAsync(json);
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });
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
            var schema = await JsonSchema4.FromJsonAsync(json);
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
            Assert.Contains(@"Dictionary: { [key: string] : number; };", output);
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
            var schema = await JsonSchema4.FromTypeAsync<Teacher>();
            schema.AllOf.First().ActualSchema.Properties["Gender"].Description = "EnumDesc.";
            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains(@"/** EnumDesc. *", output);
        }

        [Fact]
        public async Task When_class_has_description_then_typescript_has_comment()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Teacher>();
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
            var schema = await JsonSchema4.FromTypeAsync<Teacher>();
            schema.Properties["Class"].Description = "PropertyDesc.";
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
            var schema = await JsonSchema4.FromTypeAsync<Teacher>();
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
            var schema = new JsonSchema4();
            schema.Properties["foo-bar"] = new JsonProperty
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
            var schema = new JsonSchema4();

            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile("MyClass");

            //// Assert
            Assert.DoesNotContain(@"interface  {", output);
        }

        private static async Task<TypeScriptGenerator> CreateGeneratorAsync()
        {
            var schema = await JsonSchema4.FromTypeAsync<Teacher>();
            var schemaData = schema.ToJson();
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            return generator;
        }

        [Fact]
        public async Task When_patternProperties_is_set_with_string_value_type_then_correct_dictionary_is_generated()
        {
            //// Arrange
            var schemaJson = @"{
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

            var schema = await JsonSchema4.FromJsonAsync(schemaJson);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("dict: { [key: string] : string; };", code); // property not nullable
            Assert.Contains("this.dict = {};", code); // must be initialized with {}
        }
    }
}
