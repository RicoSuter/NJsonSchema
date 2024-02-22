//-----------------------------------------------------------------------
// <copyright file="PropertyRenameAndIgnoreSerializerContractResolver.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NJsonSchema.Infrastructure
{
    /// <summary>JsonConvert resolver that allows to ignore and rename properties for given types.</summary>
    public class PropertyRenameAndIgnoreSerializerContractResolver : DefaultContractResolver
    {
        private readonly Dictionary<string, HashSet<string>> _ignores;
        private readonly Dictionary<string, Dictionary<string, string>> _renames;

        /// <summary>Initializes a new instance of the <see cref="PropertyRenameAndIgnoreSerializerContractResolver"/> class.</summary>
        public PropertyRenameAndIgnoreSerializerContractResolver()
        {
            _ignores = new Dictionary<string, HashSet<string>>();
            _renames = new Dictionary<string, Dictionary<string, string>>();
        }

        /// <summary>Ignore the given property/properties of the given type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="jsonPropertyNames">One or more JSON properties to ignore.</param>
        public void IgnoreProperty(Type type, params string[] jsonPropertyNames)
        {
            if (!_ignores.TryGetValue(type.FullName!, out HashSet<string>? value))
            {
                value = new HashSet<string>();
                _ignores[type.FullName!] = value;
            }

            foreach (var prop in jsonPropertyNames)
            {
                value.Add(prop);
            }
        }

        /// <summary>Rename a property of the given type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="propertyName">The JSON property name to rename.</param>
        /// <param name="newJsonPropertyName">The new JSON property name.</param>
        public void RenameProperty(Type type, string propertyName, string newJsonPropertyName)
        {
            if (!_renames.TryGetValue(type.FullName!, out Dictionary<string, string>? value))
            {
                value = new Dictionary<string, string>();
                _renames[type.FullName!] = value;
            }

            value[propertyName] = newJsonPropertyName;
        }

        /// <summary>Creates a JsonProperty for the given System.Reflection.MemberInfo.</summary>
        /// <param name="member">The member's parent Newtonsoft.Json.MemberSerialization.</param>
        /// <param name="memberSerialization">The member to create a JsonProperty for.</param>
        /// <returns>A created JsonProperty for the given System.Reflection.MemberInfo.</returns>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property.DeclaringType != null && 
                property.PropertyName != null && 
                IsIgnored(property.DeclaringType, property.PropertyName))
            {
                property.Ignored = true;

                property.ShouldSerialize = i => false;
                property.ShouldDeserialize = i => false;
            }

            if (property.DeclaringType != null &&
                property.PropertyName != null &&
                IsRenamed(property.DeclaringType, property.PropertyName, out var newJsonPropertyName))
            {
                property.PropertyName = newJsonPropertyName;
            }

            return property;
        }

        private bool IsIgnored(Type type, string jsonPropertyName)
        {
            if (!_ignores.TryGetValue(type.FullName!, out HashSet<string>? value))
            {
                return false;
            }

            return value.Contains(jsonPropertyName);
        }

        private bool IsRenamed(Type type, string jsonPropertyName, out string? newJsonPropertyName)
        {
            if (!_renames.TryGetValue(type.FullName!, out var renames) || !renames.TryGetValue(jsonPropertyName, out newJsonPropertyName))
            {
                newJsonPropertyName = null;
                return false;
            }

            return true;
        }
    }
}