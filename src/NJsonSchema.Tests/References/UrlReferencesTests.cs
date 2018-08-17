using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.References
{
    public class UrlReferencesTests
    {
        [Fact]
        public async Task Test()
        {
            // Arrange

            // see https://github.com/RSuter/NJsonSchema/issues/566

            // Act
            var schema = await JsonSchema4.FromUrlAsync(@"https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json");

            // Assert
        }
    }
}
