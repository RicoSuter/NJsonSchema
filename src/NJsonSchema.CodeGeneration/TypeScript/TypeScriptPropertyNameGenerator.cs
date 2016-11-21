//-----------------------------------------------------------------------
// <copyright file="TypeScriptPropertyNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary></summary>
    public class TypeScriptPropertyNameGenerator : IPropertyNameGenerator
    {
        /// <summary>Generates the property name.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The new name.</returns>
        public virtual string Generate(JsonProperty property)
        {
            return ConversionUtilities.ConvertToLowerCamelCase(property.Name, true)
                .Replace("-", "_");
        }
    }
}