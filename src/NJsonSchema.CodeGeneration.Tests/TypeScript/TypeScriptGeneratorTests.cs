using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.Tests.Models;
using NJsonSchema.CodeGeneration.TypeScript;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class TypeScriptGeneratorTests
    {
        [TestMethod]
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
                        'properties': { 
                            'prop2' : { 'type' : 'string' } 
                        }
                    }
                ]
            }";

            //// Act
            var schema = await JsonSchema4.FromJsonAsync(json);
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });
            var code = generator.GenerateFile("Foo");

            //// Assert
            Assert.IsTrue(code.Contains("class Foo"));
            Assert.IsTrue(code.Contains("prop1: string;"));
            Assert.IsTrue(code.Contains("prop2: string;"));
            Assert.IsFalse(code.Contains("class Anonymous"));
        }

        [TestMethod]
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
            Assert.IsTrue(code.Contains("class Foo extends Bar"));
        }

        [TestMethod]
        public async Task When_property_name_does_not_match_property_name_then_casing_is_correct_in_output()
        {
            //// Arrange
            var generator = await CreateGeneratorAsync();

            //// Act
            var output = generator.GenerateFile("Teacher");

            //// Assert
            Assert.IsTrue(output.Contains(@"lastName: string;"));
            Assert.IsTrue(output.Contains(@"Dictionary: { [key: string] : number; };"));
        }

        [TestMethod]
        public async Task When_property_is_required_name_then_TypeScript_property_is_not_optional()
        {
            //// Arrange
            var generator = await CreateGeneratorAsync();

            //// Act
            var output = generator.GenerateFile("Teacher");

            //// Assert
            Assert.IsTrue(output.Contains(@"FirstName: string;"));
        }

        [TestMethod]
        public async Task When_allOf_contains_one_schema_then_csharp_inheritance_is_generated()
        {
            //// Arrange
            var generator = await CreateGeneratorAsync();

            //// Act
            var output = generator.GenerateFile("Teacher");

            //// Assert
            Assert.IsTrue(output.Contains(@"interface Teacher extends Person"));
        }

        [TestMethod]
        public async Task When_enum_has_description_then_typescript_has_comment()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Teacher>();
            schema.AllOf.First().Properties["Gender"].Description = "EnumDesc.";
            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(output.Contains(@"/** EnumDesc. *"));
        }

        [TestMethod]
        public async Task When_class_has_description_then_typescript_has_comment()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Teacher>();
            schema.Description = "ClassDesc.";
            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(output.Contains(@"/** ClassDesc. *"));
        }

        [TestMethod]
        public async Task When_property_has_description_then_csharp_has_xml_comment()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Teacher>();
            schema.Properties["Class"].Description = "PropertyDesc.";
            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(output.Contains(@"/** PropertyDesc. *"));
        }

        [TestMethod]
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
            Assert.IsTrue(output.Contains(@"readonly Birthday"));
        }

        [TestMethod]
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
            Assert.IsTrue(output.Contains(@"""foo-bar"": string;"));
        }

        [TestMethod]
        public void When_type_name_is_missing_then_anonymous_name_is_generated()
        {
            //// Arrange
            var schema = new JsonSchema4();

            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsFalse(output.Contains(@"interface  {"));
        }

        private static async Task<TypeScriptGenerator> CreateGeneratorAsync()
        {
            var schema = await JsonSchema4.FromTypeAsync<Teacher>();
            var schemaData = await schema.ToJsonAsync();
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface });
            return generator;
        }

        [TestMethod]
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
            Assert.IsTrue(code.Contains("dict: { [key: string] : string; } = {};")); // property not nullable, must be initialized with {}
        }
    }
}
