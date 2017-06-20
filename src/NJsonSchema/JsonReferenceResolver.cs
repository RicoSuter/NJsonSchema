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
using System.Threading.Tasks;
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    /// <summary>Resolves JSON Pointer references.</summary>
    public class JsonReferenceResolver
    {
        private readonly JsonSchemaResolver _schemaResolver;
        private readonly Dictionary<string, JsonSchema4> _resolvedSchemas = new Dictionary<string, JsonSchema4>();

        /// <summary>Initializes a new instance of the <see cref="JsonReferenceResolver"/> class.</summary>
        /// <param name="schemaResolver">The schema resolver.</param>
        public JsonReferenceResolver(JsonSchemaResolver schemaResolver)
        {
            _schemaResolver = schemaResolver;
        }

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
        /// <exception cref="InvalidOperationException">Could not resolve the JSON path.</exception>
        /// <exception cref="NotSupportedException">Could not resolve the JSON path.</exception>
        public async Task<JsonSchema4> ResolveReferenceAsync(object rootObject, string jsonPath)
        {
            if (jsonPath == "#")
            {
                if (rootObject is JsonSchema4)
                    return (JsonSchema4)rootObject;

                throw new InvalidOperationException("Could not resolve the JSON path '#' because the root object is not a JsonSchema4.");
            }
            else if (jsonPath.StartsWith("#/"))
            {
                return ResolveDocumentReference(rootObject, jsonPath);
            }
            else if (jsonPath.StartsWith("http://") || jsonPath.StartsWith("https://"))
                return await ResolveUrlReferenceWithAlreadyResolvedCheckAsync(jsonPath, jsonPath).ConfigureAwait(false);
            else
            {
                var documentPathProvider = rootObject as IDocumentPathProvider;

                var documentPath = documentPathProvider?.DocumentPath;
                if (documentPath != null)
                {
                    if (documentPath.StartsWith("http://") || documentPath.StartsWith("https://"))
                    {
                        var url = new Uri(new Uri(documentPath), jsonPath).ToString();
                        return await ResolveUrlReferenceWithAlreadyResolvedCheckAsync(url, jsonPath).ConfigureAwait(false);
                    }
                    else
                    {
                        var filePath = DynamicApis.PathCombine(DynamicApis.PathGetDirectoryName(documentPath), jsonPath);
                        return await ResolveFileReferenceWithAlreadyResolvedCheckAsync(filePath, jsonPath).ConfigureAwait(false);
                    }
                }
                else
                    throw new NotSupportedException("Could not resolve the JSON path '" + jsonPath + "' because no document path is available.");
            }
        }

        /// <summary>Resolves a document reference.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="jsonPath">The JSON path to resolve.</param>
        /// <returns>The resolved JSON Schema.</returns>
        /// <exception cref="InvalidOperationException">Could not resolve the JSON path.</exception>
        public virtual JsonSchema4 ResolveDocumentReference(object rootObject, string jsonPath)
        {
            var allSegments = jsonPath.Split('/').Skip(1).ToList();
            var schema = ResolveDocumentReference(rootObject, allSegments, new HashSet<object>());
            if (schema == null)
                throw new InvalidOperationException("Could not resolve the path '" + jsonPath + "'.");
            return schema;
        }

        /// <summary>Resolves a file reference.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The resolved JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The System.IO.File API is not available on this platform.</exception>
        public virtual async Task<JsonSchema4> ResolveFileReferenceAsync(string filePath)
        {
            return await JsonSchema4.FromFileAsync(filePath, schema => this).ConfigureAwait(false);
        }

        /// <summary>Resolves an URL reference.</summary>
        /// <param name="url">The URL.</param>
        /// <exception cref="NotSupportedException">The HttpClient.GetAsync API is not available on this platform.</exception>
        public virtual async Task<JsonSchema4> ResolveUrlReferenceAsync(string url)
        {
            return await JsonSchema4.FromUrlAsync(url, schema => this).ConfigureAwait(false);
        }

        private async Task<JsonSchema4> ResolveFileReferenceWithAlreadyResolvedCheckAsync(string fullJsonPath, string jsonPath)
        {
            try
            {
                var arr = Regex.Split(fullJsonPath, @"(?=#)");
                if (!_resolvedSchemas.ContainsKey(arr[0]))
                {
                    var schema = await ResolveFileReferenceAsync(arr[0]).ConfigureAwait(false);
                    _schemaResolver.AppendSchema(schema, null);
                    _resolvedSchemas[arr[0]] = schema;
                }

                var result = _resolvedSchemas[arr[0]];
                return arr.Length == 1 ? result : await ResolveReferenceAsync(result, arr[1]).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Could not resolve the JSON path '" + jsonPath + "' with the full JSON path '" + fullJsonPath + "'.", exception);
            }
        }

        private async Task<JsonSchema4> ResolveUrlReferenceWithAlreadyResolvedCheckAsync(string fullJsonPath, string jsonPath)
        {
            try
            {
                var arr = fullJsonPath.Split('#');
                if (!_resolvedSchemas.ContainsKey(arr[0]))
                {
                    var schema = await ResolveUrlReferenceAsync(arr[0]).ConfigureAwait(false);
                    _schemaResolver.AppendSchema(schema, null);
                    _resolvedSchemas[arr[0]] = schema;
                }

                var result = _resolvedSchemas[arr[0]];
                return arr.Length == 1 ? result : await ResolveReferenceAsync(result, "#" + arr[1]).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Could not resolve the JSON path '" + jsonPath + "' with the full JSON path '" + fullJsonPath + "'.", exception);
            }
        }

        private JsonSchema4 ResolveDocumentReference(object obj, List<string> segments, HashSet<object> checkedObjects)
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
                    return ResolveDocumentReference(((IDictionary)obj)[firstSegment], segments.Skip(1).ToList(), checkedObjects);
            }
            else if (obj is IEnumerable)
            {
                int index;
                if (int.TryParse(firstSegment, out index))
                {
                    var enumerable = ((IEnumerable)obj).Cast<object>().ToArray();
                    if (enumerable.Length > index)
                        return ResolveDocumentReference(enumerable[index], segments.Skip(1).ToList(), checkedObjects);
                }
            }
            else
            {
                var extensionObj = obj as JsonExtensionObject;
                if (extensionObj?.ExtensionData?.ContainsKey(firstSegment) == true)
                {
                    return ResolveDocumentReference(extensionObj.ExtensionData[firstSegment], segments.Skip(1).ToList(), checkedObjects);
                }

                foreach (var member in ReflectionCache.GetPropertiesAndFields(obj.GetType()).Where(p => p.CustomAttributes.JsonIgnoreAttribute == null))
                {
                    var pathSegment = member.GetName();
                    if (pathSegment == firstSegment)
                    {
                        var value = member.GetValue(obj);
                        return ResolveDocumentReference(value, segments.Skip(1).ToList(), checkedObjects);
                    }
                }
            }

            return null;
        }
    }
}