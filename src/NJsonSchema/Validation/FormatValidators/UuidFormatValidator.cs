//-----------------------------------------------------------------------
// <copyright file="UuidFormatValidator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json.Linq;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>Validator for "Uuid" format.</summary>
    public class UuidFormatValidator : IFormatValidator
    {
        /// <summary>Gets the format attribute's value.</summary>
        public string Format { get; } = JsonFormatStrings.Uuid;

        /// <summary>Gets the kind of error produced by validator.</summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.UuidExpected;

        /// <summary>Validates format of given value.</summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        public bool IsValid(string value, JTokenType tokenType)
        {
            return Guid.TryParse(value, out Guid guid);
        }
    }
}
