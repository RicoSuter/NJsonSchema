using NJsonSchema.CodeGeneration.CSharp.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Templates
{
    internal partial class EnumTemplate : ITemplate
    {
        public EnumTemplate(EnumTemplateModel model)
        {
            Model = model;
        }

        public EnumTemplateModel Model { get; }
        
        public string Render()
        {
            return NJsonSchema.ConversionUtilities.TrimWhiteSpaces(TransformText());
        }
    }
}
