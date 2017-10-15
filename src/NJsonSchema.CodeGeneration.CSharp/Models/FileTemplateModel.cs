//-----------------------------------------------------------------------
// <copyright file="FileTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.CSharp.Models
{
    /// <summary>The CSharp file template model.</summary>
    public class FileTemplateModel
    {
        /// <summary>Gets or sets the namespace.</summary>
        public string Namespace { get; set; }

        /// <summary>Gets or sets the types code.</summary>
        public string TypesCode { get; set; }
    }
}