//-----------------------------------------------------------------------
// <copyright file="DescriptionAttributeType.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.Infrastructure
{
    /// <summary>Specifies what attribute types to use to resolve the description.</summary>
    public enum DescriptionAttributeType
    {
        /// <summary>The context attributes.</summary>
        Context,

        /// <summary>The type attributes.</summary>
        Type
    }
}