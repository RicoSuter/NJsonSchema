//-----------------------------------------------------------------------
// <copyright file="ReflectionCache.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace NJsonSchema.Infrastructure
{
    internal static class ReflectionCache
    {
        private static readonly Dictionary<Type, IList<Property>> PropertyCacheByType = new Dictionary<Type, IList<Property>>();

        private static readonly Dictionary<Type, DataContractAttribute> DataContractAttributeCacheByType = new Dictionary<Type, DataContractAttribute>();

        public static IEnumerable<Property> GetProperties(Type type)
        {
            lock (PropertyCacheByType)
            {
                if (!PropertyCacheByType.ContainsKey(type))
                {
                    var properties = type.GetRuntimeProperties().Select(p => new Property(p, GetCustomAttributes(p))).ToList();

                    PropertyCacheByType[type] = properties;
                }

                return PropertyCacheByType[type];
            }
        }

        private static CustomAttributes GetCustomAttributes(PropertyInfo property)
        {
            JsonIgnoreAttribute jsonIgnoreAttribute = null;
            JsonPropertyAttribute jsonPropertyAttribute = null;
            DataMemberAttribute dataMemberAttribute = null;

            foreach (var attribute in property.GetCustomAttributes())
            {
                if (attribute is JsonIgnoreAttribute)
                    jsonIgnoreAttribute = attribute as JsonIgnoreAttribute;
                else if (attribute is JsonPropertyAttribute)
                    jsonPropertyAttribute = attribute as JsonPropertyAttribute;
                else if (attribute is DataMemberAttribute)
                    dataMemberAttribute = attribute as DataMemberAttribute;
            }

            return new CustomAttributes(jsonIgnoreAttribute, jsonPropertyAttribute, GetDataContractAttribute(property.DeclaringType), dataMemberAttribute);
        }

        public static DataContractAttribute GetDataContractAttribute(Type type)
        {
            lock (DataContractAttributeCacheByType)
            {
                if (!DataContractAttributeCacheByType.ContainsKey(type))
                {
                    var attribute = type.GetTypeInfo().GetCustomAttribute<DataContractAttribute>();
                    DataContractAttributeCacheByType[type] = attribute;
                }

                return DataContractAttributeCacheByType[type];
            }
        }

        public class Property
        {
            public Property(PropertyInfo propertyInfo, CustomAttributes customAttributes)
            {
                PropertyInfo = propertyInfo;
                CustomAttributes = customAttributes;
            }

            public PropertyInfo PropertyInfo { get; }

            public CustomAttributes CustomAttributes { get; }

            /// <summary>Gets the name of the property for JSON serialization.</summary>
            /// <returns>The name.</returns>
            public string GetName()
            {
                if (CustomAttributes.JsonPropertyAttribute != null && !string.IsNullOrEmpty(CustomAttributes.JsonPropertyAttribute.PropertyName))
                    return CustomAttributes.JsonPropertyAttribute.PropertyName;

                if (CustomAttributes.DataContractAttribute != null)
                {
                    if (CustomAttributes.DataMemberAttribute != null && !string.IsNullOrEmpty(CustomAttributes.DataMemberAttribute.Name))
                        return CustomAttributes.DataMemberAttribute.Name;
                }

                return PropertyInfo.Name;
            }
        }

        public class CustomAttributes
        {
            public CustomAttributes(
                JsonIgnoreAttribute jsonIgnoreAttribute,
                JsonPropertyAttribute jsonPropertyAttribute,
                DataContractAttribute dataContractAttribute,
                DataMemberAttribute dataMemberAttribute)
            {
                JsonIgnoreAttribute = jsonIgnoreAttribute;
                JsonPropertyAttribute = jsonPropertyAttribute;
                DataContractAttribute = dataContractAttribute;
                DataMemberAttribute = dataMemberAttribute;
            }

            public JsonIgnoreAttribute JsonIgnoreAttribute { get; }

            public JsonPropertyAttribute JsonPropertyAttribute { get; }

            public DataContractAttribute DataContractAttribute { get; }

            public DataMemberAttribute DataMemberAttribute { get; }
        }
    }
}