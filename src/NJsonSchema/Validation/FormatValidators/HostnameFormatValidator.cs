using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>
    /// Validator for "Hostname" format
    /// </summary>
    public class HostnameFormatValidator : IFormatValidator
    {
        private const string HostnameExpression = "^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\\-]*[a-zA-Z0-9])\\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\\-]*[A-Za-z0-9])$";

        /// <summary>
        /// Format attribute's value
        /// </summary>
        public string Format { get; } = JsonFormatStrings.Hostname;

        /// <summary>
        /// Kind of error produced by validator.
        /// </summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.HostnameExpected;

        /// <summary>
        /// Validates format of given value.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        public bool IsValid(string value, JTokenType tokenType)
        {
            return Regex.IsMatch(value, HostnameExpression, RegexOptions.IgnoreCase);
        }
    }

}
