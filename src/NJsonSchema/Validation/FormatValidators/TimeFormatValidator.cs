using Newtonsoft.Json.Linq;
using System;
using System.Globalization;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>
    /// Validator for "Time" format
    /// </summary>
    public class TimeFormatValidator : IFormatValidator
    {
        /// <summary>
        /// Format attribute's value
        /// </summary>
        public string Format { get; } = JsonFormatStrings.Time;

        /// <summary>
        /// Kind of error produced by validator.
        /// </summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.TimeExpected;

        /// <summary>
        /// Validates format of given value.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        public bool IsValid(string value, JTokenType tokenType)
        {
            return tokenType == JTokenType.Date
                || DateTime.TryParseExact(value, "HH:mm:ss.FFFFFFFK", null, DateTimeStyles.None, out DateTime dateTimeResult);
        }
    }
}
