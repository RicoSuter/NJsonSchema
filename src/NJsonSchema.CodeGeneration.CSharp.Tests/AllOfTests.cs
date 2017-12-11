using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class AllOfTests
    {
        [Fact]
        public async Task When_allOf_has_two_schemas_then_referenced_schema_is_inherited()
        {
            //// Arrange
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
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("A");

            //// Assert
            Assert.False(code.Contains("Anonymous"));
            Assert.True(code.Contains("A : B"));
        }

        [Fact]
        public async Task When_allOf_has_one_schema_then_it_is_inherited()
        {
            //// Arrange
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
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("A");

            //// Assert
            Assert.False(code.Contains("Anonymous"));
            Assert.True(code.Contains("A : B"));
        }

        [Fact]
        public async Task When_all_of_has_multiple_refs_then_the_properties_should_expand_to_single_class()
        {
            //// Arrange
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

            //// Act
            var schema = await JsonSchema4.FromJsonAsync(json);
            var settings = new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco, Namespace = "ns" };
            var generator = new CSharpGenerator(schema, settings);
            var output = generator.GenerateFile("Foo");

            //// Assert
            Assert.True(output.Contains("public partial class TAgg"));
            Assert.True(output.Contains("public string Val1 { get; set; }"));
            Assert.True(output.Contains("public string Val2 { get; set; }"));
            Assert.True(output.Contains("public string Val3 { get; set; }"));
        }

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
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("Foo").Replace("\r\n", "\n");

            //// Assert
            Assert.True(code.Contains(
@"  public partial class Foo : Anonymous
    {
        [Newtonsoft.Json.JsonProperty(""prop1"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Prop1 { get; set; }
    
        [Newtonsoft.Json.JsonProperty(""prop2"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Prop2 { get; set; }
".Replace("\r", string.Empty)));

            Assert.True(code.Contains("class Anonymous"));
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
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("Foo");

            //// Assert
            Assert.True(code.Contains("class Foo : Bar"));
            Assert.True(code.Contains("public string Prop1 { get; set; }"));
            Assert.True(code.Contains("public string Prop2 { get; set; }"));
        }
    }
}