//-----------------------------------------------------------------------
// <copyright file="ConversionUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
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

            var lowered = char.ToLowerInvariant(input[0]) + (input.Length > 1 ? input.Substring(1) : "");
            var cleaned = lowered.Replace(' ', '_').Replace('/', '_');
            input = ConvertDashesToCamelCase(cleaned);

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

            var cleaned = Capitalize(input).Replace(' ', '_').Replace('/', '_');
            input = ConvertDashesToCamelCase(cleaned);

            if (firstCharacterMustBeAlpha && char.IsNumber(input[0]))
            {
                return "_" + input;
            }

            return input;
        }

        [MethodImpl((MethodImplOptions) 256)]
        private static string Capitalize(string input)
        {
            if (char.IsUpper(input[0]))
            {
                return input;
            }
            if (input.Length == 1)
            {
                return char.ToUpperInvariant(input[0]).ToString();
            }
            return char.ToUpperInvariant(input[0]) + input.Substring(1);
        }

        /// <summary>Converts the string to a string literal which can be used in C# or TypeScript code.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The literal.</returns>
        public static string ConvertToStringLiteral(string input) => ConvertToStringLiteral(input, null, null);

        internal static string ConvertToStringLiteral(string input, string? prefix, string? postfix)
        {
            using var literal = new ValueStringBuilder(input.Length + (prefix?.Length ?? 0) + (postfix?.Length ?? 0));

            if (prefix != null)
            {
                literal.Append(prefix);
            }

            foreach (var c in input)
            {
                switch (c)
                {
                    case '\'':
                        literal.Append(@"\'");
                        break;
                    case '\"':
                        literal.Append("\\\"");
                        break;
                    case '\\':
                        literal.Append(@"\\");
                        break;
                    case '\0':
                        literal.Append(@"\0");
                        break;
                    case '\a':
                        literal.Append(@"\a");
                        break;
                    case '\b':
                        literal.Append(@"\b");
                        break;
                    case '\f':
                        literal.Append(@"\f");
                        break;
                    case '\n':
                        literal.Append(@"\n");
                        break;
                    case '\r':
                        literal.Append(@"\r");
                        break;
                    case '\t':
                        literal.Append(@"\t");
                        break;
                    case '\v':
                        literal.Append(@"\v");
                        break;
                    default:
                        // ASCII printable character
                        if (c is >= (char)0x20 and <= (char)0x7e)
                        {
                            literal.Append(c);
                            // As UTF16 escaped character
                        }
                        else
                        {
                            literal.Append(@"\u");
                            literal.Append(((int) c).ToString("x4", CultureInfo.InvariantCulture));
                        }

                        break;
                }
            }

            if (postfix != null)
            {
                literal.Append(postfix);
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


        private static readonly char[] _whiteSpaceChars = ['\n', '\r', '\t', ' '];

        /// <summary>Trims white spaces from the text.</summary>
        /// <param name="text">The text.</param>
        /// <returns>The updated text.</returns>
        public static string TrimWhiteSpaces(string? text)
        {
            return text?.Trim(_whiteSpaceChars) ?? string.Empty;
        }

        /// <summary>Trims white spaces from the text.</summary>
        /// <param name="text">The text.</param>
        /// <returns>The updated text.</returns>
        public static ReadOnlySpan<char> TrimWhiteSpaces(ReadOnlySpan<char> text)
        {
            return text.Trim(_whiteSpaceChars);
        }

        private static readonly char[] _lineBreakTrimChars = ['\n', '\t', ' '];

        /// <summary>Removes the line breaks from the text.</summary>
        /// <param name="text">The text.</param>
        /// <returns>The updated text.</returns>
        public static string RemoveLineBreaks(string? text)
        {
            return text?.Replace("\r", "")
                .Replace("\n", " \n")
                .Replace("\n ", "\n")
                .Replace("  \n", " \n")
                .Replace("\n", "")
                .Trim(_lineBreakTrimChars) ?? string.Empty;
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

            return word.EndsWith('s') ? word.Substring(0, word.Length - 1) : word;
        }

        /// <summary>Add tabs to the given string.</summary>
        /// <param name="input">The input.</param>
        /// <param name="tabCount">The tab count.</param>
        /// <returns>The output.</returns>
        public static string Tab(string input, int tabCount)
        {
            if (input is null)
            {
                return "";
            }

            var tabString = CreateTabString(tabCount);
            if (tabString.Length == 0)
            {
                return input;
            }

            using var stringBuilder = new ValueStringBuilder(input.Length);
            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];
                stringBuilder.Append(c);
                if (c == '\n')
                {
                    // only write if not entirely empty line
                    var foundNonEmptyBeforeNewLine = false;
                    for (var j = i + 1; j < input.Length; ++j)
                    {
                        var c2 = input[j];
                        if (c2 == '\n')
                        {
                            break;
                        }

                        if (!char.IsWhiteSpace(c2))
                        {
                            foundNonEmptyBeforeNewLine = true;
                            break;
                        }
                    }

                    if (foundNonEmptyBeforeNewLine)
                    {
                        stringBuilder.Append(tabString);
                    }
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>Add tabs to the given string.</summary>
        /// <param name="input">The input.</param>
        /// <param name="tabCount">The tab count.</param>
        /// <param name="writer">Stream to write transformed content into.</param>
        /// <returns>The output.</returns>
        public static void Tab(string input, int tabCount, TextWriter writer)
        {
            var tabString = CreateTabString(tabCount);
            AddPrefixToBeginningOfNonEmptyLines(input, tabString, writer);
        }

        private static void AddPrefixToBeginningOfNonEmptyLines(string input, string tabString, TextWriter writer)
        {
            if (tabString.Length == 0)
            {
                return;
            }

            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];
                writer.Write(c);
                if (c == '\n')
                {
                    // only write if not entirely empty line
                    var foundNonEmptyBeforeNewLine = false;
                    for (var j = i + 1; j < input.Length; ++j)
                    {
                        var c2 = input[j];
                        if (c2 == '\n')
                        {
                            break;
                        }

                        if (!char.IsWhiteSpace(c2))
                        {
                            foundNonEmptyBeforeNewLine = true;
                            break;
                        }
                    }

                    if (foundNonEmptyBeforeNewLine)
                    {
                        writer.Write(tabString);
                    }
                }
            }
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

        private static string CreateTabString(int tabCount)
        {
            if (tabCount == 0)
            {
                return "";
            }

            if (tabCount == 1)
            {
                return "    ";
            }

            if (tabCount == 2)
            {
                return "        ";
            }

            var tabString = new string(' ', 4 * tabCount);
            return tabString;
        }

        private static string ConvertDashesToCamelCase(string input)
        {
            if (!input.Contains('-'))
            {
                // no conversion necessary
                return input;
            }

            // we are removing at least one character
            var sb = new ValueStringBuilder(input.Length - 1);
            var caseFlag = false;
            foreach (var c in input)
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