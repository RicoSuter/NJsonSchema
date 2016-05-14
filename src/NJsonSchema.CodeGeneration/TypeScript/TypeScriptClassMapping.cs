//-----------------------------------------------------------------------
// <copyright file="TypeScriptClassMapping.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Describes a type mapping.</summary>
    public class TypeScriptClassMapping
    {
        /// <summary>Gets or sets the original type name.</summary>
        public string Class { get; set; }

        /// <summary>Gets or sets the name of the target class.</summary>
        public string TargetClass { get; set; }
    }
}