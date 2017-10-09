//-----------------------------------------------------------------------
// <copyright file="IJsonExtensionObject.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace NJsonSchema
{
    /// <summary>The base JSON interface with extension data.</summary>
    public interface IJsonExtensionObject
    {
        /// <summary>Gets or sets the extension data (i.e. additional properties which are not directly defined by the JSON object).</summary>
        IDictionary<string, object> ExtensionData { get; set; }
    }
}