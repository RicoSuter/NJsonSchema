//-----------------------------------------------------------------------
// <copyright file="JsonInheritanceConverterTemplate.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using NJsonSchema.CodeGeneration.CSharp.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Templates
{
    internal partial class JsonInheritanceConverterTemplate
    {
        public JsonInheritanceConverterTemplate(JsonInheritanceConverterTemplateModel model)
        {
            Model = model;
        }

        public JsonInheritanceConverterTemplateModel Model { get; }

        public string Render()
        {
            return ConversionUtilities.TrimWhiteSpaces(TransformText());
        }
    }
}
