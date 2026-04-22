//-----------------------------------------------------------------------
// <copyright file="JsonPropertyToken.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Text.Json.Nodes;

namespace NJsonSchema.Validation
{
    /// <summary>
    /// Represents a JSON property (name-value pair) for validation error reporting.
    /// Provides a <c>ToString()</c> format equivalent to Newtonsoft's <c>JProperty.ToString()</c>
    /// (e.g. <c>"foo": 5</c>), since System.Text.Json does not have a JProperty equivalent.
    /// </summary>
    internal sealed class JsonPropertyToken
    {
        private readonly string _propertyName;
        private readonly JsonNode? _value;

        public JsonPropertyToken(string propertyName, JsonNode? value)
        {
            _propertyName = propertyName;
            _value = value;
        }

        /// <summary>
        /// Returns the property in Newtonsoft JProperty format: <c>"name": value</c>.
        /// </summary>
        public override string ToString()
        {
            var valueString = _value?.ToJsonString() ?? "null";
            return $"\"{_propertyName}\": {valueString}";
        }
    }
}
