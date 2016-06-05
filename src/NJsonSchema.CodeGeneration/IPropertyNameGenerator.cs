//-----------------------------------------------------------------------
// <copyright file="IPropertyNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration
{
    /// <summary>Converts a JSON property name to its name in the generated code.</summary>
    public interface IPropertyNameGenerator
    {
        /// <summary>Converts a property name.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The new name.</returns>
        string Convert(JsonProperty property);
    }
}