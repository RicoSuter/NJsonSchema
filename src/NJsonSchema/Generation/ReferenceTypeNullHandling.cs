//-----------------------------------------------------------------------
// <copyright file="ReferenceTypeNullHandling.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.Generation
{
    /// <summary>Specifies the default null handling for reference types when no nullability information is available.</summary>
    public enum ReferenceTypeNullHandling
    {
        /// <summary>Reference types are nullable by default (C# default).</summary>
        Null,

        /// <summary>Reference types cannot be null by default.</summary>
        NotNull
    }
}