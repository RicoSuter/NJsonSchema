using Newtonsoft.Json.Linq;
using System;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>
    /// Validator for "IpV6" format
    /// </summary>
    public class IpV6FormatValidator : IFormatValidator
    {
        /// <summary>
        /// Format attribute's value
        /// </summary>
        public string Format { get; } = JsonFormatStrings.IpV6;

        /// <summary>
        /// Kind of error produced by validator.
        /// </summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.IpV6Expected;

        /// <summary>
        /// Validates format of given value.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        public bool IsValid(string value, JTokenType tokenType)
        {
            return Uri.CheckHostName(value) == UriHostNameType.IPv6;
        }
    }
}
