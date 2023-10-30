//-----------------------------------------------------------------------
// <copyright file="ByteFormatValidator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using NJsonSchema.Annotations;
using System.Text.RegularExpressions;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>Validator for "Byte" format.</summary>
    public class ByteFormatValidator : IFormatValidator
    {
        private const string Base64Expression = @"^[a-zA-Z0-9\+/]*={0,3}$";

        /// <summary>Gets the format attribute's value.</summary>
        public string Format { get; } = JsonFormatStrings.Byte;

        /// <summary>Gets the kind of error produced by validator.</summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.Base64Expected;

        /// <summary>Validates format of given value.</summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        public bool IsValid(string value, JTokenType tokenType)
        {
            return (value.Length % 4 == 0) 
                && Regex.IsMatch(value, Base64Expression, RegexOptions.None);
        }
    }
}
