//-----------------------------------------------------------------------
// <copyright file="IJsonExtensionObject.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema
{
    /// <summary>The base JSON interface with extension data.</summary>
    public interface IJsonExtensionObject
    {
        /// <summary>Gets or sets the extension data (i.e. additional properties which are not directly defined by the JSON object).</summary>
        IDictionary<string, object?>? ExtensionData { get; set; }
    }
}