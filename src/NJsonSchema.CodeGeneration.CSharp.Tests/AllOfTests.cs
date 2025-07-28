using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp.Tests;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class AllOfTests
    {
        [Fact]
        public async Task When_allOf_has_two_schemas_then_referenced_schema_is_inherited()
        {
            // Arrange
            var json =
@"{
    ""allOf"": [
        {
            ""$ref"": ""#/definitions/B""
        },
        {
            ""type"": ""object"", 
            ""required"": [
                ""prop""
            ],
            ""properties"": {
                ""prop"": {
                    ""type"": ""string"",
                    ""minLength"": 1,
                    ""maxLength"": 30
                },
                ""prop2"": {
                    ""type"": ""string""
                }
            }
        }
    ],
    ""definitions"": {
        ""B"": {
            ""properties"": {
                ""foo"": {
                    ""type"": ""string""
                }
            }
        }
    }
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("A");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_allOf_has_one_schema_then_it_is_inherited()
        {
            // Arrange
            var json =
@"{
    ""type"": ""object"",
    ""discriminator"": ""type"",
    ""required"": [
        ""prop"", 
        ""type""
    ],
    ""properties"": {
        ""prop"": {
            ""type"": ""string"",
            ""minLength"": 1,
            ""maxLength"": 30
        },
        ""prop2"": {
            ""type"": ""string""
        },
        ""type"": {
            ""type"": ""string""
        }
    },
    ""allOf"": [
        {
            ""$ref"": ""#/definitions/B""
        }
    ],
    ""definitions"": {
        ""B"": {
            ""type"": ""object"",
            ""properties"": {
                ""foo"": {
                    ""type"": ""string""
                }
            }
        }
    }
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("A");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_all_of_has_multiple_refs_then_the_properties_should_expand_to_single_class()
        {
            // Arrange
            var json = @"{
                '$schema': 'http://json-schema.org/draft-04/schema#',
                'id': 'http://some.domain.com/foo.json',
                'type': 'object',
                'additionalProperties': false,
                'definitions': {
                    'tRef1': {
                        'properties': {
                            'val1': {
                                'type': 'string',
                            }
                        }
                    },
                    'tRef2': {
                        'properties': {
                            'val2': {
                                'type': 'string',
                            }
                        }
                    },
                    'tRef3': {
                        'properties': {
                            'val3': {
                                'type': 'string',
                            }
                        }
                    }
                },
                'properties' : {
                    'tAgg': {
                        'allOf': [
                            {'$ref': '#/definitions/tRef1'},
                            {'$ref': '#/definitions/tRef2'},
                            {'$ref': '#/definitions/tRef3'}
                        ]
                    }
                }
            }";

            // Act
            var schema = await JsonSchema.FromJsonAsync(json);
            var settings = new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco, Namespace = "ns" };
            var generator = new CSharpGenerator(schema, settings);
            var output = generator.GenerateFile("Foo");

            // Assert
            await VerifyHelper.Verify(output);
            CSharpCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_more_properties_are_defined_in_allOf_and_type_none_then_all_of_contains_all_properties_in_generated_code()
        {
            // Arrange
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

            // Act
            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("Foo").Replace("\r\n", "\n");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_allOf_schema_is_object_type_then_it_is_an_inherited_class_in_generated_code()
        {
            // Arrange
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

            // Act
            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("Foo");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_allOf_schema_contains_two_anonymous_nodes_without_type_specifier_an_anonymous_class_is_generated()
        {
            // Arrange
            // The issue here is that the 'type' specifier has been (legally) omitted.
            var json = @"
                {
                '$schema': 'http://json-schema.org/draft-04/schema#',
                'type': 'object',
                'allOf': [
                    {
                        'properties': {
                            'prop1' : { 'type' : 'string' }
                        }
                    },
                    {
                        'properties': {
                            'prop2' : { 'type' : 'number' }
                        }
                    }                    
                ]
            }";

            // Act
            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("Foo");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }
    }
}