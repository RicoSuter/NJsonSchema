//-----------------------------------------------------------------------
// <copyright file="CSharpGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
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