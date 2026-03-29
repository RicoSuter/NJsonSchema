//-----------------------------------------------------------------------
// <copyright file="JsonProperty.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace NJsonSchema
{
    /// <summary>A description of a JSON property of a JSON object (used in Swagger specifications). </summary>
    public class JsonXmlObject
    {
        /// <summary>Gets the parent schema of the XML object schema. </summary>
        [JsonIgnore]
        public JsonSchema? ParentSchema { get; internal set; }

        /// <summary>Gets or sets the name of the xml object. </summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Name { get; internal set; }

        /// <summary>Gets or sets if the array elements are going to be wrapped or not. </summary>
        [JsonPropertyName("wrapped")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Wrapped { get; internal set; }

        /// <summary>Gets or sets the URL of the namespace definition. </summary>
        [JsonPropertyName("namespace")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Namespace { get; internal set; }

        /// <summary>Gets or sets the prefix for the name. </summary>
        [JsonPropertyName("prefix")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Prefix { get; internal set; }

        /// <summary>Gets or sets if the property definition translates into an attribute instead of an element. </summary>
        [JsonPropertyName("attribute")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Attribute { get; internal set; }
    }
}
