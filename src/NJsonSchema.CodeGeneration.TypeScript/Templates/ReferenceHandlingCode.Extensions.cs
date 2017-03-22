namespace NJsonSchema.CodeGeneration.TypeScript.Templates
{
    internal partial class ReferenceHandlingCode : ITemplate
    {
        public string Render()
        {
            return ConversionUtilities.TrimWhiteSpaces(TransformText());
        }
    }
}
