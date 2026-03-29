using System.Text.Json.Nodes;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    public class FormatHostnameTests
    {
        [Fact]
        public void When_format_hostname_incorrect_then_validation_succeeds()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Hostname;

            var token = JsonValue.Create("foo:bar");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Equal(ValidationErrorKind.HostnameExpected, errors.First().Kind);
        }

        [Fact]
        public void When_format_hostname_is_ip_then_validation_succeeds()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Hostname;

            var token = JsonValue.Create("rsuter.com");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Empty(errors);
        }
    }
}