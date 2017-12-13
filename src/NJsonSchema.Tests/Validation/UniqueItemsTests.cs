using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class UniqueItemsTests
    {
        [Fact]
        public async Task When_unique_items_is_set_and_items_are_objects_then_validation_works()
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
            var schema = await JsonSchema4.FromJsonAsync(jsonSchema);
            var errors = schema.Validate(jsonData).ToList();

            //// Assert
            Assert.Single(errors);
        }
    }
}
