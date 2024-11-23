using Xunit;

namespace NJsonSchema.Tests.Generation
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
            // Arrange

            // Act
            var schema = JsonSchema.FromType<Address>();
            var data = schema.ToJson();

            var add = new Address();

            // Assert
            Assert.Single(schema.Properties);
        }
    }
}