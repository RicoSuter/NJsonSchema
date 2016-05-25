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
            public PropertyInfo PropertyInfo { get; private set; }
            public CustomAttributes CustomAttributes { get; private set; }


            public Property(PropertyInfo propertyInfo, CustomAttributes customAttributes)
            {
                this.PropertyInfo = propertyInfo;
                this.CustomAttributes = customAttributes;
            }

            /// <summary>Gets the name of the property for JSON serialization.</summary>
            /// <returns>The name.</returns>
            public string GetName()
            {
                if (this.CustomAttributes.JsonPropertyAttribute != null && !string.IsNullOrEmpty(this.CustomAttributes.JsonPropertyAttribute.PropertyName))
                    return this.CustomAttributes.JsonPropertyAttribute.PropertyName;

                if (this.CustomAttributes.DataContractAttribute != null)
                {
                    if (this.CustomAttributes.DataMemberAttribute != null && !string.IsNullOrEmpty(this.CustomAttributes.DataMemberAttribute.Name))
                        return this.CustomAttributes.DataMemberAttribute.Name;
                }

                return this.PropertyInfo.Name;
            }
        }

        public class CustomAttributes
        {
            public JsonIgnoreAttribute JsonIgnoreAttribute { get; private set; }
            public JsonPropertyAttribute JsonPropertyAttribute { get; private set; }
            public DataContractAttribute DataContractAttribute { get; private set; }
            public DataMemberAttribute DataMemberAttribute { get; private set; }

            public CustomAttributes(
                JsonIgnoreAttribute jsonIgnoreAttribute,
                JsonPropertyAttribute jsonPropertyAttribute,
                DataContractAttribute dataContractAttribute,
                DataMemberAttribute dataMemberAttribute)
            {
                this.JsonIgnoreAttribute = jsonIgnoreAttribute;
                this.JsonPropertyAttribute = jsonPropertyAttribute;
                this.DataContractAttribute = dataContractAttribute;
                this.DataMemberAttribute = dataMemberAttribute;
            }
        }
    }
}