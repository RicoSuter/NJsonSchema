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
    internal partial class DateFormatConverterTemplate : ITemplate
    {
        public DateFormatConverterTemplate(DateFormatConverterTemplateModel model)
        {
            Model = model;
        }

        public DateFormatConverterTemplateModel Model { get; }

        public string Render()
        {
            return ConversionUtilities.TrimWhiteSpaces(TransformText());
        }
    }
}
