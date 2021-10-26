//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
using Newtonsoft.Json;
using System;
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
        public SystemTextJsonSchemaGeneratorSettings()
        {
            ReflectionService = new SystemTextJsonReflectionService();
        }

        /// <summary>Gets or sets the System.Text.Json serializer options.</summary>
        [JsonIgnore]
        public JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions();
    }
}