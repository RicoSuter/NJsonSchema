//-----------------------------------------------------------------------
// <copyright file="TimeSpanFormatValidator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using System;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>Validator for "TimeSpan" format.</summary>
    public class TimeSpanFormatValidator : IFormatValidator
    {
        /// <summary>Gets the format attribute's value.</summary>
        public string Format { get; } = JsonFormatStrings.TimeSpan;

        /// <summary>Gets the kind of error produced by validator.</summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.TimeSpanExpected;

        /// <summary>Validates format of given value.</summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        public bool IsValid(string value, JTokenType tokenType)
        {
            return tokenType == JTokenType.TimeSpan
                || TimeSpan.TryParse(value, out TimeSpan timeSpanResult);
        }
    }
}
