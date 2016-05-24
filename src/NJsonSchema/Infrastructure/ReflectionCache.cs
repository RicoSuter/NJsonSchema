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
        private static readonly Dictionary<Type, IList<PropertyInfo>> PropertyCacheByType = new Dictionary<Type, IList<PropertyInfo>>();
        private static readonly Dictionary<PropertyInfo, CustomAttributes> AttributeCacheByProperty = new Dictionary<PropertyInfo, CustomAttributes>();
        private static readonly Dictionary<Type, DataContractAttribute> DataContractAttributeCacheByType = new Dictionary<Type, DataContractAttribute>();

        public static IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            IList<PropertyInfo> properties;

            if (PropertyCacheByType.ContainsKey(type))
                properties = PropertyCacheByType[type];
            else
            {
                properties = type.GetRuntimeProperties().ToList();
                PropertyCacheByType[type] = properties;
            }

            return properties;
        }

        public static CustomAttributes GetCustomAttributes(PropertyInfo property)
        {
            if (AttributeCacheByProperty.ContainsKey(property))
                return AttributeCacheByProperty[property];

            CustomAttributes customAttributes = new CustomAttributes();

            foreach (var attribute in property.GetCustomAttributes())
            {
                if (attribute is JsonIgnoreAttribute)
                    customAttributes.JsonIgnoreAttribute = attribute as JsonIgnoreAttribute;
                else if (attribute is JsonPropertyAttribute)
                    customAttributes.JsonPropertyAttribute = attribute as JsonPropertyAttribute;
                else if (attribute is DataMemberAttribute)
                    customAttributes.DataMemberAttribute = attribute as DataMemberAttribute;
            }

            customAttributes.DataContractAttribute = GetDataContractAttribute(property.DeclaringType);

            AttributeCacheByProperty[property] = customAttributes;

            return customAttributes;
        }

        public static DataContractAttribute GetDataContractAttribute(Type type)
        {
            if (DataContractAttributeCacheByType.ContainsKey(type))
                return DataContractAttributeCacheByType[type];

            var attribute = type.GetTypeInfo().GetCustomAttribute<DataContractAttribute>();
            DataContractAttributeCacheByType[type] = attribute;

            return attribute;
        }

        public class CustomAttributes
        {
            public JsonIgnoreAttribute JsonIgnoreAttribute { get; set; }
            public JsonPropertyAttribute JsonPropertyAttribute { get; set; }
            public DataContractAttribute DataContractAttribute { get; set; }
            public DataMemberAttribute DataMemberAttribute { get; set; }
        }
    }
}