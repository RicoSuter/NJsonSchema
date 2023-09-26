using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.NewtonsoftJson.Tests.Generation
{
    public class RecordTests
    {
        public record Address
        {
            public string Street { get; set; }
        }

        [Fact]
        public void Should_have_only_one_property()
        {
            //// Arrange

            //// Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Address>();
            var data = schema.ToJson();

            //// Assert
            Assert.Equal(1, schema.Properties.Count);
        }
    }
}