using NJsonSchema.CodeGeneration.TypeScript.Models;

namespace NJsonSchema.CodeGeneration.TypeScript.Templates
{
    public partial class EnumTemplate
    {
        internal EnumTemplate(EnumTemplateModel model)
        {
            Model = model;
        }

        internal EnumTemplateModel Model { get; set; }
    }
}
