//-----------------------------------------------------------------------
// <copyright file="IJsonReferenceBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace NJsonSchema.References
{
    /// <summary>A JSON object which may reference other objects with $ref.</summary>
    public interface IJsonReferenceBase : IDocumentPathProvider
    {
        /// <summary>Gets or sets the type reference path ($ref). </summary>
        [JsonPropertyName("$ref")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        string? ReferencePath { get; set; }

        /// <summary>Gets or sets the referenced object.</summary>
        [JsonIgnore]
        IJsonReference? Reference { get; set; }
    }
}
