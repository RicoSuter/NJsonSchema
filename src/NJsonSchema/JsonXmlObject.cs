//-----------------------------------------------------------------------
// <copyright file="JsonProperty.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json;

namespace NJsonSchema
{
    /// <summary>A description of a JSON property of a JSON object (used in Swagger specifications). </summary>
    public class JsonXmlObject
    {
        /// <summary>Gets the parent schema of the XML object schema. </summary>
        [JsonIgnore]
        public JsonSchema4 ParentSchema { get; internal set; }

        /// <summary>Gets or sets the name of the xml object. </summary>
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Name { get; internal set; }

        /// <summary>Gets or sets if the array elements are going to be wrapped or not. </summary>
        [JsonProperty("wrapped", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool Wrapped { get; internal set; }

        /// <summary>Gets or sets the URL of the namespace definition. </summary>
        [JsonProperty("namespace", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Namespace { get; internal set; }

        /// <summary>Gets or sets the prefix for the name. </summary>
        [JsonProperty("prefix", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Prefix { get; internal set; }

        /// <summary>Gets or sets if the property definition translates into an attribute instead of an element. </summary>
        [JsonProperty("attribute", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool Attribute { get; internal set; }
    }
}