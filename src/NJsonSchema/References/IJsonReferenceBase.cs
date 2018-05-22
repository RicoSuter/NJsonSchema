//-----------------------------------------------------------------------
// <copyright file="IJsonReferenceBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json;

namespace NJsonSchema.References
{
    /// <summary>A JSON object which may reference other objects with $ref.</summary>
    public interface IJsonReferenceBase : IDocumentPathProvider
    {
        /// <summary>Gets or sets the type reference path ($ref). </summary>
        [JsonProperty(JsonPathUtilities.ReferenceReplaceString, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        string ReferencePath { get; set; }

        /// <summary>Gets or sets the referenced object.</summary>
        [JsonIgnore]
        IJsonReference Reference { get; set; }
    }
}