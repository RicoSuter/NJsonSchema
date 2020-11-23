//-----------------------------------------------------------------------
// <copyright file="CodeArtifactCategory.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The code artifact category.</summary>
    public enum CodeArtifactCategory
    {
        /// <summary>Undefined.</summary>
        Undefined,

        /// <summary>Client.</summary>
        Client,

        /// <summary>Contract.</summary>
        Contract,

        /// <summary>Utility.</summary>
        Utility
    }
}