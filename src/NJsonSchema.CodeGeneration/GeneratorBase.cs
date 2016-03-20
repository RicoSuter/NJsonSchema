//-----------------------------------------------------------------------
// <copyright file="GeneratorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Globalization;
using System.IO;
using System.Text;
using Antlr4.StringTemplate;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The base class of the code generators</summary>
    public abstract class GeneratorBase
    {
        /// <summary>Gets the language.</summary>
        protected abstract string Language { get; }

        /// <summary>Generates the the whole file containing all needed types.</summary>
        /// <returns>The code</returns>
        public abstract string GenerateFile();

        /// <summary>Loads the template from an embedded resource.</summary>
        /// <param name="file">The file name.</param>
        /// <returns>The template. </returns>
        protected Template LoadTemplate(string file)
        {
            var assembly = GetType().Assembly;
            var prefix = assembly.GetName().Name == "NJsonSchema.CodeGeneration" ? assembly.GetName().Name : assembly.GetName().Name + ".CodeGenerators";
            var resourceName = string.Format("{0}.{1}.Templates.{2}.txt", prefix, Language, file);
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();
                    return new Template(text);
                }
            }
        }

        /// <summary>Converts the first letter to lower case and dashes to camel case.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The converted input. </returns>
        public static string ConvertToLowerCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return ConvertDashesToCamelCase((input[0].ToString(CultureInfo.InvariantCulture).ToLowerInvariant() + input.Substring(1)).Replace(" ", "_"));
        }

        /// <summary>Converts the first letter to upper case and dashes to camel case.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The converted input. </returns>
        public static string ConvertToUpperCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return ConvertDashesToCamelCase((input[0].ToString(CultureInfo.InvariantCulture).ToUpperInvariant() + input.Substring(1)).Replace(" ", "_"));
        }

        /// <summary>Converts the input to a camel case identifier.</summary>
        /// <param name="input">The input.</param>
        /// <returns>The converted input. </returns>
        public static string ConvertToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return ConvertDashesToCamelCase(input.Replace(" ", "_"));
        }

        /// <summary>Removes the line breaks from the .</summary>
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

        private static string ConvertDashesToCamelCase(string input)
        {
            var sb = new StringBuilder();
            var caseFlag = false;
            foreach (char c in input)
            {
                if (c == '-')
                    caseFlag = true;
                else if (caseFlag)
                {
                    sb.Append(char.ToUpper(c));
                    caseFlag = false;
                }
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }
}