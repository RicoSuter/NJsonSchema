//-----------------------------------------------------------------------
// <copyright file="JsonReferenceResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Namotion.Reflection;
using NJsonSchema.Infrastructure;
using NJsonSchema.References;
using System.Text.Json.Nodes;

namespace NJsonSchema
{
    /// <summary>Resolves JSON Pointer references.</summary>
    public class JsonReferenceResolver
    {
        private static readonly List<ContextualAccessorInfo> JsonSchemaContextualAccessors = typeof(JsonSchema)
            .GetContextualAccessors()
            .Where(p =>
            {
                if (p.MemberInfo is PropertyInfo propertyInfo && propertyInfo.GetMethod?.IsStatic == true)
                {
                    return false;
                }

                var jsonIgnoreAttribute = p.MemberInfo.GetCustomAttribute<JsonIgnoreAttribute>();
                return jsonIgnoreAttribute == null || jsonIgnoreAttribute.Condition != JsonIgnoreCondition.Always;
            })
            .ToList();

        private readonly JsonSchemaAppender _schemaAppender;
        private readonly Dictionary<string, IJsonReference> _resolvedObjects = [];

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
            JsonReferenceResolver ReferenceResolverFactory(JsonSchema schema)
            {
                return new JsonReferenceResolver(new JsonSchemaAppender(schema, typeNameGenerator));
            }

            return ReferenceResolverFactory;
        }

        /// <summary>Adds a document reference.</summary>
        /// <param name="documentPath">The document path.</param>
        /// <param name="schema">The referenced schema.</param>
        public void AddDocumentReference(string documentPath, IJsonReference schema)
        {
            _resolvedObjects[documentPath.Contains("://") ? documentPath : Path.GetFullPath(documentPath)] = schema;
        }

