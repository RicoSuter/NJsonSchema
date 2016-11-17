using NJsonSchema.CodeGeneration.CSharp.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Templates
{
    internal partial class ClassTemplate : ITemplate
    {
        public ClassTemplate(ClassTemplateModel model)
        {
            Model = model; 
        }

        public ClassTemplateModel Model { get; }

        public string Render()
        {
            return NJsonSchema.ConversionUtilities.TrimWhiteSpaces(TransformText());
        }
    }
}
