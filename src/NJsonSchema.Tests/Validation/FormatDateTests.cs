using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using NJsonSchema.Validation;
using NJsonSchema.Validation.FormatValidators;

namespace NJsonSchema.Tests.Validation
{
    public class FormatDateTests
    {
        [Fact]
        public void When_format_date_incorrect_then_validation_fails()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Date;

            var token = JsonValue.Create("test");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Equal(ValidationErrorKind.DateExpected, errors.First().Kind);
        }

        [Fact]
        public void When_format_date_time_then_validation_fails()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Date;

            var token = JsonValue.Create("2014-12-01 11:54");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Equal(ValidationErrorKind.DateExpected, errors.First().Kind);
        }

        [Fact]
        public void When_format_date_correct_then_validation_succeeds()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Date;

            var token = JsonValue.Create("2014-12-01");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void Date_format_validation_uses_the_Gregorian_calendar_in_all_cultures()
        {
            // Arrange
            var originalCulture = CultureInfo.CurrentCulture;
            var validator = new DateFormatValidator();

            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");

                // Act
                var validLeapDate = validator.IsValid("2024-02-29", JsonValueKind.String);
                var invalidLeapDate = validator.IsValid("2023-02-29", JsonValueKind.String);
                var yearOnly = validator.IsValid("2024", JsonValueKind.String);

                // Assert
                Assert.True(validLeapDate);
                Assert.False(invalidLeapDate);
                Assert.False(yearOnly);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
    }
}
