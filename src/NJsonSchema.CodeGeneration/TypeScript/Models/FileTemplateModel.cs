//-----------------------------------------------------------------------
// <copyright file="FileTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.TypeScript.Models
{
    internal class FileTemplateModel
    {
        public string Toolchain { get; set; }

        public string Namespace { get; set; }

        public string Types { get; set; }

        public string ExtensionCodeBefore { get; set; }

        public string ExtensionCodeAfter { get; set; }

        public bool HasModuleName { get; set; }

        public string ModuleName { get; set; }
    }
}