//-----------------------------------------------------------------------
// <copyright file="DefaultEnumNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The default enumeration name generator.</summary>
    public class DefaultEnumNameGenerator : IEnumNameGenerator
    {
        /// <summary>Generates the enumeration name/key of the given enumeration entry.</summary>
        /// <param name="index">The index of the enumeration value (check <see cref="JsonSchema.Enumeration" /> and <see cref="JsonSchema.EnumerationNames" />).</param>
        /// <param name="name">The name/key.</param>
        /// <param name="value">The value.</param>
        /// <param name="schema">The schema.</param>
        /// <returns>The enumeration name.</returns>
        public string Generate(int index, string name, object value, JsonSchema schema)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Empty";
            }

            if (name.StartsWith("_-"))
            {
                name = "__" + name.Substring(2);
            }

            return ConversionUtilities.ConvertToUpperCamelCase(name
                .Replace(":", "-").Replace(@"""", @""), true)
                .Replace(".", "_")
                .Replace(",", "_")
                .Replace("#", "_")
                .Replace("&", "_")
                .Replace("-", "_")
                .Replace("'", "_")
                .Replace("(", "_")
                .Replace(")", "_")
                .Replace("+", "_")
                .Replace("\\", "_");
        }
    }
}
