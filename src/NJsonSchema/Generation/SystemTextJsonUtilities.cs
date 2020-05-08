//-----------------------------------------------------------------------
// <copyright file="SystemTextJsonUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.Linq;

namespace NJsonSchema.Generation
{
    /// <summary>
    /// Utility methods for dealing with System.Text.Json.
    /// </summary>
    public static class SystemTextJsonUtilities
    {
        /// <summary>
        /// Converst System.Text.Json serializer options to Newtonsoft JSON settings.
        /// </summary>
        /// <param name="serializerOptions">The options.</param>
        /// <returns>The settings.</returns>
        public static JsonSerializerSettings ConvertJsonOptionsToNewtonsoftSettings(dynamic serializerOptions)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            if (((IEnumerable)serializerOptions.Converters).OfType<object>().Any(c =>
                c.GetType().IsAssignableToTypeName("System.Text.Json.Serialization.JsonStringEnumConverter", TypeNameStyle.FullName)))
            {
                settings.Converters.Add(new StringEnumConverter());
            }

            if (serializerOptions.PropertyNamingPolicy == null)
            {
                settings.ContractResolver = new DefaultContractResolver();
            }

            return settings;
        }
    }
}