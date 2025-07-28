using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    public class FormatEmailTests
    {
        [Fact]
        public void When_format_email_incorrect_then_validation_succeeds()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Email;

            var token = new JValue("test");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Equal(ValidationErrorKind.EmailExpected, errors.First().Kind);
        }

        [Fact]
        public void When_format_email_correct_then_validation_succeeds()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Email;

            var token = new JValue("mail@rsuter.com");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Empty(errors);
        }
    }
}