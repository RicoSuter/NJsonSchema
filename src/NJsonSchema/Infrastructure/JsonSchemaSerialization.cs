//-----------------------------------------------------------------------
// <copyright file="JsonSchemaSerialization.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NJsonSchema.References;

namespace NJsonSchema.Infrastructure
{
    /// <summary>The JSON Schema serialization context holding information about the current serialization.</summary>
    public class JsonSchemaSerialization
    {
        internal static readonly JsonSerializerOptions DefaultSerializerOptions = new JsonSerializerOptions();

        [ThreadStatic]
        private static SchemaType _currentSchemaType;

        [ThreadStatic]
        private static bool _isWriting;

        [ThreadStatic]
        private static JsonSerializerOptions? _currentSerializerOptions;

        /// <summary>Gets or sets the current schema type.</summary>
        public static SchemaType CurrentSchemaType
        {
            get => _currentSchemaType;
            set => _currentSchemaType = value;
        }

        /// <summary>Gets the current serializer options.</summary>
        public static JsonSerializerOptions? CurrentSerializerOptions
        {
            get => _currentSerializerOptions;
            private set => _currentSerializerOptions = value;
        }

        /// <summary>Gets or sets a value indicating whether the object is currently converted to JSON.</summary>
        public static bool IsWriting
        {
            get => _isWriting;
            private set => _isWriting = value;
        }

        /// <summary>Serializes an object to a JSON string with reference handling.</summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="schemaType">The schema type.</param>
        /// <param name="converter">The schema serialization converter (may be null).</param>
        /// <param name="writeIndented">Whether to write indented JSON.</param>
        /// <returns>The JSON.</returns>
        public static string ToJson(object obj, SchemaType schemaType, SchemaSerializationConverter? converter, bool writeIndented)
        {
            IsWriting = false;
            CurrentSchemaType = schemaType;

            JsonSchemaReferenceUtilities.UpdateSchemaReferencePaths(obj, false);

            IsWriting = false;
            var options = CreateSerializerOptions(converter, writeIndented);
            CurrentSerializerOptions = options;

            var json = JsonSerializer.Serialize(obj, obj.GetType(), options);

            CurrentSerializerOptions = null;
            CurrentSchemaType = SchemaType.JsonSchema;

            return json;
        }

        /// <summary>Deserializes JSON data to a schema with reference handling.</summary>
        /// <param name="json">The JSON data.</param>
        /// <param name="schemaType">The schema type.</param>
        /// <param name="documentPath">The document path.</param>
        /// <param name="referenceResolverFactory">The reference resolver factory.</param>
        /// <param name="converter">The schema serialization converter (may be null).</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The deserialized schema.</returns>
        public static Task<T> FromJsonAsync<T>(string json, SchemaType schemaType, string? documentPath,
            Func<T, JsonReferenceResolver> referenceResolverFactory, SchemaSerializationConverter? converter, CancellationToken cancellationToken = default)
            where T : notnull
        {
            var loader = () => FromJson<T>(json, converter)!;
            return FromJsonWithLoaderAsync(loader, schemaType, documentPath, referenceResolverFactory, cancellationToken);
        }

        /// <summary>Deserializes JSON data to a schema with reference handling.</summary>
        /// <param name="stream">The JSON data stream.</param>
        /// <param name="schemaType">The schema type.</param>
        /// <param name="documentPath">The document path.</param>
        /// <param name="referenceResolverFactory">The reference resolver factory.</param>
        /// <param name="converter">The schema serialization converter (may be null).</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The deserialized schema.</returns>
        public static Task<T> FromJsonAsync<T>(Stream stream, SchemaType schemaType, string? documentPath,
            Func<T, JsonReferenceResolver> referenceResolverFactory, SchemaSerializationConverter? converter, CancellationToken cancellationToken = default)
            where T : notnull
        {
            var loader = () => FromJson<T>(stream, converter)!;
            return FromJsonWithLoaderAsync(loader, schemaType, documentPath, referenceResolverFactory, cancellationToken);
        }

        private static async Task<T> FromJsonWithLoaderAsync<T>(
            Func<T> loader,
            SchemaType schemaType,
            string? documentPath,
            Func<T, JsonReferenceResolver> referenceResolverFactory,
            CancellationToken cancellationToken)
            where T : notnull
        {
            cancellationToken.ThrowIfCancellationRequested();
            CurrentSchemaType = schemaType;

            T schema;
            try
            {
                schema = loader();
                if (schema is IDocumentPathProvider documentPathProvider)
                {
                    documentPathProvider.DocumentPath = documentPath;
                }

                var referenceResolver = referenceResolverFactory.Invoke(schema);
                if (schema is IJsonReference referenceSchema)
                {
                    if (!string.IsNullOrEmpty(documentPath))
                    {
                        referenceResolver.AddDocumentReference(documentPath!, referenceSchema);
                    }
                }

                // Post-process extension data to detect and deserialize embedded schemas
                // before resolving references (refs may point into extension data)
                if (schema is IJsonExtensionObject)
                {
                    PostProcessExtensionData(schema);
                }

                await JsonSchemaReferenceUtilities.UpdateSchemaReferencesAsync(schema, referenceResolver, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CurrentSchemaType = SchemaType.JsonSchema;
            }

            return schema;
        }

        /// <summary>Deserializes JSON data with the given converter.</summary>
        /// <param name="json">The JSON data.</param>
        /// <param name="converter">The schema serialization converter (may be null).</param>
        /// <returns>The deserialized schema.</returns>
        public static T? FromJson<T>(string json, SchemaSerializationConverter? converter)
        {
            IsWriting = true;
            var options = CreateSerializerOptions(converter, false);
            CurrentSerializerOptions = options;

            try
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(json, options);
                }
                catch (JsonException)
                {
                    var fixedJson = FixLenientJson(json);
                    return JsonSerializer.Deserialize<T>(fixedJson, options);
                }
            }
            finally
            {
                CurrentSerializerOptions = null;
            }
        }

