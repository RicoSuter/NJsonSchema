using System.Text.Json.Nodes;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    public class FormatTimeSpanTests
    {
        [Fact]
        public void When_format_time_span_incorrect_then_validation_fails()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.TimeSpan;

            var token = JsonValue.Create("test");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Equal(ValidationErrorKind.TimeSpanExpected, errors.First().Kind);
        }

        [Fact]
        public void When_format_time_span_correct_then_validation_succeeds()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.TimeSpan;

            var token = JsonValue.Create("1:30:45");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Empty(errors);
        }
    }
}