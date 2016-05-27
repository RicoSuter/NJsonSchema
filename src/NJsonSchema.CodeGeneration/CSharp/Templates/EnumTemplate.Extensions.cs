using NJsonSchema.CodeGeneration.CSharp.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Templates
{
    internal partial class EnumTemplate : ITemplate
    {
        public EnumTemplateModel Model { get; set; }
        
        /// <summary>Initializes the template with a model.</summary>
        /// <param name="model">The model.</param>
        public void Initialize(object model)
        {
            Model = (EnumTemplateModel)model;
        }

        /// <summary>Renders the template.</summary>
        /// <returns>The output.</returns>
        public string Render()
        {
            return ConversionUtilities.TrimWhiteSpaces(TransformText());
        }
    }
}
