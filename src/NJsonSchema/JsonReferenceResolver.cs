//-----------------------------------------------------------------------
// <copyright file="JsonReferenceResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Namotion.Reflection;
using Newtonsoft.Json;
using NJsonSchema.Infrastructure;
using NJsonSchema.References;

namespace NJsonSchema
{
    /// <summary>Resolves JSON Pointer references.</summary>
    public class JsonReferenceResolver
    {
        private readonly JsonSchemaAppender _schemaAppender;
        private readonly Dictionary<string, IJsonReference> _resolvedObjects = new Dictionary<string, IJsonReference>();

        /// <summary>Initializes a new instance of the <see cref="JsonReferenceResolver"/> class.</summary>
        /// <param name="schemaAppender">The schema appender.</param>
        public JsonReferenceResolver(JsonSchemaAppender schemaAppender)
        {
            _schemaAppender = schemaAppender;
        }

        /// <summary>Creates the factory to be used in the FromJsonAsync method.</summary>
        /// <param name="typeNameGenerator">The type name generator.</param>
        /// <returns>The factory.</returns>
        public static Func<JsonSchema, JsonReferenceResolver> CreateJsonReferenceResolverFactory(ITypeNameGenerator typeNameGenerator)
        {
            JsonReferenceResolver ReferenceResolverFactory(JsonSchema schema) =>
                new JsonReferenceResolver(new JsonSchemaAppender(schema, typeNameGenerator));

            return ReferenceResolverFactory;
        }

        /// <summary>Adds a document reference.</summary>
        /// <param name="documentPath">The document path.</param>
        /// <param name="schema">The referenced schema.</param>
        public void AddDocumentReference(string documentPath, IJsonReference schema)
        {
            _resolvedObjects[documentPath.Contains("://") ? documentPath : DynamicApis.GetFullPath(documentPath)] = schema;
        }

        /// <summary>Gets the object from the given JSON path.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="jsonPath">The JSON path.</param>
        /// <returns>The JSON Schema or <c>null</c> when the object could not be found.</returns>
        /// <exception cref="InvalidOperationException">Could not resolve the JSON path.</exception>
        /// <exception cref="NotSupportedException">Could not resolve the JSON path.</exception>
        public async Task<IJsonReference> ResolveReferenceAsync(object rootObject, string jsonPath)
        {
            return await ResolveReferenceAsync(rootObject, jsonPath, true).ConfigureAwait(false);
        }

        /// <summary>Gets the object from the given JSON path.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="jsonPath">The JSON path.</param>
        /// <returns>The JSON Schema or <c>null</c> when the object could not be found.</returns>
        /// <exception cref="InvalidOperationException">Could not resolve the JSON path.</exception>
        /// <exception cref="NotSupportedException">Could not resolve the JSON path.</exception>
        public async Task<IJsonReference> ResolveReferenceWithoutAppendAsync(object rootObject, string jsonPath)
        {
            return await ResolveReferenceAsync(rootObject, jsonPath, false).ConfigureAwait(false);
        }

        /// <summary>Resolves a document reference.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="jsonPath">The JSON path to resolve.</param>
        /// <returns>The resolved JSON Schema.</returns>
        /// <exception cref="InvalidOperationException">Could not resolve the JSON path.</exception>
        public virtual IJsonReference ResolveDocumentReference(object rootObject, string jsonPath)
        {
            var allSegments = jsonPath.Split('/').Skip(1).ToList();
            var schema = ResolveDocumentReference(rootObject, allSegments, new HashSet<object>());
            if (schema == null)
            {
                throw new InvalidOperationException("Could not resolve the path '" + jsonPath + "'.");
            }

            return schema;
        }

        /// <summary>Resolves a file reference.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The resolved JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The System.IO.File API is not available on this platform.</exception>
        public virtual async Task<IJsonReference> ResolveFileReferenceAsync(string filePath)
        {
            return await JsonSchema.FromFileAsync(filePath, schema => this).ConfigureAwait(false);
        }

        /// <summary>Resolves an URL reference.</summary>
        /// <param name="url">The URL.</param>
        /// <exception cref="NotSupportedException">The HttpClient.GetAsync API is not available on this platform.</exception>
        public virtual async Task<IJsonReference> ResolveUrlReferenceAsync(string url)
        {
            return await JsonSchema.FromUrlAsync(url, schema => this).ConfigureAwait(false);
        }

        private async Task<IJsonReference> ResolveReferenceAsync(object rootObject, string jsonPath, bool append)
        {
            if (jsonPath == "#")
            {
                if (rootObject is IJsonReference)
                {
                    return (IJsonReference)rootObject;
                }

                throw new InvalidOperationException("Could not resolve the JSON path '#' because the root object is not a JsonSchema4.");
            }
            else if (jsonPath.StartsWith("#/"))
            {
                return ResolveDocumentReference(rootObject, jsonPath);
            }
            else if (jsonPath.StartsWith("http://") || jsonPath.StartsWith("https://"))
            {
                return await ResolveUrlReferenceWithAlreadyResolvedCheckAsync(jsonPath, jsonPath, append).ConfigureAwait(false);
            }
            else
            {
                var documentPathProvider = rootObject as IDocumentPathProvider;

                var documentPath = documentPathProvider?.DocumentPath;
                if (documentPath != null)
                {
                    if (documentPath.StartsWith("http://") || documentPath.StartsWith("https://"))
                    {
                        var url = new Uri(new Uri(documentPath), jsonPath).ToString();
                        return await ResolveUrlReferenceWithAlreadyResolvedCheckAsync(url, jsonPath, append).ConfigureAwait(false);
                    }
                    else
                    {
                        var filePath = DynamicApis.PathCombine(DynamicApis.PathGetDirectoryName(documentPath), jsonPath);
                        return await ResolveFileReferenceWithAlreadyResolvedCheckAsync(filePath, jsonPath, append).ConfigureAwait(false);
                    }
                }
                else
                {
                    throw new NotSupportedException("Could not resolve the JSON path '" + jsonPath + "' because no document path is available.");
                }
            }
        }

