using System.Linq;
using System.Text.RegularExpressions;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Provides access to the extension code (used in TypeScript).</summary>
    public class TypeScriptExtensionCode : ExtensionCode
    {
        /// <summary>Initializes a new instance of the <see cref="ExtensionCode"/> class.</summary>
        /// <param name="code">The code.</param>
        /// <param name="extendedClasses">The list of extended class names.</param>
        public TypeScriptExtensionCode(string code, string[] extendedClasses)
        {
            code = code.Replace("\r", string.Empty);

            code = Regex.Replace(code, "import generated = (.*?)\\n", string.Empty, RegexOptions.Multiline);
            code = Regex.Replace(code, "(import (.*?) = (.*?)\\n)|(/// <reference path(.*?)\\n)", match =>
            {
                CodeBefore += ConversionUtilities.TrimWhiteSpaces(match.Groups[0].Value) + "\n";
                return string.Empty;
            }, RegexOptions.Multiline);

            CodeBefore = ConversionUtilities.TrimWhiteSpaces(CodeBefore);

            code = Regex.Replace(code, "(export )?class (.*?) ([\\s\\S]*?)\\n}", match =>
            {
                var className = match.Groups[2].Value;
                if (extendedClasses?.Contains(className) == true)
                {
                    Classes[className] = (match.Groups[1].Success ? match.Groups[0].Value : "export " + match.Groups[0].Value).Replace("generated.", string.Empty);
                    return string.Empty;
                }
                return match.Groups[0].Value;
            }, RegexOptions.Multiline);

            CodeAfter = ConversionUtilities.TrimWhiteSpaces(code);
        }
    }
}