//-----------------------------------------------------------------------
// <copyright file="IgnoreEmptyCollectionsContractResolver.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NJsonSchema.Infrastructure
{
    internal sealed class IgnoreEmptyCollectionsContractResolver : PropertyRenameAndIgnoreSerializerContractResolver
    {
        private static readonly Type enumerableType = typeof(IEnumerable);

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property.Required is Required.Default or Required.DisallowNull &&
                property.PropertyType is { IsPrimitive: false } &&
                property.PropertyType != typeof(string) &&
                enumerableType.IsAssignableFrom(property.PropertyType))
            {
                property.ShouldSerialize = instance =>
                {
                    var value = instance != null ? property.ValueProvider?.GetValue(instance) : null;
                    if (value is ICollection collection)
                    {
                        return collection.Count > 0;
                    }
                    if (value is IEnumerable enumerable)
                    {
                        return enumerable.GetEnumerator().MoveNext();
                    }

                    return true;
                };
            }

            return property;
        }
    }
}