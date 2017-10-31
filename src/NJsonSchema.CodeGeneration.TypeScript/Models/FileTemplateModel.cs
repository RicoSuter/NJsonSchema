//-----------------------------------------------------------------------
// <copyright file="FileTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.TypeScript.Models
{
    /// <summary>The TypeScript file template model.</summary>
    public class FileTemplateModel
    {
        private readonly TypeScriptGeneratorSettings _settings;

        /// <summary>Initializes a new instance of the <see cref="FileTemplateModel"/> class.</summary>
        /// <param name="settings">The settings.</param>
        public FileTemplateModel(TypeScriptGeneratorSettings settings)
        {
            _settings = settings; 
        }

        /// <summary>Gets or sets the code of all types.</summary>
        public string Types { get; set; }

        /// <summary>Gets or sets the extension code.</summary>
        public TypeScriptExtensionCode ExtensionCode { get; set; }

        /// <summary>Gets or sets a value indicating whether the file has module name.</summary>
        public bool HasModuleName => !string.IsNullOrEmpty(_settings.ModuleName);

        /// <summary>Gets or sets the name of the module.</summary>
        public string ModuleName => _settings.ModuleName;

        /// <summary>Gets or sets a value indicating whether the file has module name.</summary>
        public bool HasNamespace => !string.IsNullOrEmpty(_settings.Namespace);

        /// <summary>Gets or sets the name of the module.</summary>
        public string Namespace => _settings.Namespace;

        /// <summary>Gets a value indicating whether to handle JSON references.</summary>
        public bool HandleReferences => _settings.HandleReferences;
    }
}