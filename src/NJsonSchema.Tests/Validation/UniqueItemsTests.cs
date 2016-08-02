using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class UniqueItemsTests
    {
        [TestMethod]
        public void When_unique_items_is_set_and_items_are_objects_then_validation_works()
        {
            //// Arrange
            var jsonSchema = @"{
    ""$schema"": ""http://json-schema.org/draft-04/schema#"",
    ""title"": ""Config"",
    ""type"": ""array"",
    ""uniqueItems"": true,
    ""items"": {
        ""title"": ""KeyValue Pair"",
        ""type"": ""object"",
        ""properties"": {
            ""key"": {
                ""title"": ""Key"",
                ""type"": ""string"",
                ""minLength"": 1
            },
            ""value"": {
                ""title"": ""Value"",
                ""type"": ""string"",
                ""minLength"": 1
            }
        }
    }
}";

            var jsonData = @"[{
    ""key"": ""a"",
    ""value"": ""b""
},
{
    ""key"": ""a"",
    ""value"": ""b""
}]";
            
            //// Act
            var schema = JsonSchema4.FromJson(jsonSchema);
            var errors = schema.Validate(jsonData).ToList();

            //// Assert
            Assert.AreEqual(1, errors.Count);
        }
    }
}
