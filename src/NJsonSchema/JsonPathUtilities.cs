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
using System.Reflection;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    /// <summary>Utilities to work with JSON paths.</summary>
    public static class JsonPathUtilities
    {
        /// <summary>Gets the JSON path of the given object.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="objectToSearch">The object to search.</param>
        /// <param name="schemaDefinitionAppender">Appends the <paramref name="objectToSearch"/> to the 'definitions' if it could not be found.</param>
        /// <returns>The path or <c>null</c> when the object could not be found.</returns>
        /// <exception cref="InvalidOperationException">Could not find the JSON path of a child object.</exception>
        public static string GetJsonPath(object root, object objectToSearch, ISchemaDefinitionAppender schemaDefinitionAppender = null)
        {
            var path = GetJsonPath(root, objectToSearch, "#", new HashSet<object>());
            if (path == null)
            {
                if (schemaDefinitionAppender != null && objectToSearch is JsonSchema4)
                {
                    schemaDefinitionAppender.Append(root, (JsonSchema4)objectToSearch);
                    return GetJsonPath(root, objectToSearch, schemaDefinitionAppender);
                }
                else
                    throw new InvalidOperationException("Could not find the JSON path of a child object.");

            }
            return path;
        }

        /// <summary>Gets the object from the given JSON path.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="path">The JSON path.</param>
        /// <returns>The object or <c>null</c> when the object could not be found.</returns>
        /// <exception cref="InvalidOperationException">Could not resolve the path.</exception>
        /// <exception cref="NotSupportedException">Could not resolve the path.</exception>
        public static JsonSchema4 GetObjectFromJsonPath(object root, string path)
        {
            if (path == "#")
            {
                if (root is JsonSchema4)
                    return (JsonSchema4)root;

                throw new InvalidOperationException("Could not resolve the path '#' because the root object is not a JsonSchema4.");
            }
            else if (path.StartsWith("#/"))
            {
                var allSegments = path.Split('/').Skip(1).ToList();
                var schema = GetObjectFromJsonPath(root, allSegments, allSegments, new HashSet<object>());
                if (schema == null)
                    throw new InvalidOperationException("Could not resolve the path '" + path + "'.");

                return schema;
            }
            else if (path.StartsWith("http://") || path.StartsWith("https://"))
                return GetObjectFromJsonPathInUrl(path, path);
            else
            {
                var documentPathProvider = root as IDocumentPathProvider;

                var documentPath = documentPathProvider?.DocumentPath;
                if (documentPath != null)
                {
                    if (documentPath.StartsWith("http://") || documentPath.StartsWith("https://"))
                    {
                        var url = new Uri(new Uri(documentPath), path).ToString();
                        return GetObjectFromJsonPathInUrl(url, path);
                    }
                    else
                    {
                        var filePath = DynamicApis.PathCombine(DynamicApis.PathGetDirectoryName(documentPath), path);
                        return GetObjectFromJsonPathInFile(filePath, path);
                    }
                }
                else
                    throw new NotSupportedException("Could not resolve the path '" + path +
                        "' because no document path is available.");
            }
        }

        private static JsonSchema4 GetObjectFromJsonPathInFile(string url, string jsonPath)
        {
            if (DynamicApis.SupportsFileApis)
            {
                try
                {
                    var arr = url.Split('#');
                    var result = JsonSchema4.FromFile(arr[0]);
                    return arr.Length == 1 ? result : GetObjectFromJsonPath(result, arr[1]);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException("Could not resolve the path '" + jsonPath + "' with the file path '" + url + "': " + exception.Message, exception);
                }
            }
            else
                throw new NotSupportedException("Could not resolve the path '" + jsonPath +
                    "' because JSON file references are not supported on this platform.");
        }

        private static JsonSchema4 GetObjectFromJsonPathInUrl(string filePath, string jsonPath)
        {
            if (DynamicApis.SupportsWebClientApis)
            {
                try
                {
                    var arr = filePath.Split('#');
                    var result = JsonSchema4.FromUrl(arr[0]);
                    return arr.Length == 1 ? result : GetObjectFromJsonPath(result, arr[1]);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException("Could not resolve the path '" + jsonPath + "' with the URL '" + filePath + "': " + exception.Message, exception);
                }
            }
            else
                throw new NotSupportedException("Could not resolve the path '" + jsonPath +
                    "' because JSON web references are not supported on this platform.");
        }

        private static readonly Lazy<CamelCasePropertyNamesContractResolver> CamelCaseResolverLazy = new Lazy<CamelCasePropertyNamesContractResolver>();

        /// <summary>Gets the name of the property for JSON serialization.</summary>
        /// <returns>The name.</returns>
        /// <exception cref="NotSupportedException">The PropertyNameHandling is not supported.</exception>
        public static string GetPropertyName(PropertyInfo property, PropertyNameHandling propertyNameHandling)
        {
            switch (propertyNameHandling)
            {
                case PropertyNameHandling.Default:
                    return ReflectionCache.GetProperties(property.DeclaringType).First(p => p.PropertyInfo.Name == property.Name).GetName();

                case PropertyNameHandling.CamelCase:
                    return CamelCaseResolverLazy.Value.GetResolvedPropertyName(property.Name);

                default:
                    throw new NotSupportedException($"The PropertyNameHandling '{propertyNameHandling}' is not supported.");
            }
        }

        private static string GetJsonPath(object obj, object objectToSearch, string basePath, HashSet<object> checkedObjects)
        {
            if (obj == null || obj is string || checkedObjects.Contains(obj))
                return null;

            if (obj == objectToSearch)
                return basePath;

            checkedObjects.Add(obj);

            if (obj is IDictionary)
            {
                foreach (var key in ((IDictionary)obj).Keys)
                {
                    var path = GetJsonPath(((IDictionary)obj)[key], objectToSearch, basePath + "/" + key, checkedObjects);
                    if (path != null)
                        return path;
                }
            }
            else if (obj is IEnumerable)
            {
                var i = 0;
                foreach (var item in (IEnumerable)obj)
                {
                    var path = GetJsonPath(item, objectToSearch, basePath + "/" + i, checkedObjects);
                    if (path != null)
                        return path;
                    i++;
                }
            }
            else
            {
                foreach (var property in ReflectionCache.GetProperties(obj.GetType()).Where(p => p.CustomAttributes.JsonIgnoreAttribute == null))
                {
                    var value = property.PropertyInfo.GetValue(obj);
                    if (value != null)
                    {
                        var pathSegment = property.GetName();
                        var path = GetJsonPath(value, objectToSearch, basePath + "/" + pathSegment, checkedObjects);
                        if (path != null)
                            return path;
                    }
                }
            }

            return null;
        }

        private static JsonSchema4 GetObjectFromJsonPath(object obj, List<string> segments, List<string> allSegments, HashSet<object> checkedObjects)
        {
            if (obj == null || obj is string || checkedObjects.Contains(obj))
                return null;

            if (segments.Count == 0)
            {
                var jsonSchema = obj as JsonSchema4;
                if (jsonSchema != null && jsonSchema.TypeNameRaw == null)
                {
                    var referencesSchemaInDefinitionsList = allSegments.Count >= 2 && allSegments.ElementAt(allSegments.Count - 2) == "definitions";
                    if (referencesSchemaInDefinitionsList)
                        jsonSchema.TypeNameRaw = allSegments.Last();
                }

                return jsonSchema;
            }

            checkedObjects.Add(obj);
            var firstSegment = segments[0];

            if (obj is IDictionary)
            {
                if (((IDictionary)obj).Contains(firstSegment))
                    return GetObjectFromJsonPath(((IDictionary)obj)[firstSegment], segments.Skip(1).ToList(), allSegments, checkedObjects);
            }
            else if (obj is IEnumerable)
            {
                int index;
                if (int.TryParse(firstSegment, out index))
                {
                    var enumerable = ((IEnumerable)obj).Cast<object>().ToArray();
                    if (enumerable.Length > index)
                        return GetObjectFromJsonPath(enumerable[index], segments.Skip(1).ToList(), allSegments, checkedObjects);
                }
            }
            else
            {
                foreach (var property in ReflectionCache.GetProperties(obj.GetType()).Where(p => p.CustomAttributes.JsonIgnoreAttribute == null))
                {
                    var pathSegment = property.GetName();
                    if (pathSegment == firstSegment)
                    {
                        var value = property.PropertyInfo.GetValue(obj);
                        return GetObjectFromJsonPath(value, segments.Skip(1).ToList(), allSegments, checkedObjects);
                    }
                }
            }

            return null;
        }
    }
}