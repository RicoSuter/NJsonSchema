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

            private string PrivateReadOnlyProperty1 { get; }

            private string PrivateReadOnlyProperty2 => "TEST";

            public static string PublicReadOnlyStaticProperty { get; }

            private static string PrivateReadOnlyStaticProperty { get; }
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

        [Fact]
        public async Task When_property_is_private_and_readonly_then_its_not_in_the_schema()
        {
            //// Act
            var schema = JsonSchema.FromType<HealthCheckResult>();
            var data = schema.ToJson();

            //// Assert
            Assert.NotNull(data);
            Assert.False(data.Contains("PrivateReadOnlyProperty1"), data);
            Assert.False(data.Contains("PrivateReadOnlyProperty2"), data);
        }

        [Fact]
        public async Task When_property_is_static_readonly_then_its_not_in_the_schema()
        {
            //// Act
            var schema = JsonSchema.FromType<HealthCheckResult>();
            var data = schema.ToJson();

            //// Assert
            Assert.NotNull(data);
            Assert.False(data.Contains("PublicReadOnlyStaticProperty"), data);
            Assert.False(data.Contains("PrivateReadOnlyStaticProperty"), data);
        }
    }
}
