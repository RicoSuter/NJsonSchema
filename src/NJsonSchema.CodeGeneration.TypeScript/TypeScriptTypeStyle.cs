//-----------------------------------------------------------------------
// <copyright file="CSharpGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>The TypeScript type styles.</summary>
    public enum TypeScriptTypeStyle
    {
        /// <summary>Generates interfaces which can add typings to existing JSON.</summary>
        Interface,

        /// <summary>Generates classes which can be constructed from JSON.</summary>
        Class,

        /// <summary>Generates classes with KnockoutJS observable properties.</summary>
        KnockoutClass
    }
}