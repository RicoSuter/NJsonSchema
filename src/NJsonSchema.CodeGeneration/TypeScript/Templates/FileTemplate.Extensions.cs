using NJsonSchema.CodeGeneration.TypeScript.Models;

namespace NJsonSchema.CodeGeneration.TypeScript.Templates
{
    internal partial class FileTemplate : ITemplate
    {
        public FileTemplate(FileTemplateModel model)
        {
            Model = model;
        }

        public FileTemplateModel Model { get; }
        
        public string Render()
        {
            return ConversionUtilities.TrimWhiteSpaces(TransformText());
        }
    }
}