        private async Task<IJsonReference> ResolveFileReferenceWithAlreadyResolvedCheckAsync(string fullJsonPath, string jsonPath, bool append)
        {
            try
            {
                var arr = Regex.Split(fullJsonPath, @"(?=#)");
                var filePath = DynamicApis.GetFullPath(arr[0]);
                if (!_resolvedObjects.ContainsKey(filePath))
                {
                    var loadedFile = await ResolveFileReferenceAsync(filePath).ConfigureAwait(false);
                    loadedFile.DocumentPath = jsonPath;
                    _resolvedObjects[filePath] = loadedFile;
                }

                var referencedFile = _resolvedObjects[filePath];
                var resolvedSchema = arr.Length == 1 ? referencedFile : await ResolveReferenceAsync(referencedFile, arr[1]).ConfigureAwait(false);
                if (resolvedSchema is JsonSchema && append &&
                    (_schemaAppender.RootObject as JsonSchema)?.Definitions.Values.Contains(referencedFile) != true)
                {
                    var key = jsonPath.Split('/', '\\').Last().Split('.').First();
                    _schemaAppender.AppendSchema((JsonSchema)resolvedSchema, key);
                }

                return resolvedSchema;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Could not resolve the JSON path '" + jsonPath + "' with the full JSON path '" + fullJsonPath + "'.", exception);
            }
        }

        private async Task<IJsonReference> ResolveUrlReferenceWithAlreadyResolvedCheckAsync(string fullJsonPath, string jsonPath, bool append)
        {
            try
            {
                var arr = fullJsonPath.Split('#');
                if (!_resolvedObjects.ContainsKey(arr[0]))
                {
                    var schema = await ResolveUrlReferenceAsync(arr[0]).ConfigureAwait(false);
                    schema.DocumentPath = jsonPath;
                    if (schema is JsonSchema && append)
                    {
                        _schemaAppender.AppendSchema((JsonSchema)schema, null);
                    }

                    _resolvedObjects[arr[0]] = schema;
                }

                var result = _resolvedObjects[arr[0]];
                return arr.Length == 1 ? result : await ResolveReferenceAsync(result, "#" + arr[1]).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Could not resolve the JSON path '" + jsonPath + "' with the full JSON path '" + fullJsonPath + "'.", exception);
            }
        }

        private IJsonReference ResolveDocumentReference(object obj, List<string> segments, HashSet<object> checkedObjects)
        {
            if (obj == null || obj is string || checkedObjects.Contains(obj))
            {
                return null;
            }

            if (obj is IJsonReference reference && reference.Reference != null)
            {
                var result = ResolveDocumentReferenceWithoutDereferencing(reference.Reference, segments, checkedObjects);
                if (result == null)
                {
                    return ResolveDocumentReferenceWithoutDereferencing(obj, segments, checkedObjects);
                }
                else
                {
                    return result;
                }
            }

            return ResolveDocumentReferenceWithoutDereferencing(obj, segments, checkedObjects);
        }

        private IJsonReference ResolveDocumentReferenceWithoutDereferencing(object obj, List<string> segments, HashSet<object> checkedObjects)
        {
            if (segments.Count == 0)
            {
                return obj as IJsonReference;
            }

            checkedObjects.Add(obj);
            var firstSegment = segments[0];

            if (obj is IDictionary)
            {
                if (((IDictionary)obj).Contains(firstSegment))
                {
                    return ResolveDocumentReference(((IDictionary)obj)[firstSegment], segments.Skip(1).ToList(),
                        checkedObjects);
                }
            }
            else if (obj is IEnumerable)
            {
                int index;
                if (int.TryParse(firstSegment, out index))
                {
                    var enumerable = ((IEnumerable)obj).Cast<object>().ToArray();
                    if (enumerable.Length > index)
                    {
                        return ResolveDocumentReference(enumerable[index], segments.Skip(1).ToList(), checkedObjects);
                    }
                }
            }
            else
            {
                var extensionObj = obj as IJsonExtensionObject;
                if (extensionObj?.ExtensionData?.ContainsKey(firstSegment) == true)
                {
                    return ResolveDocumentReference(extensionObj.ExtensionData[firstSegment], segments.Skip(1).ToList(),
                        checkedObjects);
                }

                foreach (var member in obj.GetType().GetContextualPropertiesAndFields()
                    .Where(p => p.GetTypeAttribute<JsonIgnoreAttribute>() == null))
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