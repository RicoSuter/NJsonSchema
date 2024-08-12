//-----------------------------------------------------------------------
// <copyright file="GuidFormatValidator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using NJsonSchema.Annotations;
using System;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>Validator for "Guid" format.</summary>
    public class GuidFormatValidator : IFormatValidator
    {
        /// <summary>Gets the format attribute's value.</summary>
        public string Format { get; } = JsonFormatStrings.Guid;

        /// <summary>Gets the kind of error produced by validator.</summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.GuidExpected;

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
