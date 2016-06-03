using NJsonSchema.CodeGeneration.TypeScript.Models;

namespace NJsonSchema.CodeGeneration.TypeScript.Templates
{
    internal partial class FileTemplate : ITemplate
    {
        public FileTemplateModel Model { get; private set; }

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
