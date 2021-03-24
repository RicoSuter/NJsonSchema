using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class AnyOfTests
    {
        [Fact]
        public async Task When_anyOf_has_two_schemas_then_the_properties_should_expand_to_single_class()
        {
            //// Arrange
            var json =
@"{
    ""anyOf"": [
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

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("A");

            //// Assert
            Assert.Contains(@"public partial class A 
    {
        [Newtonsoft.Json.JsonProperty(""foo"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Foo { get; set; }
    
        [Newtonsoft.Json.JsonProperty(""prop"", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(30, MinimumLength = 1)]
        public string Prop { get; set; }
    
        [Newtonsoft.Json.JsonProperty(""prop2"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Prop2 { get; set; }
    
        private System.Collections.Generic.IDictionary<string, object> _additionalProperties = new System.Collections.Generic.Dictionary<string, object>();
    
        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }".Replace("\r", string.Empty), code);
            Assert.DoesNotContain("Anonymous", code);
        }

        // TODO: This one fails, and I suspect this indicates there is a bug somewhere?
//         [Fact]
//         public async Task When_anyOf_has_one_schema_then_the_properties_should_expand_into_class()
//         {
//             //// Arrange
//             var json =
// @"{
//     ""type"": ""object"",
//     ""discriminator"": ""type"",
//     ""required"": [
//         ""prop"",
//         ""type""
//     ],
//     ""properties"": {
//         ""prop"": {
//             ""type"": ""string"",
//             ""minLength"": 1,
//             ""maxLength"": 30
//         },
//         ""prop2"": {
//             ""type"": ""string""
//         },
//         ""type"": {
//             ""type"": ""string""
//         }
//     },
//     ""anyOf"": [
//         {
//             ""$ref"": ""#/definitions/B""
//         }
//     ],
//     ""definitions"": {
//         ""B"": {
//             ""type"": ""object"",
//             ""properties"": {
//                 ""foo"": {
//                     ""type"": ""string""
//                 }
//             }
//         }
//     }
// }";
//             var schema = await JsonSchema.FromJsonAsync(json);
//
//             //// Act
//             var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
//             var code = generator.GenerateFile("A");
//
//             //// Assert
//             Assert.Contains(@"public partial class A
//     {
//         [Newtonsoft.Json.JsonProperty(""prop"", Required = Newtonsoft.Json.Required.Always)]
//         [System.ComponentModel.DataAnnotations.Required]
//         [System.ComponentModel.DataAnnotations.StringLength(30, MinimumLength = 1)]
//         public string Prop { get; set; }
//
//         [Newtonsoft.Json.JsonProperty(""prop2"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
//         public string Prop2 { get; set; }
//
//         [Newtonsoft.Json.JsonProperty(""type"", Required = Newtonsoft.Json.Required.Always)]
//         [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
//         public string Type { get; set; }
//
//         [Newtonsoft.Json.JsonProperty(""foo"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
//         public string Foo { get; set; }
//
//         private System.Collections.Generic.IDictionary<string, object> _additionalProperties = new System.Collections.Generic.Dictionary<string, object>();
//
//         [Newtonsoft.Json.JsonExtensionData]
//         public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
//         {
//             get { return _additionalProperties; }
//             set { _additionalProperties = value; }
//         }".Replace("\r", string.Empty), code);
//             Assert.DoesNotContain("Anonymous", code);
//         }

        [Fact]
        public async Task When_anyOf_has_multiple_refs_then_the_properties_should_expand_to_single_class()
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
                        'anyOf': [
                            {'$ref': '#/definitions/tRef1'},
                            {'$ref': '#/definitions/tRef2'},
                            {'$ref': '#/definitions/tRef3'}
                        ]
                    }
                }
            }";

            //// Act
            var schema = await JsonSchema.FromJsonAsync(json);
            var settings = new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco, Namespace = "ns" };
            var generator = new CSharpGenerator(schema, settings);
            var code = generator.GenerateFile("Foo");

            //// Assert
            Assert.Contains(@"public partial class TRef1 
    {
        [Newtonsoft.Json.JsonProperty(""val1"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Val1 { get; set; }
    
        private System.Collections.Generic.IDictionary<string, object> _additionalProperties = new System.Collections.Generic.Dictionary<string, object>();
    
        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }".Replace("\r", string.Empty), code);
            Assert.Contains(@"public partial class TRef1 
    {
        [Newtonsoft.Json.JsonProperty(""val1"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Val1 { get; set; }
    
        private System.Collections.Generic.IDictionary<string, object> _additionalProperties = new System.Collections.Generic.Dictionary<string, object>();
    
        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }".Replace("\r", string.Empty), code);

            Assert.Contains(@"public partial class TRef2 
    {
        [Newtonsoft.Json.JsonProperty(""val2"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Val2 { get; set; }
    
        private System.Collections.Generic.IDictionary<string, object> _additionalProperties = new System.Collections.Generic.Dictionary<string, object>();
    
        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }".Replace("\r", string.Empty), code);

            Assert.Contains(@"public partial class TRef3 
    {
        [Newtonsoft.Json.JsonProperty(""val3"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Val3 { get; set; }
    
        private System.Collections.Generic.IDictionary<string, object> _additionalProperties = new System.Collections.Generic.Dictionary<string, object>();
    
        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }".Replace("\r", string.Empty), code);

            Assert.Contains(@"public partial class Foo 
    {
        [Newtonsoft.Json.JsonProperty(""tAgg"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public TAgg TAgg { get; set; }".Replace("\r", string.Empty), code);

            Assert.Contains(@"public partial class TAgg 
    {
        [Newtonsoft.Json.JsonProperty(""val1"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Val1 { get; set; }
    
        [Newtonsoft.Json.JsonProperty(""val2"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Val2 { get; set; }
    
        [Newtonsoft.Json.JsonProperty(""val3"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Val3 { get; set; }
    
        private System.Collections.Generic.IDictionary<string, object> _additionalProperties = new System.Collections.Generic.Dictionary<string, object>();
    
        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }".Replace("\r", string.Empty), code);
        }

        [Fact]
        public async Task When_more_properties_are_defined_in_anyOf_and_type_none_then_the_properties_should_expand_to_single_class()
        {
            //// Arrange
            var json = @"{
                '$schema': 'http://json-schema.org/draft-04/schema#',
                'type': 'object',
                'properties': { 
                    'prop1' : { 'type' : 'string' } 
                },
                'anyOf': [
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
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("Foo").Replace("\r\n", "\n");

            //// Assert
            Assert.Contains(@"public partial class Foo 
    {
        [Newtonsoft.Json.JsonProperty(""prop1"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Prop1 { get; set; }
    
        [Newtonsoft.Json.JsonProperty(""baseProperty"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string BaseProperty { get; set; }
    
        [Newtonsoft.Json.JsonProperty(""prop2"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Prop2 { get; set; }
    
        private System.Collections.Generic.IDictionary<string, object> _additionalProperties = new System.Collections.Generic.Dictionary<string, object>();
    
        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }".Replace("\r", string.Empty), code);
            Assert.DoesNotContain("class Anonymous", code);
        }

        [Fact]
        public async Task When_anyOf_schema_is_object_type_then_the_properties_should_expand_to_single_class()
        {
            //// Arrange
            var json = @"{
                '$schema': 'http://json-schema.org/draft-04/schema#',
                'type': 'object',
                'properties': { 
                    'prop1' : { 'type' : 'string' } 
                },
                'anyOf': [
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
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("Foo");

            //// Assert
            Assert.Contains(@"public partial class Foo 
    {
        [Newtonsoft.Json.JsonProperty(""prop1"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Prop1 { get; set; }
    
        [Newtonsoft.Json.JsonProperty(""prop2"", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Prop2 { get; set; }
    
        private System.Collections.Generic.IDictionary<string, object> _additionalProperties = new System.Collections.Generic.Dictionary<string, object>();
    
        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }".Replace("\r", string.Empty), code);
        }
    }
}