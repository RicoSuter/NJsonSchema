//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json;
using System.Text.Json;

namespace NJsonSchema.Generation
{
    /// <summary>
    /// 
    /// </summary>
    public class SystemTextJsonSchemaGeneratorSettings : JsonSchemaGeneratorSettings
    {
        /// <summary>
        /// 
        /// </summary>
        public SystemTextJsonSchemaGeneratorSettings() : base(new SystemTextJsonReflectionService())
        {
        }

        /// <summary>Gets or sets the System.Text.Json serializer options.</summary>
        [JsonIgnore]
        public JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions();
    }
}