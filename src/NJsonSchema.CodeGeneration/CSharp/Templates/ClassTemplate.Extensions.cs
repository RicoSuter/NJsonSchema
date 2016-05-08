using NJsonSchema.CodeGeneration.CSharp.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Templates
{
    public partial class ClassTemplate
    {
        internal ClassTemplate(ClassTemplateModel model)
        {
            Model = model; 
        }

        internal ClassTemplateModel Model { get; }
    }
}
