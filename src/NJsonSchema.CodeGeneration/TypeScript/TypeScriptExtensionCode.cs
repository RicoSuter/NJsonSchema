using System.Text.RegularExpressions;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Provides access to the extension code (used in TypeScript).</summary>
    public class TypeScriptExtensionCode : ExtensionCode
    {
        /// <summary>Initializes a new instance of the <see cref="ExtensionCode"/> class.</summary>
        /// <param name="code">The code.</param>
        public TypeScriptExtensionCode(string code)
        {
            code = code.Replace("\r", string.Empty);

            code = Regex.Replace(code, "import generated = (.*?)\\n", string.Empty, RegexOptions.Multiline);
            code = Regex.Replace(code, "import (.*?) = (.*?)\\n", match =>
            {
                CodeBefore += ConversionUtilities.TrimWhiteSpaces(match.Groups[0].Value) + "\n";
                return string.Empty;
            }, RegexOptions.Multiline);

            CodeBefore = ConversionUtilities.TrimWhiteSpaces(CodeBefore);

            code = Regex.Replace(code, "(export )?class (.*?) ([\\s\\S]*?)\\n}", match =>
            {
                Classes[match.Groups[2].Value] = (match.Groups[1].Success ? match.Groups[0].Value : "export " + match.Groups[0].Value).Replace("generated.", string.Empty);
                return string.Empty;
            }, RegexOptions.Multiline);

            CodeAfter = ConversionUtilities.TrimWhiteSpaces(code);
        }
    }
}