using System.Collections.Generic;
using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class AlwaysAllowAdditionalObjectPropertiesTests
    {
        [Fact]
        public async Task When_additionalProperties_schema_is_set_for_object_then_special_property_is_rendered()
        {
            //// Arrange
            var json =
@"{ 
    ""properties"": {
        ""Pet"": {
            ""type"": ""object"",
            ""properties"": {
                ""id"": {
                    ""type"": ""integer"",
                    ""format"": ""int64""
                },
                ""category"": {
                    ""type"": ""string""
                }
            },
            ""additionalProperties"": {
                ""type"": ""string""
            }
        }
    }
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("Person");

            //// Assert
            Assert.Contains("[Newtonsoft.Json.JsonExtensionData]", code);
            Assert.Contains("public System.Collections.Generic.IDictionary<string, object> AdditionalProperties", code);
        }


        public class Page
        {
            int Number{get;set;}
            string Content {get;set;}
        }

        public class Book
        {
            public Dictionary<string, string> Index {get; set;}
            public Page Page {get;set;}
        }

        [Fact]
        public void AllowAdditionalProperties_DictionaryAndNoObjectAreNotSame()
        {
            var schema = JsonSchema.FromType<Book>(new JsonSchemaGeneratorSettings
            {
                AlwaysAllowAdditionalObjectProperties = true
            });
            var settings = new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco, Namespace = "ns", };
            var generator = new CSharpGenerator(schema, settings);

            var code = generator.GenerateFile("Library");

            Assert.Contains("public object Page", code);
            Assert.Contains("public System.Collections.Generic.IDictionary<string, string> Index", code);
        }
    }
}