        /// <summary>Gets the object from the given JSON path.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="jsonPath">The JSON path.</param>
        /// <param name="targetType">The target type to resolve.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema or <c>null</c> when the object could not be found.</returns>
        /// <exception cref="InvalidOperationException">Could not resolve the JSON path.</exception>
        /// <exception cref="NotSupportedException">Could not resolve the JSON path.</exception>
        public async Task<IJsonReference> ResolveReferenceAsync(object rootObject, string jsonPath, Type targetType,
                CancellationToken cancellationToken = default)
        {
            return await ResolveReferenceAsync(rootObject, jsonPath, targetType, true, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Gets the object from the given JSON path.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="jsonPath">The JSON path.</param>
        /// <param name="targetType">The target type to resolve.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema or <c>null</c> when the object could not be found.</returns>
        /// <exception cref="InvalidOperationException">Could not resolve the JSON path.</exception>
        /// <exception cref="NotSupportedException">Could not resolve the JSON path.</exception>
        public async Task<IJsonReference> ResolveReferenceWithoutAppendAsync(object rootObject, string jsonPath, Type targetType,
                CancellationToken cancellationToken = default)
        {
            return await ResolveReferenceAsync(rootObject, jsonPath, targetType, false, cancellationToken).ConfigureAwait(false);
        }

        private static string UnescapeReferenceSegment(string segment)
        {
            var urlDecoded = Uri.UnescapeDataString(segment);
            return urlDecoded.Replace("~1", "/").Replace("~0", "~");
        }

        /// <summary>Resolves a document reference.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="jsonPath">The JSON path to resolve.</param>
        /// <param name="targetType">The target type to resolve.</param>
        /// <returns>The resolved JSON Schema.</returns>
        /// <exception cref="InvalidOperationException">Could not resolve the JSON path.</exception>
        public virtual IJsonReference ResolveDocumentReference(object rootObject, string jsonPath, Type targetType)
        {
            var allSegments = jsonPath.Split('/').Skip(1).ToList();
            for (var i = 0; i < allSegments.Count; i++)
            {
                allSegments[i] = UnescapeReferenceSegment(allSegments[i]);
            }

            var schema = ResolveDocumentReference(rootObject, allSegments, targetType, new HashSet<object>(JsonObjectGraphUtilities.ReferenceIdentityComparer.Instance))
                         ?? throw new InvalidOperationException($"Could not resolve the path '{jsonPath}'.");

            return schema;
        }

        /// <summary>Resolves a file reference.</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The resolved JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The System.IO.File API is not available on this platform.</exception>
        public virtual async Task<IJsonReference> ResolveFileReferenceAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await JsonSchema.FromFileAsync(filePath, schema => this, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Resolves an URL reference.</summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <exception cref="NotSupportedException">The HttpClient.GetAsync API is not available on this platform.</exception>
        public virtual async Task<IJsonReference> ResolveUrlReferenceAsync(string url, CancellationToken cancellationToken = default)
        {
            return await JsonSchema.FromUrlAsync(url, schema => this, cancellationToken).ConfigureAwait(false);
        }

        private async Task<IJsonReference> ResolveReferenceAsync(object rootObject, string jsonPath, Type targetType, bool append, CancellationToken cancellationToken = default)
        {
            if (jsonPath == "#")
            {
                if (rootObject is IJsonReference)
                {
                    return (IJsonReference)rootObject;
                }

                throw new InvalidOperationException("Could not resolve the JSON path '#' because the root object is not a JsonSchema4.");
            }
            else if (jsonPath.StartsWith("#/", StringComparison.Ordinal))
            {
                return ResolveDocumentReference(rootObject, jsonPath, targetType);
            }
            else if (jsonPath.StartsWith("http://", StringComparison.Ordinal) || jsonPath.StartsWith("https://", StringComparison.Ordinal))
            {
                return await ResolveUrlReferenceWithAlreadyResolvedCheckAsync(jsonPath, jsonPath, targetType, append, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var documentPathProvider = rootObject as IDocumentPathProvider;

                var documentPath = documentPathProvider?.DocumentPath;
                if (documentPath != null)
                {
                    if (documentPath.StartsWith("http://", StringComparison.Ordinal) || documentPath.StartsWith("https://", StringComparison.Ordinal))
                    {
                        var url = new Uri(new Uri(documentPath), jsonPath).ToString();
                        return await ResolveUrlReferenceWithAlreadyResolvedCheckAsync(url, jsonPath, targetType, append, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var filePath = ResolveFilePath(documentPath, jsonPath);
                        return await ResolveFileReferenceWithAlreadyResolvedCheckAsync(filePath, targetType, jsonPath, append, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    throw new NotSupportedException("Could not resolve the JSON path '" + jsonPath + "' because no document path is available.");
                }
            }
        }

        /// <summary>Resolves file path.</summary>
        /// <param name="documentPath">The document path.</param>
        /// <param name="jsonPath">The JSON path</param>
        public virtual string ResolveFilePath(string documentPath, string jsonPath)
        {
            var arr = Regex.Split(jsonPath, @"(?=#)");
            return Path.Combine(Path.GetDirectoryName(documentPath)!, arr[0]);
        }

        private async Task<IJsonReference> ResolveFileReferenceWithAlreadyResolvedCheckAsync(string filePath, Type targetType, string jsonPath, bool append, CancellationToken cancellationToken)
        {
            try
            {
                var fullPath = Path.GetFullPath(filePath);
                var arr = Regex.Split(jsonPath, @"(?=#)");

                fullPath = DynamicApis.HandleSubdirectoryRelativeReferences(fullPath, jsonPath);

                if (!_resolvedObjects.TryGetValue(fullPath, out IJsonReference? value))
                {
                    value = await ResolveFileReferenceAsync(fullPath, cancellationToken).ConfigureAwait(false);
                    value.DocumentPath = arr[0];
                    _resolvedObjects[fullPath] = value;
                }

                var referencedFile = value;
                var resolvedSchema = arr.Length == 1 ? referencedFile : await ResolveReferenceAsync(referencedFile, arr[1], targetType, cancellationToken).ConfigureAwait(false);
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
                throw new InvalidOperationException("Could not resolve the JSON path '" + jsonPath + "' within the file path '" + filePath + "'.", exception);
            }
        }

        private async Task<IJsonReference> ResolveUrlReferenceWithAlreadyResolvedCheckAsync(string fullJsonPath, string jsonPath, Type targetType, bool append, CancellationToken cancellationToken)
        {
            try
            {
                var arr = fullJsonPath.Split('#');
                if (!_resolvedObjects.TryGetValue(arr[0], out IJsonReference? value))
                {
                    var schema = await ResolveUrlReferenceAsync(arr[0], cancellationToken).ConfigureAwait(false);
                    schema.DocumentPath = arr[0];
                    if (schema is JsonSchema && append)
                    {
                        _schemaAppender.AppendSchema((JsonSchema)schema, null);
                    }

                    value = schema;
                    _resolvedObjects[arr[0]] = value;
                }

                var result = value;
                return arr.Length == 1 ? result : await ResolveReferenceAsync(result, "#" + arr[1], targetType, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Could not resolve the JSON path '" + jsonPath + "' with the full JSON path '" + fullJsonPath + "'.", exception);
            }
        }

        private IJsonReference? ResolveDocumentReference(object obj, List<string> segments, Type targetType, HashSet<object> checkedObjects)
        {
            if (obj == null || obj is string || checkedObjects.Contains(obj))
            {
                return null;
            }

            if (obj is IJsonReference reference && reference.Reference != null)
            {
                var result = ResolveDocumentReferenceWithoutDereferencing(reference.Reference, segments, targetType, checkedObjects);
                if (result == null)
                {
                    return ResolveDocumentReferenceWithoutDereferencing(obj, segments, targetType, checkedObjects);
                }
                else
                {
                    return result;
                }
            }

            return ResolveDocumentReferenceWithoutDereferencing(obj, segments, targetType, checkedObjects);
        }

        private static void PreserveMaterializedReferences(object? value, string path,
            Dictionary<string, IJsonReference> children, bool restore, Action<object?>? replace,
            HashSet<object> ancestors)
        {
            if (value == null || value is string || value is JsonNode || value.GetType().IsValueType)
            {
                return;
            }

            if (value is IJsonReference reference)
            {
                if (!restore)
                {
                    children[path] = reference;
                    return;
                }
                if (children.TryGetValue(path, out var child))
                {
                    replace?.Invoke(child);
                    return;
                }
            }

            if (!ancestors.Add(value))
            {
                return;
            }

            try
            {
                // JSON Pointer escaping is internal here; protected visitor path syntax is unchanged.
                void VisitChild(object? child, string key, Action<object?>? setter) =>
                    PreserveMaterializedReferences(child, path + "/" + key.Replace("~", "~0").Replace("/", "~1"),
                        children, restore, setter, ancestors);

                if (JsonObjectGraphUtilities.TryGetDictionaryEntries(value, out var entries))
                {
                    foreach (var entry in entries) VisitChild(entry.Value, entry.Key, entry.ReplaceOrRemove);
                    return;
                }

                if (value is IEnumerable enumerable)
                {
                    var index = 0;
                    foreach (var item in enumerable.Cast<object?>().ToArray())
                    {
                        var currentIndex = index++;
                        VisitChild(item, currentIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            value is IList list ? replacement => list[currentIndex] = replacement : null);
                    }
                    return;
                }

                if (value is IJsonExtensionObject extension && extension.ExtensionData != null)
                {
                    foreach (var pair in extension.ExtensionData.ToArray())
                    {
                        VisitChild(pair.Value, pair.Key, replacement => extension.ExtensionData[pair.Key] = replacement);
                    }
                }

                foreach (var member in value.GetType().GetContextualAccessors())
                {
                    if (member.MemberInfo.GetCustomAttribute<JsonIgnoreAttribute>() is { Condition: JsonIgnoreCondition.Always } ||
                        member.MemberInfo.GetCustomAttribute<JsonExtensionDataAttribute>() != null)
                    {
                        continue;
                    }
                    if (member.MemberInfo is PropertyInfo property &&
                        (property.GetMethod == null || property.GetMethod.IsStatic ||
                         property.GetIndexParameters().Length != 0 ||
                         (!property.GetMethod.IsPublic && property.GetCustomAttribute<JsonIncludeAttribute>() == null)))
                    {
                        continue;
                    }
                    if (member.MemberInfo is FieldInfo field &&
                        (field.IsStatic || field.GetCustomAttribute<JsonIncludeAttribute>() == null))
                    {
                        continue;
                    }

                    var originalName = member.MemberInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? member.GetName();
                    if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(value.GetType(), originalName, out var name))
                    {
                        VisitChild(member.GetValue(value), name, replacement => member.SetValue(value, replacement));
                    }
                }
            }
            finally
            {
                ancestors.Remove(value);
            }
        }

        private IJsonReference? ResolveChildReference(object child, List<string> segments, Type targetType,
            HashSet<object> checkedObjects, Action<object?>? replace)
        {
            var resolved = ResolveDocumentReference(child, segments, targetType, checkedObjects);
            if (segments.Count == 0 && child is not IJsonReference && resolved != null)
            {
                // A dictionary materialized as a reference target must stay reachable at its source path.
                replace?.Invoke(resolved);
            }

            return resolved;
        }

        private IJsonReference? ResolveDocumentReferenceWithoutDereferencing(object obj, List<string> segments, Type targetType, HashSet<object> checkedObjects)
        {
            if (segments.Count == 0)
            {
                if (obj is not IJsonReference && JsonObjectGraphUtilities.TryGetDictionaryEntries(obj, out _))
                {
                    var options = JsonSchemaSerialization.CurrentSerializerOptions
                        ?? throw new InvalidOperationException(
                            "JsonSchemaSerialization.CurrentSerializerOptions must be set before resolving references. "
                            + "Use JsonSchema.FromJsonAsync / JsonSchemaSerialization.FromJsonAsync to deserialize.");
                    // Rehydration must not clone already-materialized children: their resolved references
                    // and shared identity are not represented by the temporary JSON payload.
                    var children = new Dictionary<string, IJsonReference>();
                    PreserveMaterializedReferences(obj, "#", children, false, null, new HashSet<object>(JsonObjectGraphUtilities.ReferenceIdentityComparer.Instance));
                    var json = JsonSerializer.Serialize(obj, obj.GetType(), options);
                    var result = JsonSerializer.Deserialize(json, targetType, options) as IJsonReference;
                    if (result != null)
                    {
                        JsonSchemaSerialization.PostProcessExtensionData(result);
                        PreserveMaterializedReferences(result, "#", children, true, null, new HashSet<object>(JsonObjectGraphUtilities.ReferenceIdentityComparer.Instance));
                    }

                    return result;
                }
                else
                {
                    return obj as IJsonReference;
                }
            }

            checkedObjects.Add(obj);
            var firstSegment = segments[0];

            if (JsonObjectGraphUtilities.TryGetDictionaryEntries(obj, out var entries))
            {
                var entry = entries.FirstOrDefault(item => item.Key == firstSegment);
                if (entry?.Value != null)
                {
                    return ResolveChildReference(entry.Value, segments.Skip(1).ToList(), targetType, checkedObjects, entry.ReplaceOrRemove);
                }
            }
            else if (obj is IEnumerable)
            {
                if (int.TryParse(firstSegment, out var index))
                {
                    var enumerable = ((IEnumerable)obj).Cast<object>().ToArray();
                    if (index >= 0 && enumerable.Length > index)
                    {
                        return ResolveChildReference(enumerable[index], segments.Skip(1).ToList(), targetType, checkedObjects,
                            obj is IList list ? value => list[index] = value : null);
                    }
                }
            }
            else
            {
                var extensionObj = obj as IJsonExtensionObject;
                if (extensionObj?.ExtensionData?.ContainsKey(firstSegment) == true)
                {
                    return ResolveChildReference(extensionObj.ExtensionData[firstSegment]!, segments.Skip(1).ToList(), targetType, checkedObjects,
                        value => extensionObj.ExtensionData[firstSegment] = value);
                }

                IEnumerable<ContextualAccessorInfo> properties;
                if (obj.GetType() == typeof(JsonSchema))
                {
                    properties = JsonSchemaContextualAccessors;
                }
                else
                {
                    properties = obj.GetType()
                        .GetContextualAccessors()
                        .Where(p =>
                        {
                            var jsonIgnoreAttribute = p.MemberInfo.GetCustomAttribute<JsonIgnoreAttribute>();
                            return jsonIgnoreAttribute == null || jsonIgnoreAttribute.Condition != JsonIgnoreCondition.Always;
                        });
                }

                foreach (var member in properties)
                {
                    var jsonPropertyNameAttribute = member.MemberInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
                    var pathSegment = jsonPropertyNameAttribute?.Name ?? member.GetName();
                    if (pathSegment == firstSegment)
                    {
                        var value = member.GetValue(obj);
                        if (value != null)
                        {
                            return ResolveChildReference(value, segments.Skip(1).ToList(), targetType, checkedObjects,
                                replacement => member.SetValue(obj, replacement));
                        }
                    }
                }
            }

            return null;
        }
    }
}
