//-----------------------------------------------------------------------
// <copyright file="IFormatValidator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json.Linq;

namespace NJsonSchema.Validation.FormatValidators
{
    /// <summary>Provides a method to verify if value is of valid format.</summary>
    public interface IFormatValidator
    {
        /// <summary>Validates format of given value.</summary>
        /// <param name="value">String value.</param>
        /// <param name="tokenType">Type of token holding the value.</param>
        /// <returns>True if value is correct for given format, False - if not.</returns>
        bool IsValid(string value, JTokenType tokenType);

        /// <summary>Gets the kind of error produced by validator.</summary>
        ValidationErrorKind ValidationErrorKind { get; }

        /// <summary>Gets the format attribute's value.</summary>
        string Format { get; }
    }
}
