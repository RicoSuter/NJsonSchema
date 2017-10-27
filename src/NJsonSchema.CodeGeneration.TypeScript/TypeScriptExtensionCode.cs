using System.Linq;
using System.Text.RegularExpressions;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Provides access to the extension code (used in TypeScript).</summary>
    public class TypeScriptExtensionCode : ExtensionCode
    {
        /// <summary>Initializes a new instance of the <see cref="ExtensionCode" /> class.</summary>
        /// <param name="code">The code.</param>
        /// <param name="extendedClasses">The extended classes.</param>
        /// <param name="baseClasses">The base classes.</param>
        public TypeScriptExtensionCode(string code, string[] extendedClasses, string[] baseClasses = null)
        {
            code = code
                .Replace("\r", string.Empty)
                .Replace("generated.", string.Empty) + "\n";

            code = Regex.Replace(code, "(.*?)// ignore(.*?)\\n", string.Empty, RegexOptions.Multiline | RegexOptions.IgnoreCase);

            code = Regex.Replace(code, "import generated (=|from) (.*?)\\n", string.Empty, RegexOptions.Multiline);
            code = Regex.Replace(code, "import \\* as generated from (.*?)\\n", string.Empty, RegexOptions.Multiline);
            code = Regex.Replace(code, "(import '.*?'.*?\\n)|(import ((.|\\n)*?) (=|from) (.*?)\\n)|(/// <reference path(.*?)\\n)", match =>
            {
                ImportCode += ConversionUtilities.TrimWhiteSpaces(match.Groups[0].Value) + "\n";
                return string.Empty;
            }, RegexOptions.Multiline);

            code = Regex.Replace(code, "(@.*\n)?(export )?[^\n]*?class (.*?) ([\\s\\S]*?)\\n}", match =>
            {
                var hasExport = match.Groups[2].Success;
                var className = match.Groups[3].Value;
                var classCode = hasExport ? match.Groups[0].Value : match.Groups[0].Value.Replace("class " + className, "export class " + className);

                if (extendedClasses?.Contains(className) == true)
                {
                    ExtensionClasses[className] = classCode;
                    return string.Empty;
                }

                if (baseClasses?.Contains(className) == true)
                {
                    TopCode += classCode + "\n\n";
                    return string.Empty;
                }

                return match.Groups[0].Value;
            }, RegexOptions.Multiline);

            ImportCode = ConversionUtilities.TrimWhiteSpaces(ImportCode);
            TopCode = ConversionUtilities.TrimWhiteSpaces(TopCode);
            BottomCode = ConversionUtilities.TrimWhiteSpaces(code.Trim());
        }
    }
}