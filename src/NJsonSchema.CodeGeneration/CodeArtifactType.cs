//-----------------------------------------------------------------------
// <copyright file="TypeGeneratorResult.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The code artifact type.</summary>
    public enum CodeArtifactType
    {
        /// <summary>Undefined.</summary>
        Undefined,

        /// <summary>A class (e.g. C# or TypeScript class).</summary>
        Class,

        /// <summary>An interface (e.g. C# or TypeScript interface).</summary>
        Interface,

        /// <summary>An enum (e.g. C# or TypeScript interface).</summary>
        Enum,

        /// <summary>An internal function.</summary>
        Function
    }
}