//-----------------------------------------------------------------------
// <copyright file="ExtensionCode.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>Provides access to the extension code.</summary>
    public abstract class ExtensionCode
    {
        /// <summary>Gets the code of the class extension.</summary>
        public Dictionary<string, string> Classes { get; protected set; } = new Dictionary<string, string>();

        /// <summary>Gets the extension code which is inserted at the start of the generated code (e.g. TypeScript imports).</summary>
        public string CodeBefore { get; protected set; } = string.Empty;

        /// <summary>Gets the extension code which is appended at the end of the generated code.</summary>
        public string CodeAfter { get; protected set; }
    }
}