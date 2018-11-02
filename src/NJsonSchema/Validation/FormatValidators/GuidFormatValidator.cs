using Newtonsoft.Json.Linq;
using System;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>
    /// Validator for "Guid" format
    /// </summary>
    public class GuidFormatValidator : IFormatValidator
    {
        /// <summary>
        /// Format attribute's value
        /// </summary>
        public string Format { get; } = JsonFormatStrings.Guid;

        /// <summary>
        /// Kind of error produced by validator.
        /// </summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.GuidExpected;

        /// <summary>
        /// Validates format of given value.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        public bool IsValid(string value, JTokenType tokenType)
        {
            return Guid.TryParse(value, out Guid guid);
        }
    }
}
