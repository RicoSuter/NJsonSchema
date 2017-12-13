using System.Linq;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    /// <summary>
    /// Time format tests according to rfc 3339 full-time
    /// https://tools.ietf.org/html/rfc3339#section-5.6
    /// </summary>
    public class FormatTimeTests
    {
        [Fact]
        public void When_format_time_incorrect_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Time;

            var token = new JValue("10 am");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.TimeExpected, errors.First().Kind);
        }

        [Fact]
        public void When_format_time_positive_offset_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Time;

            var token = new JValue("14:30:00+02:00");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void When_format_time_has_negative_offset_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Time;

            var token = new JValue("14:30:00-02:00");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void When_format_time_is_utc_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Time;

            var token = new JValue("14:30:00Z");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void When_format_time_secfrac_correct_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Time;

            var token = new JValue("14:30:00.1234567+02:00");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }

        /// <summary>
        /// This allows time without a specified timezone. It represents just the 
        /// partial-time component in the RFC
        /// </summary>
        [Fact]
        public void When_format_time_is_not_utc_or_offset_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Time;

            var token = new JValue("14:30:00");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }
    }
}