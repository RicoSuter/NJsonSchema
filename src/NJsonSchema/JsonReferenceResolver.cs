//-----------------------------------------------------------------------
// <copyright file="JsonReferenceResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    /// <summary>Resolves JSON Pointer references.</summary>
    public class JsonReferenceResolver
    {
        private readonly Dictionary<string, JsonSchema4> _resolvedSchemas = new Dictionary<string, JsonSchema4>();

        /// <summary>Adds a document reference.</summary>
        /// <param name="documentPath">The document path.</param>
        /// <param name="schema">The referenced schema.</param>
        public void AddDocumentReference(string documentPath, JsonSchema4 schema)
        {
            _resolvedSchemas[documentPath] = schema;
        }
        
        /// <summary>Gets the object from the given JSON path.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="jsonPath">The JSON path.</param>
        /// <returns>The JSON Schema or <c>null</c> when the object could not be found.</returns>
        /// <exception cref="InvalidOperationException">Could not resolve the path.</exception>
        /// <exception cref="NotSupportedException">Could not resolve the path.</exception>
        public JsonSchema4 ResolveReference(object rootObject, string jsonPath)
        {
            if (jsonPath == "#")
            {
                if (rootObject is JsonSchema4)
                    return (JsonSchema4)rootObject;

                throw new InvalidOperationException("Could not resolve the path '#' because the root object is not a JsonSchema4.");
            }
            else if (jsonPath.StartsWith("#/"))
            {
                var allSegments = jsonPath.Split('/').Skip(1).ToList();
                var schema = ResolveReference(rootObject, allSegments, allSegments, new HashSet<object>());
                if (schema == null)
                    throw new InvalidOperationException("Could not resolve the path '" + jsonPath + "'.");

                return schema;
            }
            else if (jsonPath.StartsWith("http://") || jsonPath.StartsWith("https://"))
                return ResolveUrlReference(jsonPath, jsonPath);
            else
            {
                var documentPathProvider = rootObject as IDocumentPathProvider;

                var documentPath = documentPathProvider?.DocumentPath;
                if (documentPath != null)
                {
                    if (documentPath.StartsWith("http://") || documentPath.StartsWith("https://"))
                    {
                        var url = new Uri(new Uri(documentPath), jsonPath).ToString();
                        return ResolveUrlReference(url, jsonPath);
                    }
                    else
                    {
                        var filePath = DynamicApis.PathCombine(DynamicApis.PathGetDirectoryName(documentPath), jsonPath);
                        return ResolveFileReference(filePath, jsonPath);
                    }
                }
                else
                    throw new NotSupportedException("Could not resolve the path '" + jsonPath +
                        "' because no document path is available.");
            }
        }

        private JsonSchema4 ResolveReference(object obj, List<string> segments, List<string> allSegments, HashSet<object> checkedObjects)
        {
            if (obj == null || obj is string || checkedObjects.Contains(obj))
                return null;

            if (segments.Count == 0)
                return obj as JsonSchema4;

            checkedObjects.Add(obj);
            var firstSegment = segments[0];

            if (obj is IDictionary)
            {
                if (((IDictionary)obj).Contains(firstSegment))
                    return ResolveReference(((IDictionary)obj)[firstSegment], segments.Skip(1).ToList(), allSegments, checkedObjects);
            }
            else if (obj is IEnumerable)
            {
                int index;
                if (int.TryParse(firstSegment, out index))
                {
                    var enumerable = ((IEnumerable)obj).Cast<object>().ToArray();
                    if (enumerable.Length > index)
                        return ResolveReference(enumerable[index], segments.Skip(1).ToList(), allSegments, checkedObjects);
                }
            }
            else
            {
                foreach (var member in ReflectionCache.GetPropertiesAndFields(obj.GetType()).Where(p => p.CustomAttributes.JsonIgnoreAttribute == null))
                {
                    var pathSegment = member.GetName();
                    if (pathSegment == firstSegment)
                    {
                        var value = member.GetValue(obj);
                        return ResolveReference(value, segments.Skip(1).ToList(), allSegments, checkedObjects);
                    }
                }
            }

            return null;
        }

        private JsonSchema4 ResolveFileReference(string url, string jsonPath)
        {
            if (DynamicApis.SupportsFileApis)
            {
                try
                {
                    var arr = Regex.Split(url, @"(?=#)");

                    if (!_resolvedSchemas.ContainsKey(arr[0]))
                        JsonSchema4.FromFile(arr[0], this);

                    var result = _resolvedSchemas[arr[0]];
                    return arr.Length == 1 ? result : ResolveReference(result, arr[1]);
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

        private JsonSchema4 ResolveUrlReference(string filePath, string jsonPath)
        {
            if (DynamicApis.SupportsWebClientApis)
            {
                try
                {
                    var arr = filePath.Split('#');

                    if (!_resolvedSchemas.ContainsKey(arr[0]))
                        JsonSchema4.FromUrl(arr[0], this);

                    var result = _resolvedSchemas[arr[0]];
                    return arr.Length == 1 ? result : ResolveReference(result, "#" + arr[1]);
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
    }
}