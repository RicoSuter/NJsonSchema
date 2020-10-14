//-----------------------------------------------------------------------
// <copyright file="JsonPathUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Namotion.Reflection;
using Newtonsoft.Json.Serialization;

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
            {
                throw new ArgumentNullException(nameof(rootObject));
            }

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
            {
                return false;
            }

            if (searchedObjects.ContainsKey(obj))
            {
                searchedObjects[obj] = basePath;
                if (searchedObjects.All(p => p.Value != null))
                {
                    return true;
                }
            }

            checkedObjects.Add(obj);

            if (obj is IDictionary)
            {
                foreach (var key in ((IDictionary)obj).Keys)
                {
                    if (FindJsonPaths(((IDictionary)obj)[key], searchedObjects, basePath + "/" + key, checkedObjects, contractResolver))
                    {
                        return true;
                    }
                }
            }
            else if (obj is IEnumerable)
            {
                var i = 0;
                foreach (var item in (IEnumerable)obj)
                {
                    if (FindJsonPaths(item, searchedObjects, basePath + "/" + i, checkedObjects, contractResolver))
                    {
                        return true;
                    }

                    i++;
                }
            }
            else
            {
                var type = obj.GetType();
                var contract = contractResolver.ResolveContract(type) as JsonObjectContract;
                if (contract != null)
                {
                    foreach (var jsonProperty in contract.Properties.Where(p => !p.Ignored))
                    {
                        var value = jsonProperty.ValueProvider.GetValue(obj);
                        if (value != null)
                        {
                            if (FindJsonPaths(value, searchedObjects, basePath + "/" + jsonProperty.PropertyName, checkedObjects, contractResolver))
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
                            if (FindJsonPaths(value, searchedObjects, basePath, checkedObjects, contractResolver))
                            {
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