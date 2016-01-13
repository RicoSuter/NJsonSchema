//-----------------------------------------------------------------------
// <copyright file="CSharpDateTimeType.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>The CSharp date time type.</summary>
    public enum CSharpDateTimeType
    {
        /// <summary>The .NET DateTime type.</summary>
        DateTime,

        /// <summary>The .NET DateTimeOffset type.</summary>
        DateTimeOffset
    }
}