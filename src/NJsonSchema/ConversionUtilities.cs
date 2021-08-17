//-----------------------------------------------------------------------
// <copyright file="ConversionUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NJsonSchema
{
    /// <summary>Provides name conversion utility methods.</summary>
    public class ConversionUtilities
    {
        /// <summary>Converts the first letter to lower case and dashes to camel case.</summary>
        /// <param name="input">The input.</param>
        /// <param name="firstCharacterMustBeAlpha">Specifies whether to add an _ when the first character is not alpha.</param>
        /// <returns>The converted input.</returns>
        public static string ConvertToLowerCamelCase(string input, bool firstCharacterMustBeAlpha)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            input = ConvertDashesToCamelCase((input[0].ToString().ToLowerInvariant() + (input.Length > 1 ? input.Substring(1) : ""))
                .Replace(" ", "_")
                .Replace("/", "_"));

            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            if (firstCharacterMustBeAlpha && char.IsNumber(input[0]))
            {
                return "_" + input;
            }

            return input;
        }

        /// <summary>Converts the first letter to upper case and dashes to camel case.</summary>
        /// <param name="input">The input.</param>
        /// <param name="firstCharacterMustBeAlpha">Specifies whether to add an _ when the first character is not alpha.</param>
        /// <returns>The converted input.</returns>
        public static string ConvertToUpperCamelCase(string input, bool firstCharacterMustBeAlpha)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            input = ConvertDashesToCamelCase((input[0].ToString().ToUpperInvariant() + (input.Length > 1 ? input.Substring(1) : ""))
                .Replace(" ", "_")
                .Replace("/", "_"));

            if (firstCharacterMustBeAlpha && char.IsNumber(input[0]))
            {
                return "_" + input;
            }

            return input;
        }

        /// <summary>Converts the string to a string literal which can be used in C# or TypeScript code.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The literal.</returns>
        public static string ConvertToStringLiteral(string input)
        {
            var literal = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\'': literal.Append(@"\'"); break;
                    case '\"': literal.Append("\\\""); break;
                    case '\\': literal.Append(@"\\"); break;
                    case '\0': literal.Append(@"\0"); break;
                    case '\a': literal.Append(@"\a"); break;
                    case '\b': literal.Append(@"\b"); break;
                    case '\f': literal.Append(@"\f"); break;
                    case '\n': literal.Append(@"\n"); break;
                    case '\r': literal.Append(@"\r"); break;
                    case '\t': literal.Append(@"\t"); break;
                    case '\v': literal.Append(@"\v"); break;
                    default:
                        // ASCII printable character
                        if (c >= 0x20 && c <= 0x7e)
                        {
                            literal.Append(c);
                            // As UTF16 escaped character
                        }
                        else
                        {
                            literal.Append(@"\u");
                            literal.Append(((int)c).ToString("x4"));
                        }
                        break;
                }
            }
            return literal.ToString();
        }

        /// <summary>Converts the input to a camel case identifier.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The converted input. </returns>
        public static string ConvertToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return ConvertDashesToCamelCase(input.Replace(" ", "_").Replace("/", "_"));
        }


        /// <summary>Trims white spaces from the text.</summary>
        /// <param name="text">The text.</param>
        /// <returns>The updated text.</returns>
        public static string TrimWhiteSpaces(string text)
        {
            return text?.Trim('\n', '\r', '\t', ' ');
        }

        /// <summary>Removes the line breaks from the text.</summary>
        /// <param name="text">The text.</param>
        /// <returns>The updated text.</returns>
        public static string RemoveLineBreaks(string text)
        {
            return text?.Replace("\r", "")
                .Replace("\n", " \n")
                .Replace("\n ", "\n")
                .Replace("  \n", " \n")
                .Replace("\n", "")
                .Trim('\n', '\t', ' ');
        }

        /// <summary>Singularizes the given noun in plural.</summary>
        /// <param name="word">The plural noun.</param>
        /// <returns>The singular noun.</returns>
        public static string Singularize(string word)
        {
            if (word == "people")
            {
                return "Person";
            }

            return word.EndsWith("s") ? word.Substring(0, word.Length - 1) : word;
        }

        /// <summary>Add tabs to the given string.</summary>
        /// <param name="input">The input.</param>
        /// <param name="tabCount">The tab count.</param>
        /// <returns>The output.</returns>
        public static string Tab(string input, int tabCount)
        {
            return input?.Replace("\n", "\n" + string.Join("", Enumerable.Repeat("    ", tabCount))) ?? string.Empty;
        }

        /// <summary>Converts all line breaks in a string into '\n' and removes white spaces.</summary>
        /// <param name="input">The input.</param>
        /// <param name="tabCount">The tab count.</param>
        /// <returns>The output.</returns>
        public static string ConvertCSharpDocs(string input, int tabCount)
        {
            input = input?
                .Replace("\r", string.Empty)
                .Replace("\n", "\n" + string.Join("", Enumerable.Repeat("    ", tabCount)) + "/// ")
                ?? string.Empty;

            // TODO: Support more markdown features here
            var xml = new XText(input).ToString();
            return Regex.Replace(xml, @"^( *)/// ", m => m.Groups[1] + "/// <br/>", RegexOptions.Multiline);
        }

        private static Dictionary<char, string> _wordReplacements = new Dictionary<char, string>
        {
            { '+', "plus" },
            { '*', "star" }
        };

        private static HashSet<string> _reservedKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break",
            "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default",
            "delegate", "do", "double", "else", "enum",
            "event", "explicit", "extern", "false", "finally",
            "fixed", "float", "for", "foreach","goto",
            "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace",
            "new", "null", "object", "operator", "out",
            "override", "params", "private", "protected", "public",
            "readonly", "ref", "return", "sbyte",
            "sealed", "short", "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked",
            "unsafe", "ushort", "using", "virtual", "void",
            "volatile", "while"
        };

        private static IEnumerable<string> SplitWords(string input)
        {
            var sb = new StringBuilder();

            foreach (var c in input)
            {
                var isLetterOrDigit = char.IsLetterOrDigit(c);
                var isUpper = char.IsUpper(c);
                var isReplaceable = _wordReplacements.ContainsKey(c);

                if (!isLetterOrDigit || isUpper || isReplaceable)
                {
                    if (isReplaceable)
                        sb.Append(_wordReplacements[c]);

                    if (sb.Length > 0)
                    {
                        yield return sb.ToString();
                        sb.Clear();
                    }

                    if (!isUpper)
                        continue;
                }

                sb.Append(c);
            }

            if (sb.Length > 0)
                yield return sb.ToString();
        }

        private static string JoinWords(IEnumerable<string> inputs, string join, bool pascal)
        {
            var adjustedInputs = inputs.Select(x => x.ToLower())
                                       .Select((x, i) => pascal || i > 0 ? char.ToUpper(x[0]) + x.Substring(1) : x);

            // Join our Inputs by the join identifier
            var output = string.Join(join, adjustedInputs);

            // If our first character is a number, then we need to prepend it with an underscore
            if (char.IsNumber(output[0]))
                output = $"_{output}";

            return output;
        }

        private static string SplitAndJoin(string input, string join, bool pascal, Func<string, string> postprocess = null)
        {
            var split = SplitWords(input);

            var joined = JoinWords(split, join, pascal);

            var output = postprocess != null
                ? postprocess(joined)
                : joined;

            // If we got a C# reserved keyword, then we need to prepend it with an amperstand
            if (_reservedKeywords.Contains(output))
                output = $"@{output}";

            return output;
        }

        /// <summary>Converts the given input to flat case (twowords).</summary>
        /// <param name="input">The input.</param>
        /// <returns>The output.</returns>
        public static string ConvertNameToFlatCase(string input)
            => SplitAndJoin(input, string.Empty, false, postprocess: (x) => x.ToLower());

        /// <summary>Converts the given input to upper flat case (TWOWORDS).</summary>
        /// <param name="input">The input.</param>
        /// <returns>The output.</returns>
        public static string ConvertNameToUpperFlatCase(string input)
            => SplitAndJoin(input, string.Empty, false, postprocess: (x) => x.ToUpper());

        /// <summary>Converts the given input to camel case (twoWords).</summary>
        /// <param name="input">The input.</param>
        /// <returns>The output.</returns>
        public static string ConvertNameToCamelCase(string input)
            => SplitAndJoin(input, string.Empty, false);

        /// <summary>Converts the given input to pascal case (TwoWords).</summary>
        /// <param name="input">The input.</param>
        /// <returns>The output.</returns>
        public static string ConvertNameToPascalCase(string input)
            => SplitAndJoin(input, string.Empty, true);

        /// <summary>Converts the given input to snake case (two_words).</summary>
        /// <param name="input">The input.</param>
        /// <returns>The output.</returns>
        public static string ConvertNameToSnakeCase(string input)
            => SplitAndJoin(input, "_", false, postprocess: (x) => x.ToLower());

        /// <summary>Converts the given input to pascal snake case (Two_Words).</summary>
        /// <param name="input">The input.</param>
        /// <returns>The output.</returns>
        public static string ConvertNameToPascalSnakeCase(string input)
            => SplitAndJoin(input, "_", true);

        private static string ConvertDashesToCamelCase(string input)
        {
            var sb = new StringBuilder();
            var caseFlag = false;
            foreach (char c in input)
            {
                if (c == '-')
                {
                    caseFlag = true;
                }
                else if (caseFlag)
                {
                    sb.Append(char.ToUpperInvariant(c));
                    caseFlag = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}