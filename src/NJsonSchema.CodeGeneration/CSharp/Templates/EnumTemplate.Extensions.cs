using NJsonSchema.CodeGeneration.CSharp.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Templates
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
