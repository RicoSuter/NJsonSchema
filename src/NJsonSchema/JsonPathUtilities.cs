//-----------------------------------------------------------------------
// <copyright file="JsonPathUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Namotion.Reflection;
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    /// <summary>Utilities to work with JSON paths.</summary>
    public static class JsonPathUtilities
    {
        /// <summary>Gets the JSON path of the given object.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="searchedObject">The object to search.</param>
        /// <returns>The path or <c>null</c> when the object could not be found.</returns>
        /// <exception cref="InvalidOperationException">Could not find the JSON path of a child object.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="rootObject"/> is <see langword="null"/></exception>
        public static string? GetJsonPath(object rootObject, object searchedObject)
        {
            return GetJsonPaths(rootObject, [searchedObject])[searchedObject];
        }

        /// <summary>Gets the JSON path of the given object.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="searchedObjects">The objects to search.</param>
        /// <returns>The path or <c>null</c> when the object could not be found.</returns>
        /// <exception cref="InvalidOperationException">Could not find the JSON path of a child object.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="rootObject"/> is <see langword="null"/></exception>
        public static IReadOnlyDictionary<object, string?> GetJsonPaths(object rootObject,
            IEnumerable<object> searchedObjects)
        {
            if (rootObject == null)
            {
                throw new ArgumentNullException(nameof(rootObject));
            }

            var mappings = searchedObjects.ToDictionary(o => o, o => (string?)null);
            FindJsonPaths(rootObject, mappings, "#", []);

            if (mappings.Any(p => p.Value == null))
            {
                var errorItems = mappings.Where(p => p.Value == null).Select(x => x.Key.GetType().FullName);
                throw new InvalidOperationException("Could not find the JSON path of a referenced schema: " +
                                                     string.Join(",", errorItems) +
                                                    ". Manually referenced schemas must be added to the " +
                                                    "'Definitions' of a parent schema.");
            }

            return mappings;
        }

        private static bool FindJsonPaths(object obj, Dictionary<object, string?> searchedObjects,
            string basePath, HashSet<object> checkedObjects)
        {
            if (obj == null)
            {
                return false;
            }

            var type = obj.GetType();
            if (type == typeof(string)
                || type.IsPrimitive
                || type.IsEnum
                || type.IsValueType
                || checkedObjects.Contains(obj)
            )
            {
                // no need to inspect
                return false;
            }

            if (searchedObjects.ContainsKey(obj))
            {
                searchedObjects[obj] = basePath;
                if (searchedObjects.All(static p => p.Value != null))
                {
                    return true;
                }
            }

            checkedObjects.Add(obj);

            var pathAndSeparator = basePath + "/";
            if (obj is IDictionary dictionary)
            {
                foreach (DictionaryEntry pair in dictionary)
                {
                    if (pair.Value != null &&
                        FindJsonPaths(pair.Value, searchedObjects, pathAndSeparator + pair.Key, checkedObjects))
                    {
                        return true;
                    }
                }
            }
            else if (obj is IList list)
            {
                for (var i = 0; i < list.Count; ++i)
                {
                    var item = list[i];
                    if (item != null &&
                        FindJsonPaths(item, searchedObjects, pathAndSeparator + i, checkedObjects))
                    {
                        return true;
                    }
                }
            }
            else if (obj is IEnumerable enumerable)
            {
                var i = 0;
                foreach (var item in enumerable)
                {
                    if (item != null &&
                        FindJsonPaths(item, searchedObjects, pathAndSeparator + i, checkedObjects))
                    {
                        return true;
                    }
                    i++;
                }
            }
            else
            {
                var isExtensionObject = obj is IJsonExtensionObject;

                // Look up the converter once for this object's properties (not per-property)
                var converter = JsonSchemaSerialization.CurrentSerializerOptions?.Converters
                    .OfType<SchemaSerializationConverter>().FirstOrDefault();

                // Order properties so that settable properties (with a setter) are processed before
                // getter-only properties. This ensures that when two properties return the same object
                // (e.g., an alias like "Definitions1 => Definitions2"), the canonical settable property
                // is traversed first and its path is recorded, while the alias is naturally skipped
                // via the checkedObjects set.
                var properties = type.GetContextualProperties()
                    .OrderBy(static property => property.PropertyInfo.SetMethod != null ? 0 : 1);

                foreach (var property in properties)
                {
                    var jsonIgnoreAttr = property.MemberInfo.GetCustomAttribute<JsonIgnoreAttribute>();
                    if ((jsonIgnoreAttr != null && jsonIgnoreAttr.Condition == JsonIgnoreCondition.Always) ||
                        property.PropertyInfo.GetMethod?.IsStatic == true)
                    {
                        continue;
                    }

                    // Check if the property is ignored by the current SchemaSerializationConverter
                    // (e.g., "components" is ignored for Swagger2, "definitions" for OpenApi3)
                    var jsonName = property.MemberInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
                    if (converter != null && converter.IsPropertyIgnored(type, jsonName))
                    {
                        continue;
                    }

                    // Skip ExtensionData property — it's handled separately below with correct path resolution
                    if (isExtensionObject && property.MemberInfo.GetCustomAttribute<JsonExtensionDataAttribute>() != null)
                    {
                        continue;
                    }

                    var value = property.GetValue(obj);
                    if (value != null)
                    {
                        if (FindJsonPaths(value, searchedObjects, pathAndSeparator + jsonName, checkedObjects))
                        {
                            return true;
                        }
                    }
                }

                if (obj is IJsonExtensionObject)
                {
                    var extensionDataProperty = type.GetRuntimeProperty(nameof(IJsonExtensionObject.ExtensionData));
                    if (extensionDataProperty != null)
                    {
                        var value = extensionDataProperty.GetValue(obj);
                        if (value != null &&
                            FindJsonPaths(value, searchedObjects, basePath, checkedObjects))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
