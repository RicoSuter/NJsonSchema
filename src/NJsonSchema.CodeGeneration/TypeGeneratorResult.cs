//-----------------------------------------------------------------------
// <copyright file="TypeGeneratorResult.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The type generator result.</summary>
    public class TypeGeneratorResult
    {
        /// <summary>Gets or sets the type name.</summary>
        public string TypeName { get; set; }

        /// <summary>Gets or sets the generated code.</summary>
        public string Code { get; set; }
    }
}