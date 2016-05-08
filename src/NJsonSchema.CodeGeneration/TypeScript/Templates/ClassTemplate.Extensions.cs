﻿namespace NJsonSchema.CodeGeneration.TypeScript.Templates
{
    public partial class ClassTemplate : ITemplate
    {
        internal dynamic Model { get; set; }

        /// <summary>Initializes the template with a model.</summary>
        /// <param name="model">The model.</param>
        public void Initialize(object model)
        {
            Model = model; 
        }

        /// <summary>Renders the template.</summary>
        /// <returns>The output.</returns>
        public string Render()
        {
            return TransformText();
        }
    }
}
