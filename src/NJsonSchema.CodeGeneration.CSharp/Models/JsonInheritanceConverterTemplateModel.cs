//-----------------------------------------------------------------------
// <copyright file="JsonInheritanceConverterTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.CSharp.Models
{
    /// <summary>The JsonInheritanceConverterTemplateModel.</summary>
    public class JsonInheritanceConverterTemplateModel
    {
        private readonly CSharpGeneratorSettings _settings;

        /// <summary>The JsonInheritanceConverterTemplateModel.</summary>
        public JsonInheritanceConverterTemplateModel(CSharpGeneratorSettings settings)
        {
            _settings = settings;
        }

        /// <summary>Gets or sets a value indicating whether to generate the JsonInheritanceAttribute class.</summary>
        public bool GenerateJsonInheritanceAttributeClass => _settings.GenerateJsonInheritanceAttributeClass;

        /// <summary>Gets or sets a value indicating whether to generate the GenerateJsonInheritanceConverterClass class.</summary>
        public bool GenerateJsonInheritanceConverterClass => _settings.GenerateJsonInheritanceConverterClass;
    }
}
