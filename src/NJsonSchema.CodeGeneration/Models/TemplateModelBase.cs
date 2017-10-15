//-----------------------------------------------------------------------
// <copyright file="TemplateModelBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.Models
{
    /// <summary>The base template model.</summary>
    public class TemplateModelBase
    {
        /// <summary>Gets the NJsonSchema toolchain version.</summary>
        public string ToolchainVersion => JsonSchema4.ToolchainVersion;
    }
}
