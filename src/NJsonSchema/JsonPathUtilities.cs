//-----------------------------------------------------------------------
// <copyright file="JsonPathUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    /// <summary>Utilities to work with JSON paths.</summary>
    public static class JsonPathUtilities
    {
        internal const string ReferenceReplaceString = "__referencePath";

        /// <summary>Gets the JSON path of the given object.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="searchedObject">The object to search.</param>
        /// <returns>The path or <c>null</c> when the object could not be found.</returns>
        /// <exception cref="InvalidOperationException">Could not find the JSON path of a child object.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="rootObject"/> is <see langword="null"/></exception>
        public static string GetJsonPath(object rootObject, object searchedObject)
        {
            // TODO: Remove this overload?
            return GetJsonPath(rootObject, searchedObject, new DefaultContractResolver());
        }

        /// <summary>Gets the JSON path of the given object.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="searchedObject">The object to search.</param>
        /// <param name="contractResolver">The contract resolver.</param>
        /// <returns>The path or <c>null</c> when the object could not be found.</returns>
        /// <exception cref="InvalidOperationException">Could not find the JSON path of a child object.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="rootObject"/> is <see langword="null"/></exception>
        public static string GetJsonPath(object rootObject, object searchedObject, IContractResolver contractResolver)
        {
            return GetJsonPaths(rootObject, new List<object> { searchedObject }, contractResolver)[searchedObject];
        }

        /// <summary>Gets the JSON path of the given object.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="searchedObjects">The objects to search.</param>
        /// <param name="contractResolver">The contract resolver.</param>
        /// <returns>The path or <c>null</c> when the object could not be found.</returns>
        /// <exception cref="InvalidOperationException">Could not find the JSON path of a child object.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="rootObject"/> is <see langword="null"/></exception>
#if !LEGACY
        public static IReadOnlyDictionary<object, string> GetJsonPaths(object rootObject,
            IEnumerable<object> searchedObjects, IContractResolver contractResolver)
#else
        public static IDictionary<object, string> GetJsonPaths(object rootObject, 
            IEnumerable<object> searchedObjects, IContractResolver contractResolver)
#endif
        {
            if (rootObject == null)
                throw new ArgumentNullException(nameof(rootObject));

            var mappings = searchedObjects.ToDictionary(o => o, o => (string)null);
            FindJsonPaths(rootObject, mappings, "#", new HashSet<object>(), contractResolver);

            if (mappings.Any(p => p.Value == null))
            {
                throw new InvalidOperationException("Could not find the JSON path of a referenced schema: " +
                                                    "Manually referenced schemas must be added to the " +
                                                    "'Definitions' of a parent schema.");
            }

            return mappings;
        }

        private static bool FindJsonPaths(object obj, Dictionary<object, string> searchedObjects,
            string basePath, HashSet<object> checkedObjects, IContractResolver contractResolver)
        {
            if (obj == null || obj is string || checkedObjects.Contains(obj))
                return false;

            if (searchedObjects.ContainsKey(obj))
            {
                searchedObjects[obj] = basePath;
                if (searchedObjects.All(p => p.Value != null))
                    return true;
            }

            checkedObjects.Add(obj);

            if (obj is IDictionary)
            {
                foreach (var key in ((IDictionary)obj).Keys)
                {
                    if (FindJsonPaths(((IDictionary)obj)[key], searchedObjects, basePath + "/" + key, checkedObjects, contractResolver))
                        return true;
                }
            }
            else if (obj is IEnumerable)
            {
                var i = 0;
                foreach (var item in (IEnumerable)obj)
                {
                    if (FindJsonPaths(item, searchedObjects, basePath + "/" + i, checkedObjects, contractResolver))
                        return true;

                    i++;
                }
            }
            else
            {
                var contract = contractResolver.ResolveContract(obj.GetType()) as JsonObjectContract;
                var ignoredProperties = contract?.Properties
                    .Where(p => p.Ignored || p.ShouldSerialize?.Invoke(obj) == false).ToArray() ??
                    new Newtonsoft.Json.Serialization.JsonProperty[0];

                foreach (var member in ReflectionCache.GetPropertiesAndFields(obj.GetType())
                    .Where(p => p.CustomAttributes.JsonIgnoreAttribute == null))
                {
                    var propertyName = member.GetName();

                    var isExtensionDataProperty = obj is IJsonExtensionObject && propertyName == nameof(IJsonExtensionObject.ExtensionData);
                    if (isExtensionDataProperty || ignoredProperties.All(p2 => p2.UnderlyingName != member.MemberInfo.Name))
                    {
                        var value = member.GetValue(obj);
                        if (value != null)
                        {
                            if (isExtensionDataProperty)
                            {
                                if (FindJsonPaths(value, searchedObjects, basePath, checkedObjects, contractResolver))
                                    return true;
                            }
                            else
                            {
                                if (FindJsonPaths(value, searchedObjects, basePath + "/" + propertyName, checkedObjects, contractResolver))
                                    return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}