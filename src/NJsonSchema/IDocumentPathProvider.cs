//-----------------------------------------------------------------------
// <copyright file="IDocumentPathProvider.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json;

namespace NJsonSchema
{
    /// <summary>Provides a property to get a documents path or base URI.</summary>
    public interface IDocumentPathProvider
    {
        /// <summary>Gets the document path (URI or file path).</summary>
        [JsonIgnore]
        string? DocumentPath { get; set; }
    }
}