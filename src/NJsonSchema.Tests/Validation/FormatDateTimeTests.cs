using System.Linq;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class FormatDateTimeTests
    {
        [Fact]
        public void When_format_date_time_incorrect_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.DateTime;

            var token = new JValue("test");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.DateTimeExpected, errors.First().Kind);
        }

        [Fact]
        public void When_format_date_time_correct_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.DateTime;

            var token = new JValue("2014-12-01 11:00:01");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }
    }
}