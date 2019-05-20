//-----------------------------------------------------------------------
// <copyright file="ReferenceTypeNullHandling.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.Generation
{
    /// <summary>Specifies the default null handling for reference types when no nullability information is available.</summary>
    public enum ReferenceTypeNullHandling
    {
        /// <summary>Use default behavior of the current runtime (e.g. use Null Reference Type annotations if available or Null references).</summary>
        Default,

        /// <summary>Reference types are nullable by default (C# default).</summary>
        Null,

        /// <summary>Reference types cannot be null by default.</summary>
        NotNull
    }
}