using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>
    /// Validator for "Email" format
    /// </summary>
    public class EmailFormatValidator : IFormatValidator
    {
        private const string EmailRegexExpression = @"^\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z$";

        /// <summary>
        /// Format attribute's value
        /// </summary>
        public string Format { get; } = JsonFormatStrings.Email;

        /// <summary>
        /// Kind of error produced by validator.
        /// </summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.EmailExpected;

        /// <summary>
        /// Validates format of given value.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        public bool IsValid(string value, JTokenType tokenType)
        {
            return Regex.IsMatch(value, EmailRegexExpression, RegexOptions.IgnoreCase);
        }
    }
}
