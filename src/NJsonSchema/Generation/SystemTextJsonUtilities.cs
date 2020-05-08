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
using System.Reflection;

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
                ContractResolver = new SystemTextJsonContractResolver(serializerOptions)
            };

            if (((IEnumerable)serializerOptions.Converters).OfType<object>().Any(c =>
                c.GetType().IsAssignableToTypeName("System.Text.Json.Serialization.JsonStringEnumConverter", TypeNameStyle.FullName)))
            {
                settings.Converters.Add(new StringEnumConverter());
            }

            return settings;
        }

        internal class SystemTextJsonContractResolver : DefaultContractResolver
        {
            private readonly dynamic _serializerOptions;

            public SystemTextJsonContractResolver(dynamic serializerOptions)
            {
                _serializerOptions = serializerOptions;
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);

                if (_serializerOptions.PropertyNamingPolicy != null)
                {
                    property.PropertyName = _serializerOptions.PropertyNamingPolicy.ConvertName(member.Name);
                }

                dynamic jsonPropertyNameAttribute = member.GetCustomAttributes(true)
                    .FirstAssignableToTypeNameOrDefault("System.Text.Json.Serialization.JsonPropertyNameAttribute", TypeNameStyle.FullName);
                
                if (jsonPropertyNameAttribute != null && !string.IsNullOrEmpty(jsonPropertyNameAttribute.Name))
                {
                    property.PropertyName = jsonPropertyNameAttribute.Name;
                }

                return property;
            }
        }
    }
}