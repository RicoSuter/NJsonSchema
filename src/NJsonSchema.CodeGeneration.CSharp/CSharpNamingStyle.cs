//-----------------------------------------------------------------------
// <copyright file="CSharpClassStyle.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>The CSharp naming styles.</summary>
    public enum CSharpNamingStyle
    {
        /// <summary>Generate Names with flat case (twowords).</summary>
        FlatCase,

        /// <summary>Generate Names with upper flat case (TWOWORDS).</summary>
        UpperFlatCase,

        /// <summary>Generate Names with camel case (twoWords).</summary>
        CamelCase,

        /// <summary>Generate Names with pascal case (TwoWords).</summary>
        PascalCase,

        /// <summary>Generate Names with snake case (two_words).</summary>
        SnakeCase,

        /// <summary>Generate Names with pascal snake case (Two_Words).</summary>
        PascalSnakeCase
    }
}
