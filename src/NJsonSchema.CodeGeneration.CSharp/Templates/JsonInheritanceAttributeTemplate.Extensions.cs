//-----------------------------------------------------------------------
// <copyright file="JsonInheritanceConverterTemplate.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.CSharp.Templates
{
    internal partial class JsonInheritanceAttributeTemplate : ITemplate
    {
        public JsonInheritanceAttributeTemplate(object model)
        {
            Model = model;
        }

        public object Model { get; }

        public string Render()
        {
            return ConversionUtilities.TrimWhiteSpaces(TransformText());
        }
    }
}
