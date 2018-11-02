using Newtonsoft.Json.Linq;
using System;
using System.Globalization;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>
    /// Validator for "Date" format
    /// </summary>
    public class DateFormatValidator : IFormatValidator
    {
        /// <summary>
        /// Validates format of given value.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        public bool IsValid(string value, JTokenType tokenType)
        {
            return tokenType == JTokenType.Date 
                || DateTime.TryParseExact(value, "yyyy-MM-dd", null, DateTimeStyles.None, out DateTime dateTimeResult) 
                    && dateTimeResult.Date == dateTimeResult;
        }

        /// <summary>
        /// Format attribute's value
        /// </summary>
        public string Format { get; } = JsonFormatStrings.Date;

        /// <summary>
        /// Returns validation error kind.
        /// </summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.DateExpected;
    }
}
