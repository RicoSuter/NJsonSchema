//-----------------------------------------------------------------------
// <copyright file="TypeScriptPropertyNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Generates the property name for a given TypeScript <see cref="JsonProperty"/>.</summary>
    public class TypeScriptPropertyNameGenerator : IPropertyNameGenerator
    {
        /// <summary>Gets or sets the reserved names.</summary>
        public IEnumerable<string> ReservedPropertyNames { get; set; } = new List<string> { "constructor" };

        /// <summary>Generates the property name.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The new name.</returns>
        public virtual string Generate(JsonProperty property)
        {
            var name = ConversionUtilities.ConvertToLowerCamelCase(property.Name
                    .Replace("\"", string.Empty)
                    .Replace("@", string.Empty)
                    .Replace(".", "-")
                    .Replace("=", "-")
                    .Replace("+", "plus"), true)
                .Replace("*", "Star")
                .Replace(":", "_")
                .Replace("-", "_");

            if (ReservedPropertyNames.Contains(name))
            {
                return name + "_";
            }

            return name;
        }
    }
}