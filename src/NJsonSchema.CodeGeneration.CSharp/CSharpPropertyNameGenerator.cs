//-----------------------------------------------------------------------
// <copyright file="CSharpPropertyNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Generates the property name for a given CSharp <see cref="JsonSchemaProperty"/>.</summary>
    public sealed class CSharpPropertyNameGenerator : IPropertyNameGenerator
    {
        private static readonly char[] _reservedFirstPassChars = ['"', '\'', '@', '?', '!', '$', '[', ']', '(', ')', '.', '=', '+', '|'
        ];
        private static readonly char[] _reservedSecondPassChars = ['*', ':', '-', '#', '&'];

        /// <summary>Generates the property name.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The new name.</returns>
        public string Generate(JsonSchemaProperty property)
        {
            var name = property.Name;

            if (name.IndexOfAny(_reservedFirstPassChars) != -1)
            {
                name = name.Replace("\"", string.Empty)
                    .Replace("'", string.Empty)
                    .Replace("@", string.Empty)
                    .Replace("?", string.Empty)
                    .Replace("!", string.Empty)
                    .Replace("$", string.Empty)
                    .Replace("[", string.Empty)
                    .Replace("]", string.Empty)
                    .Replace("(", "_")
                    .Replace(")", string.Empty)
                    .Replace(".", "-")
                    .Replace("=", "-")
                    .Replace("+", "plus")
                    .Replace("|", "_");
            }

            name = ConversionUtilities.ConvertToUpperCamelCase(name, true);

            if (name.IndexOfAny(_reservedSecondPassChars) != -1)
            {
                name = name
                    .Replace("*", "Star")
                    .Replace(":", "_")
                    .Replace("-", "_")
                    .Replace("#", "_")
                    .Replace("&", "And");
            }

            return name;
        }
    }
}
