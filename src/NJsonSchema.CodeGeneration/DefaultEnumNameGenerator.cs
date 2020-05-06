//-----------------------------------------------------------------------
// <copyright file="DefaultEnumNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The default enumeration name generator.</summary>
    public class DefaultEnumNameGenerator : IEnumNameGenerator
    {
        private readonly static Regex _invalidNameCharactersPattern = new Regex(@"[^\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]");
        private const string _defaultReplacementCharacter = "_";

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

            switch (name)
            {
                case ("="):
                    name = "eq";
                    break;
                case ("!="):
                    name = "ne";
                    break;
                case (">"):
                    name = "gt";
                    break;
                case ("<"):
                    name = "lt";
                    break;
                case (">="):
                    name = "ge";
                    break;
                case ("<="):
                    name = "le";
                    break;
                case ("~="):
                    name = "approx";
                    break;
            }

            if (name.StartsWith("-"))
            {
                name = "minus_" + name.Substring(1);
            }

            if (name.StartsWith("_-"))
            {
                name = "__" + name.Substring(2);
            }

            return _invalidNameCharactersPattern.Replace(ConversionUtilities.ConvertToUpperCamelCase(name
                .Replace(":", "-").Replace(@"""", @""), true), "_");
        }
    }
}
