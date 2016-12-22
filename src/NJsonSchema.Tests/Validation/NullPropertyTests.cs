using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class NullPropertyTests
    {
        [TestMethod]
        public void When_property_can_be_null_then_null_is_allowed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Properties["test"] = new JsonProperty
            {
                Type = JsonObjectType.Null | JsonObjectType.Object
            };
            schema.Properties["test"].Properties["foo"] = new JsonProperty
            {
                Type = JsonObjectType.String
            };

            //// Act
            var data = "{ 'test': null }";
            var errors = schema.Validate(data);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }

        public class NullablePropertyClass
        {
            public QueryRule ReportRules { get; set; }
        }

        public class QueryRule
        {
            public string Name { get; set; }
        }

        [TestMethod]
        public async Task When_property_can_be_null_then_null_is_allowed2()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<NullablePropertyClass>();
            var schemaData = await schema.ToJsonAsync();

            //// Act
            var data = "{ 'ReportRules': null }";
            var errors = schema.Validate(data);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }
    }
}