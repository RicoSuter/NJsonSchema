//-----------------------------------------------------------------------
// <copyright file="JsonExtensionObject.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace NJsonSchema
{
    /// <summary>The base JSON class with extension data.</summary>
    public class JsonExtensionObject : IJsonExtensionObject
    {
        /// <summary>Gets or sets the extension data (i.e. additional properties which are not directly defined by the JSON object).</summary>
        [JsonExtensionData]
        public IDictionary<string, object?>? ExtensionData { get; set; }
    }
}
