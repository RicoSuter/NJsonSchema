using NJsonSchema.CodeGeneration.CSharp.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Templates
{
    public partial class FileTemplate : ITemplate
    {
        internal FileTemplateModel Model { get; private set; }

        /// <summary>Initializes the template with a model.</summary>
        /// <param name="model">The model.</param>
        public void Initialize(object model)
        {
            Model = (FileTemplateModel)model;
        }

        /// <summary>Renders the template.</summary>
        /// <returns>The output.</returns>
        public string Render()
        {
            return ConversionUtilities.TrimWhiteSpaces(TransformText());
        }
    }
}
