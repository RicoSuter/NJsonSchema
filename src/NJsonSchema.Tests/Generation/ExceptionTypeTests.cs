using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class ExceptionTypeTests
    {
        public class MyException : Exception
        {
            [JsonProperty("foo")]
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_exception_schema_is_generated_then_special_properties_are_generated_and_JsonProperty_attribute_used()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<MyException>();
            var json = schema.ToJson();

            //// Act
            var exceptionSchema = schema.InheritedSchema.ActualSchema;

            //// Assert
            Assert.True(schema.Properties.ContainsKey("foo"));
            Assert.True(exceptionSchema.Properties.ContainsKey("InnerException"));
        }
    }
}
