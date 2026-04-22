using System.Text.Json.Nodes;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    public class FormatIpV6Tests
    {
        [Fact]
        public void When_format_ipv6_incorrect_then_validation_fails()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.IpV6;

            var token = JsonValue.Create("test");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Equal(ValidationErrorKind.IpV6Expected, errors.First().Kind);
        }

        [Fact]
        public void When_format_ipv6_correct_then_validation_succeeds()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.IpV6;

            var token = JsonValue.Create("::1");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Empty(errors);
        }
    }
}