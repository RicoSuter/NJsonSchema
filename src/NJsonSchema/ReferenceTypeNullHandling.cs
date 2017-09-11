//-----------------------------------------------------------------------
// <copyright file="ReferenceTypeNullHandling.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema
{
    /// <summary>Specifies the default null handling for reference types.</summary>
    public enum ReferenceTypeNullHandling
    {
        /// <summary>Reference types can be null by default (C# default).</summary>
        Null,

        /// <summary>Reference types cannot be null by default.</summary>
        NotNull
    }
}