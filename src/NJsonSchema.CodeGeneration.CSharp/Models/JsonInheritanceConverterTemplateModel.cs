//-----------------------------------------------------------------------
// <copyright file="JsonInheritanceConverterTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.CSharp.Models
{
    /// <summary>The JsonInheritanceConverterTemplateModel.</summary>
    public class JsonInheritanceConverterTemplateModel
    {
        private readonly CSharpGeneratorSettings _settings;

        /// <summary>The DateFormatConverterTemplateModel.</summary>
        public JsonInheritanceConverterTemplateModel(CSharpGeneratorSettings settings)
        {
            _settings = settings;
        }

        /// <summary>Gets a value indicating whether to use System.Text.Json</summary>
        public bool UseSystemTextJson => _settings.JsonLibrary == CSharpJsonLibrary.SystemTextJson;
    }
}
