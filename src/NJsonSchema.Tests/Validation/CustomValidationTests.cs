using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;
using NJsonSchema.Validation.FormatValidators;
using System;
using System.Globalization;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class CustomValidationTests
    {
        [Fact]
        public void When_format_date_time_correct_with_custom_validator_passed_then_no_errors()
        {
            //// Arrange
            var schema = new JsonSchema
            {
                Type = JsonObjectType.String,
                Format = JsonFormatStrings.DateTime
            };

            var token = new JValue("2014-12-01 11:00:01:55");

            //// Act
            var settings = new JsonSchemaValidatorSettings();
            settings.AddCustomFormatValidator(new CustomDateTimeFormatValidator());

            var errors = schema.Validate(token, settings);

            //// Assert
            Assert.Empty(errors);
        }

        private class CustomDateTimeFormatValidator : IFormatValidator
        {
            private readonly string[] _acceptableFormats =
            [
                "yyyy'-'MM'-'dd HH':'mm':'ss':'ff"
            ];

            /// <summary>
            /// Gets the format attributes value.
            /// </summary>
            public string Format { get; } = JsonFormatStrings.DateTime;

            /// <summary>
            /// Gets the validation error kind.
            /// </summary>
            public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.DateTimeExpected;

            /// <summary>Validates if a string is valid DateTime.</summary>
            /// <param name="value">String value.</param>
            /// <param name="tokenType">Type of token holding the value.</param>
            /// <returns></returns>
            public bool IsValid(string value, JTokenType tokenType)
            {
                return tokenType == JTokenType.Date
                       || DateTimeOffset.TryParseExact(value, _acceptableFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
            }
        }
    }
}
