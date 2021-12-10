using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.References
{
    public class ExternalReferencesTests
    {
        [Fact]
        public async Task When_definitions_is_nested_then_refs_work()
        {
            //// Arrange
            var url = "http://namespace.lantmateriet.se/distribution/produkter/fastighet/v2.1/fastighet-2.1.0.json";

            //// Act
            var schema = await JsonSchema.FromUrlAsync(url);

            //// Assert
            Assert.NotNull(schema);
        }
    }
}
