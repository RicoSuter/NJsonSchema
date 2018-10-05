using Newtonsoft.Json.Linq;
using System;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>
    /// Validator for "Uri" format
    /// </summary>
    public class UriFormatValidator : IFormatValidator
    {
        /// <summary>
        /// Format attribute's value
        /// </summary>
        public string Format { get; } = JsonFormatStrings.Uri;

        /// <summary>
        /// Kind of error produced by validator.
        /// </summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.UriExpected;

        /// <summary>
        /// Validates format of given value.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        public bool IsValid(string value, JTokenType tokenType)
        {
            return tokenType == JTokenType.Uri
                || Uri.TryCreate(value, UriKind.Absolute, out Uri uriResult);
        }
    }
}
