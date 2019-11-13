using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class JsonPropertyAttributeTests
    {
        [Fact]
        public async Task When_name_of_JsonPropertyAttribute_is_set_then_it_is_used_as_json_property_name()
        {
            //// Arrange
            var schema = JsonSchema.FromType<JsonPropertyAttributeTests.MyJsonPropertyTestClass>();

            //// Act
            var property = schema.Properties["NewName"];

            //// Assert
            Assert.Equal("NewName", property.Name);
        }

        [Fact]
        public async Task When_name_of_JsonPropertyAttribute_is_set_then_it_is_used_as_json_property_name_even_with_contactresolver_that_has_nameing_strategy()
        {
            var settings = new NJsonSchema.Generation.JsonSchemaGeneratorSettings();
            settings.SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                {
                    NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()
                }
            };

            //// Arrange
            var schema = JsonSchema.FromType<JsonPropertyAttributeTests.MyJsonPropertyTestClass>(settings);

            //// Act
            var property = schema.Properties["NewName"];

            //// Assert
            Assert.Equal("NewName", property.Name);
        }

        [Fact]
        public async Task When_required_is_always_in_JsonPropertyAttribute_then_the_property_is_required()
        {
            //// Arrange
            var schema = JsonSchema.FromType<JsonPropertyAttributeTests.MyJsonPropertyTestClass>();

            //// Act
            var property = schema.Properties["Required"];

            //// Assert
            Assert.True(property.IsRequired);
        }

        public class MyJsonPropertyTestClass
        {
            [JsonProperty("NewName")]
            public string Name { get; set; }

            [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
            public string Required { get; set; }
        }
    }
}