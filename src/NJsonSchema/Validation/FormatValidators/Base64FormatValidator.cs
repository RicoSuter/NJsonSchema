//-----------------------------------------------------------------------
// <copyright file="Base64FormatValidator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json.Linq;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>Validator for "Base64" format.</summary>
    public class Base64FormatValidator : IFormatValidator
    {
        private readonly ByteFormatValidator _byteFormatValidator = new ByteFormatValidator();

        /// <summary>Gets the format attribute's value.</summary>
#pragma warning disable 618 //Base64 check is used for backward compatibility
        public string Format { get; } = JsonFormatStrings.Base64;
#pragma warning restore 618

        /// <summary>Gets the kind of error produced by validator.</summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.Base64Expected;

        /// <summary>Validates format of given value.</summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        public bool IsValid(string value, JTokenType tokenType)
        {
            return _byteFormatValidator.IsValid(value, tokenType);
        }
    }
}
