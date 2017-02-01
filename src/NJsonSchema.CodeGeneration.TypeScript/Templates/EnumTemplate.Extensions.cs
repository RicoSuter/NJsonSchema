//-----------------------------------------------------------------------
// <copyright file="EnumTemplate.Extensions.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using NJsonSchema.CodeGeneration.TypeScript.Models;

namespace NJsonSchema.CodeGeneration.TypeScript.Templates
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
            return ConversionUtilities.TrimWhiteSpaces(TransformText());
        }
    }
}
