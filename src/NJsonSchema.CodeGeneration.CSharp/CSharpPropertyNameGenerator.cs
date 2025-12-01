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
        private const string FirstPassChars = "\"'@?!$[]().=+|";
#if NET8_0_OR_GREATER
        private static readonly System.Buffers.SearchValues<char> _reservedFirstPassChars = System.Buffers.SearchValues.Create(FirstPassChars);
#else
        private static readonly char[] _reservedFirstPassChars = FirstPassChars.ToCharArray();
#endif

        private const string SecondPassChars = "*:-#&";
#if NET8_0_OR_GREATER
        private static readonly System.Buffers.SearchValues<char> _reservedSecondPassChars = System.Buffers.SearchValues.Create(SecondPassChars);
#else
        private static readonly char[] _reservedSecondPassChars = SecondPassChars.ToCharArray();
#endif

        /// <summary>Generates the property name.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The new name.</returns>
        public string Generate(JsonSchemaProperty property)
        {
            var name = property.Name;

            if (name.AsSpan().IndexOfAny(_reservedFirstPassChars) != -1)
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

            if (name.AsSpan().IndexOfAny(_reservedSecondPassChars) != -1)
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
