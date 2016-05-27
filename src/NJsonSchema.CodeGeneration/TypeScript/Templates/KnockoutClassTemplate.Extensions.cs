//-----------------------------------------------------------------------
// <copyright file="KnockoutClassTemplate.Extensions.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.TypeScript.Templates
{
    internal partial class KnockoutClassTemplate : ITemplate
    {
        public dynamic Model { get; set; }

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
            return ConversionUtilities.TrimWhiteSpaces(TransformText());
        }
    }
}
