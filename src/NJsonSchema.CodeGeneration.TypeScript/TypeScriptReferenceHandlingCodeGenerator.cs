using NJsonSchema.CodeGeneration.TypeScript.Templates;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Generates the code to handle JSON references.</summary>
    public static class TypeScriptReferenceHandlingCodeGenerator
    {
        /// <summary>Generates the code to handle JSON references.</summary>
        /// <returns>The code.</returns>
        public static string Generate()
        {
            var tpl = new ReferenceHandlingCode();
            return tpl.Render();
        }
    }
}
