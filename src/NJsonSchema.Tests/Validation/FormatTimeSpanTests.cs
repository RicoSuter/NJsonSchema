using System.Linq;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class FormatTimeSpanTests
    {
        [Fact]
        public void When_format_time_span_incorrect_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.TimeSpan;

            var token = new JValue("test");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.TimeSpanExpected, errors.First().Kind);
        }

        [Fact]
        public void When_format_time_span_correct_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.TimeSpan;

            var token = new JValue("1:30:45");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }
    }
}