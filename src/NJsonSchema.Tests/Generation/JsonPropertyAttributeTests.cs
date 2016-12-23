using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class JsonPropertyAttributeTests
    {
        [TestMethod]
        public async Task When_name_of_JsonPropertyAttribute_is_set_then_it_is_used_as_json_property_name()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<JsonPropertyAttributeTests.MyJsonPropertyTestClass>();

            //// Act
            var property = schema.Properties["NewName"];

            //// Assert
            Assert.AreEqual("NewName", property.Name);
        }

        [TestMethod]
        public async Task When_required_is_always_in_JsonPropertyAttribute_then_the_property_is_required()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<JsonPropertyAttributeTests.MyJsonPropertyTestClass>();

            //// Act
            var property = schema.Properties["Required"];

            //// Assert
            Assert.IsTrue(property.IsRequired);
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