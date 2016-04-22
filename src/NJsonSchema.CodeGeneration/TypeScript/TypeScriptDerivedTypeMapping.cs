//-----------------------------------------------------------------------
// <copyright file="TypeScriptDerivedTypeMapping.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Desribes a mapping from a type to a derived to so that generated classes can be extended.</summary>
    public class TypeScriptDerivedTypeMapping
    {
        /// <summary>Gets or sets the original type name.</summary>
        public string Type { get; set; }

        /// <summary>Gets or sets the new type name.</summary>
        public string NewType { get; set; }

        /// <summary>The module</summary>
        public string Module { get; set; }
    }
}