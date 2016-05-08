using NJsonSchema.CodeGeneration.CSharp.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Templates
{
    public partial class FileTemplate
    {
        internal FileTemplate(FileTemplateModel model)
        {
            Model = model;
        }

        internal FileTemplateModel Model { get; }
    }
}
