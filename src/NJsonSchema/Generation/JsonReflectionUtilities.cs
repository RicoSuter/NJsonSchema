//-----------------------------------------------------------------------
// <copyright file="JsonReflectionUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Generation
{
    /// <summary>Utility methods for reflection.</summary>
    public static class JsonReflectionUtilities
    {
        private static readonly Lazy<CamelCasePropertyNamesContractResolver> CamelCaseResolverLazy = new Lazy<CamelCasePropertyNamesContractResolver>();

        private static readonly Lazy<DefaultContractResolver> SnakeCaseResolverLazy = new Lazy<DefaultContractResolver>(() => new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() });

        /// <summary>Gets the name of the property for JSON serialization.</summary>
        /// <returns>The name.</returns>
        /// <exception cref="NotSupportedException">The PropertyNameHandling is not supported.</exception>
        public static string GetPropertyName(MemberInfo property, PropertyNameHandling propertyNameHandling)
        {
            var propertyName = ReflectionCache.GetPropertiesAndFields(property.DeclaringType)
                .First(p => p.MemberInfo.Name == property.Name).GetName();
            switch (propertyNameHandling)
            {
                case PropertyNameHandling.Default:
                    return propertyName;

                case PropertyNameHandling.CamelCase:
                    return CamelCaseResolverLazy.Value.GetResolvedPropertyName(propertyName);

                case PropertyNameHandling.SnakeCase:
                    return SnakeCaseResolverLazy.Value.GetResolvedPropertyName(propertyName);

                default:
                    throw new NotSupportedException($"The PropertyNameHandling '{propertyNameHandling}' is not supported.");
            }
        }

        /// <summary>Checks whether a type is nullable.</summary>
        /// <param name="type">The type.</param>
        /// <param name="parentAttributes">The parent attributes (e.g. property or parameter attributes).</param>
        /// <param name="settings">The settings</param>
        /// <returns>true if the type can be null.</returns>
        public static bool IsNullable(Type type, IEnumerable<Attribute> parentAttributes, JsonSchemaGeneratorSettings settings)
        {
            var isStruct = type.Name != "Nullable`1" && type.GetTypeInfo().IsValueType && !type.GetTypeInfo().IsPrimitive;
            var allowsNull = isStruct == false && settings.DefaultReferenceTypeNullHandling == ReferenceTypeNullHandling.Null;

            var jsonPropertyAttribute = parentAttributes?.OfType<JsonPropertyAttribute>().SingleOrDefault();
            if (jsonPropertyAttribute != null && jsonPropertyAttribute.Required == Required.DisallowNull)
                allowsNull = false;

            if (parentAttributes?.Any(a => a.GetType().Name == "NotNullAttribute") == true)
                allowsNull = false;

            if (parentAttributes?.Any(a => a.GetType().Name == "CanBeNullAttribute") == true)
                allowsNull = true;

            return allowsNull;
        }
    }
}
