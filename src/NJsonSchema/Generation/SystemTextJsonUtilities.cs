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

            var jsonStringEnumConverter = ((IEnumerable) serializerOptions.Converters).OfType<object>()
                .FirstOrDefault(c => c.GetType().IsAssignableToTypeName("System.Text.Json.Serialization.JsonStringEnumConverter",
                    TypeNameStyle.FullName));

            if (jsonStringEnumConverter == null) 
                return settings;
            
            var camelCasePolicy = IsCamelCaseEnumNamingPolicy(jsonStringEnumConverter);
            settings.Converters.Add(new StringEnumConverter(camelCasePolicy));

            return settings;
        }

        private static bool IsCamelCaseEnumNamingPolicy(object jsonStringEnumConverter)
        {
            try
            {
                var enumNamingPolicy = jsonStringEnumConverter
                    .GetType().GetRuntimeFields()
                    .FirstOrDefault(x => x.FieldType.FullName == "System.Text.Json.JsonNamingPolicy")
                    ?.GetValue(jsonStringEnumConverter);

                return enumNamingPolicy != null 
                       && enumNamingPolicy.GetType().FullName == "System.Text.Json.JsonCamelCaseNamingPolicy";
            }
            catch
            {
                return false;
            }
        }

        private sealed class SystemTextJsonContractResolver : DefaultContractResolver
        {
            private readonly dynamic _serializerOptions;

            public SystemTextJsonContractResolver(dynamic serializerOptions)
            {
                _serializerOptions = serializerOptions;
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var attributes = member.GetCustomAttributes(true);

                var property = base.CreateProperty(member, memberSerialization);

                var propertyIgnored = false;
                var jsonIgnoreAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.Text.Json.Serialization.JsonIgnoreAttribute", TypeNameStyle.FullName);
                if (jsonIgnoreAttribute != null)
                {
                    var condition = jsonIgnoreAttribute.TryGetPropertyValue<object>("Condition");
                    if (condition is null || condition.ToString() == "Always")
                    {
                        propertyIgnored = true;
                    }
                }

                property.Ignored = propertyIgnored || attributes.FirstAssignableToTypeNameOrDefault("System.Text.Json.Serialization.JsonExtensionDataAttribute", TypeNameStyle.FullName) != null;

                if (_serializerOptions.PropertyNamingPolicy != null)
                {
                    property.PropertyName = _serializerOptions.PropertyNamingPolicy.ConvertName(member.Name);
                }

                dynamic jsonPropertyNameAttribute = attributes
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