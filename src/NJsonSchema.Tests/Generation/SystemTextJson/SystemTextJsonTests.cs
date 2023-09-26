using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation.SystemTextJson
{
    public class SystemTextJsonTests
    {
        public class HealthCheckResult
        {
            [Required]
            public string Name { get; }

            public string Description { get; }
        }

        [Fact]
        public async Task When_property_is_readonly_then_its_in_the_schema()
        {
            //// Act
            var schema = JsonSchema.FromType<HealthCheckResult>();
            var data = schema.ToJson();

            //// Assert
            Assert.NotNull(data);
            Assert.Contains(@"Name", data);
            Assert.Contains(@"Description", data);
        }
    }
}
