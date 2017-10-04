//-----------------------------------------------------------------------
// <copyright file="CSharpPropertyNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Generates the property name for a given CSharp <see cref="JsonProperty"/>.</summary>
    public class CSharpPropertyNameGenerator : IPropertyNameGenerator
    {
        /// <summary>Generates the property name.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The new name.</returns>
        public virtual string Generate(JsonProperty property)
        {
            return ConversionUtilities.ConvertToUpperCamelCase(property.Name
                    .Replace("\"", string.Empty)
                    .Replace("@", string.Empty)
                    .Replace(".", "-")
                    .Replace("=", "-")
                    .Replace("+", "plus"), true)
                .Replace(":", "_")
                .Replace("-", "_");
        }
    }
}