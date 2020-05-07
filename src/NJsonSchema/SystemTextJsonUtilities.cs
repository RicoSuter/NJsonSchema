//-----------------------------------------------------------------------
// <copyright file="SystemTextJsonUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

#if NETSTANDARD2_0

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Text.Json;

namespace NJsonSchema
{
    /// <summary>
    /// Utility methods for dealing with System.Text.Json.
    /// </summary>
    public static class SystemTextJsonUtilities
    {
        /// <summary>
        /// Converst System.Text.Json serializer options to Newtonsoft JSON settings.
        /// </summary>
        /// <param name="SerializerOptions">The options.</param>
        /// <returns>The settings.</returns>
        public static JsonSerializerSettings ConvertJsonOptionsToNewtonsoftSettings(JsonSerializerOptions SerializerOptions)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            if (SerializerOptions.Converters.Any(c => c is System.Text.Json.Serialization.JsonStringEnumConverter))
            {
                settings.Converters.Add(new StringEnumConverter());
            }

            if (SerializerOptions.PropertyNamingPolicy == null)
            {
                settings.ContractResolver = new DefaultContractResolver();
            }

            return settings;
        }
    }
}

#endif