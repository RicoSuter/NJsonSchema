//-----------------------------------------------------------------------
// <copyright file="NullHandling.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema
{
    /// <summary>Defines how to express the nullability of a property.</summary>
    public enum SchemaType
    {
        /// <summary>Uses oneOf with null schema and null type to express the nullability of a property (valid JSON Schema draft v4).</summary>
        JsonSchema,

        /// <summary>Uses required to express the nullability of a property (not valid in JSON Schema draft v4).</summary>
        Swagger2,

        /// <summary>Uses null handling of Open API 3.</summary>
        OpenApi3
    }
}