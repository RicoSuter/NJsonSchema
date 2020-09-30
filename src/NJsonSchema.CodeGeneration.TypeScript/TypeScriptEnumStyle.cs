//-----------------------------------------------------------------------
// <copyright file="CSharpGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>The TypeScript enum styles.</summary>
    public enum TypeScriptEnumStyle
    {
        /// <summary>Generates enum.</summary>
        Enum,

        /// <summary>Generates enum as a string literal.</summary>
        StringLiteral,
    }
}