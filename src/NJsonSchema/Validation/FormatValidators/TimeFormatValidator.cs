//-----------------------------------------------------------------------
// <copyright file="TimeFormatValidator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using NJsonSchema.Annotations;
using System;
using System.Globalization;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>Validator for "Time" format.</summary>
    public class TimeFormatValidator : IFormatValidator
    {
        /// <summary>Gets the format attribute's value.</summary>
        public string Format { get; } = JsonFormatStrings.Time;

        /// <summary>Gets the kind of error produced by validator.</summary>
        public ValidationErrorKind ValidationErrorKind { get; } = ValidationErrorKind.TimeExpected;

        /// <summary>Validates format of given value.</summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        public bool IsValid(string value, JTokenType tokenType)
        {
            return tokenType == JTokenType.Date
                || DateTime.TryParseExact(value, "HH:mm:ss.FFFFFFFK", null, DateTimeStyles.None, out DateTime dateTimeResult);
        }
    }
}
