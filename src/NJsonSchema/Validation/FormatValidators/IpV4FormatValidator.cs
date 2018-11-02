using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>
    /// Validator for "IpV4" format
    /// </summary>
    public class IpV4FormatValidator : IFormatValidator
    {
        private const string IpV4RegexExpression = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?).){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";

        /// <summary>
        /// Format attribute's value
        /// </summary>
        public string Format { get; } = JsonFormatStrings.IpV4;

        /// <summary>
        /// Kind of error produced by validator.
        /// </summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.IpV4Expected;

        /// <summary>
        /// Validates format of given value.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        public bool IsValid(string value, JTokenType tokenType)
        {
            return Regex.IsMatch(value, IpV4RegexExpression, RegexOptions.IgnoreCase);
        }
    }
}
