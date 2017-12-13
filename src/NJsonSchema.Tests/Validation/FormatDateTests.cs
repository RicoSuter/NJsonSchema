using System.Linq;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class FormatDateTests
    {
        [Fact]
        public void When_format_date_incorrect_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Date;

            var token = new JValue("test");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.DateExpected, errors.First().Kind);
        }

        [Fact]
        public void When_format_date_time_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Date;

            var token = new JValue("2014-12-01 11:54");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.DateExpected, errors.First().Kind);
        }

        [Fact]
        public void When_format_date_correct_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Date;

            var token = new JValue("2014-12-01");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }
    }
}