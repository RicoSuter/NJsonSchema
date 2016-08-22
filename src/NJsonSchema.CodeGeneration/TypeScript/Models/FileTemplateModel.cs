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
        /// <summary>Gets or sets the toolchain version.</summary>
        public string Toolchain { get; set; }

        /// <summary>Gets or sets the namespace.</summary>
        public string Namespace { get; set; }

        /// <summary>Gets or sets the code of all types.</summary>
        public string Types { get; set; }

        /// <summary>Gets or sets the extension code to insert at the beginning.</summary>
        public string ExtensionCodeBefore { get; set; }

        /// <summary>Gets or sets the extension code to insert at the end.</summary>
        public string ExtensionCodeAfter { get; set; }

        /// <summary>Gets or sets a value indicating whether the file has module name.</summary>
        public bool HasModuleName { get; set; }

        /// <summary>Gets or sets the name of the module.</summary>
        public string ModuleName { get; set; }
    }
}