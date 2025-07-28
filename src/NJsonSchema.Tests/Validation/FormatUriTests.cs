using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    public class FormatUriTests
    {
        [Fact]
        public void When_format_uri_incorrect_then_validation_succeeds()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Uri;

            var token = new JValue("test");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Equal(ValidationErrorKind.UriExpected, errors.First().Kind);
        }

        [Fact]
        public void When_format_uri_correct_then_validation_succeeds()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Uri;

            var token = new JValue("http://rsuter.com");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void When_uri_token_is_of_type_uri_then_validation_succeeds()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Uri;

            var token = new JValue(new Uri("http://rsuter.com"));

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Empty(errors);
        }
    }
}