        /// <summary>Deserializes JSON data with the given converter.</summary>
        /// <param name="stream">The JSON data stream.</param>
        /// <param name="converter">The schema serialization converter (may be null).</param>
        /// <returns>The deserialized schema.</returns>
        public static T? FromJson<T>(Stream stream, SchemaSerializationConverter? converter)
        {
            IsWriting = true;
            var options = CreateSerializerOptions(converter, false);
            CurrentSerializerOptions = options;

            try
            {
                return JsonSerializer.Deserialize<T>(stream, options);
            }
            finally
            {
                CurrentSerializerOptions = null;
            }
        }

        private static JsonSerializerOptions CreateSerializerOptions(SchemaSerializationConverter? converter, bool writeIndented)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = writeIndented,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                MaxDepth = 128,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };

            if (converter != null)
            {
                options.Converters.Add(converter);
            }

            return options;
        }

        /// <summary>Walks the deserialized object tree and converts extension data values
        /// that look like schemas (have "type" or "properties") into JsonSchema instances.</summary>
        internal static void PostProcessExtensionData(object obj)
        {
            PostProcessExtensionData(obj, []);
        }

        private static void PostProcessExtensionData(object obj, HashSet<object> visited)
        {
            if (obj == null || !visited.Add(obj))
            {
                return;
            }

            if (obj is IJsonExtensionObject extensionObj && extensionObj.ExtensionData != null)
            {
                foreach (var pair in extensionObj.ExtensionData.ToArray())
                {
                    extensionObj.ExtensionData[pair.Key] = TryDeserializeValueSchemas(pair.Value);
                }
            }

            if (obj is JsonSchema schema)
            {
                foreach (var prop in schema.Properties.Values)
                {
                    PostProcessExtensionData(prop, visited);
                }

                foreach (var def in schema.Definitions.Values)
                {
                    PostProcessExtensionData(def, visited);
                }

                foreach (var item in schema.AllOf)
                {
                    PostProcessExtensionData(item, visited);
                }

                foreach (var item in schema.AnyOf)
                {
                    PostProcessExtensionData(item, visited);
                }

                foreach (var item in schema.OneOf)
                {
                    PostProcessExtensionData(item, visited);
                }

                if (schema.Item != null)
                {
                    PostProcessExtensionData(schema.Item, visited);
                }

                if (schema.AdditionalPropertiesSchema != null)
                {
                    PostProcessExtensionData(schema.AdditionalPropertiesSchema, visited);
                }

                if (schema.AdditionalItemsSchema != null)
                {
                    PostProcessExtensionData(schema.AdditionalItemsSchema, visited);
                }

                if (schema.Not != null)
                {
                    PostProcessExtensionData(schema.Not, visited);
                }
            }
        }

        private static object? TryDeserializeValueSchemas(object? value)
        {
            if (value is JsonElement element)
            {
                return ConvertJsonElement(element);
            }

            return value;
        }

        private static object? ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                {
                    var hasType = element.TryGetProperty("type", out _);
                    var hasProperties = element.TryGetProperty("properties", out _);
                    var isSchema = hasType || hasProperties;

                    if (isSchema && element.TryGetProperty("required", out var req) &&
                        (req.ValueKind == JsonValueKind.True || req.ValueKind == JsonValueKind.False))
                    {
                        isSchema = false;
                    }

                    if (isSchema)
                    {
                        try
                        {
                            var options = CurrentSerializerOptions ?? DefaultSerializerOptions;
                            return element.Deserialize<JsonSchema>(options);
                        }
                        catch
                        {
                            // object was probably not a JSON Schema
                        }
                    }

                    var dictionary = new Dictionary<string, object?>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dictionary[prop.Name] = ConvertJsonElement(prop.Value);
                    }
                    return dictionary;
                }

                case JsonValueKind.Array:
                    return element.EnumerateArray().Select(ConvertJsonElement).ToArray();

                case JsonValueKind.String:
                {
                    var stringValue = element.GetString();
                    if (stringValue != null &&
                        DateTime.TryParse(stringValue, System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.RoundtripKind, out var dateTime))
                    {
                        return dateTime;
                    }
                    return stringValue;
                }

                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var longValue))
                    {
                        return longValue;
                    }
                    return element.GetDouble();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                default:
                    return null;
            }
        }
        /// <summary>Fixes lenient JSON (single quotes, unquoted property names) to be valid JSON.</summary>
        internal static string FixLenientJson(string json)
        {
            var fixedJson = json.Replace('\'', '"');
            fixedJson = Regex.Replace(fixedJson, @"(?<=[\{,]\s*)([a-zA-Z_$][a-zA-Z0-9_$]*)\s*:", "\"$1\":");
            return fixedJson;
        }
    }
}
