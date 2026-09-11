//-----------------------------------------------------------------------
// <copyright file="JsonSchemaSerialization.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;
using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NJsonSchema.References;

namespace NJsonSchema.Infrastructure
{
    /// <summary>The JSON Schema serialization context holding information about the current serialization.</summary>
    public class JsonSchemaSerialization
    {
        internal static readonly JsonSerializerOptions DefaultSerializerOptions = new JsonSerializerOptions();

        /// <summary>Cached type info resolver that enables Populate for getter-only collection properties,
        /// matching Newtonsoft.Json's default behavior of populating existing collections.</summary>
        private static readonly IJsonTypeInfoResolver PopulateTypeInfoResolver =
            new DefaultJsonTypeInfoResolver().WithAddedModifier(static typeInfo =>
            {
                if (typeInfo.Kind != JsonTypeInfoKind.Object || typeInfo.CreateObject == null)
                {
                    return;
                }

                foreach (var property in typeInfo.Properties)
                {
                    if (property.ObjectCreationHandling != null || property.Set != null)
                    {
                        continue;
                    }

                    if (property.PropertyType == typeof(string) || property.PropertyType.IsValueType)
                    {
                        continue;
                    }

                    if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
                    {
                        property.ObjectCreationHandling = JsonObjectCreationHandling.Populate;
                    }
                }
            });

        private static readonly AsyncLocal<SerializationContext?> CurrentContext = new();

        private sealed class SerializationContext(SchemaType schemaType, JsonSerializerOptions options, bool isWriting)
        {
            public SchemaType SchemaType { get; } = schemaType;
            public JsonSerializerOptions Options { get; } = options;
            public bool IsWriting { get; } = isWriting;
        }

        /// <summary>Gets the current schema type.</summary>
        public static SchemaType CurrentSchemaType => CurrentContext.Value?.SchemaType ?? SchemaType.JsonSchema;

        /// <summary>Gets the current serializer options.</summary>
        public static JsonSerializerOptions? CurrentSerializerOptions => CurrentContext.Value?.Options;

        /// <summary>Gets a value indicating whether the object is currently converted to JSON.</summary>
        public static bool IsWriting => CurrentContext.Value?.IsWriting ?? false;

        /// <summary>Serializes an object to a JSON string with reference handling.</summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="schemaType">The schema type.</param>
        /// <param name="converter">The schema serialization converter (may be null).</param>
        /// <param name="writeIndented">Whether to write indented JSON.</param>
        /// <returns>The JSON.</returns>
        public static string ToJson(object obj, SchemaType schemaType, SchemaSerializationConverter? converter, bool writeIndented)
        {
            var options = CreateSerializerOptions(converter, writeIndented);
            var previous = CurrentContext.Value;
            CurrentContext.Value = new SerializationContext(schemaType, options, true);

            try
            {
                JsonSchemaReferenceUtilities.UpdateSchemaReferencePaths(obj, false);
                return JsonSerializer.Serialize(obj, obj.GetType(), options);
            }
            finally
            {
                CurrentContext.Value = previous;
            }
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
            var options = CreateSerializerOptions(converter, false);
            var loader = () => Deserialize<T>(json, options)!;
            return FromJsonWithLoaderAsync(loader, options, schemaType, documentPath, referenceResolverFactory, cancellationToken);
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
            var options = CreateSerializerOptions(converter, false);
            var loader = () =>
            {
                using var reader = new StreamReader(stream);
                return Deserialize<T>(reader.ReadToEnd(), options)!;
            };
            return FromJsonWithLoaderAsync(loader, options, schemaType, documentPath, referenceResolverFactory, cancellationToken);
        }

        private static async Task<T> FromJsonWithLoaderAsync<T>(
            Func<T> loader,
            JsonSerializerOptions options,
            SchemaType schemaType,
            string? documentPath,
            Func<T, JsonReferenceResolver> referenceResolverFactory,
            CancellationToken cancellationToken)
            where T : notnull
        {
            cancellationToken.ThrowIfCancellationRequested();
            var previous = CurrentContext.Value;
            CurrentContext.Value = new SerializationContext(schemaType, options, false);

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
                PostProcessExtensionData(schema);

                await JsonSchemaReferenceUtilities.UpdateSchemaReferencesAsync(schema, referenceResolver, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CurrentContext.Value = previous;
            }

            return schema;
        }

        /// <summary>Deserializes JSON data with the given converter.</summary>
        /// <param name="json">The JSON data.</param>
        /// <param name="converter">The schema serialization converter (may be null).</param>
        /// <returns>The deserialized schema.</returns>
        public static T? FromJson<T>(string json, SchemaSerializationConverter? converter)
        {
            var options = CreateSerializerOptions(converter, false);
            var previous = CurrentContext.Value;
            CurrentContext.Value = new SerializationContext(CurrentSchemaType, options, false);

            try
            {
                return Deserialize<T>(json, options);
            }
            finally
            {
                CurrentContext.Value = previous;
            }
        }

        private static T? Deserialize<T>(string json, JsonSerializerOptions options)
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

        /// <summary>Deserializes JSON data with the given converter.</summary>
        /// <param name="stream">The JSON data stream.</param>
        /// <param name="converter">The schema serialization converter (may be null).</param>
        /// <returns>The deserialized schema.</returns>
        public static T? FromJson<T>(Stream stream, SchemaSerializationConverter? converter)
        {
            // Buffer the stream and delegate to the string overload so we get the
            // lenient-JSON fallback (single quotes, unquoted keys, NBSP, stringified
            // booleans) that stream callers would otherwise miss. Schema documents
            // are small enough that forgoing STJ's streaming path is worth the uniform
            // error-recovery behaviour.
            using var reader = new StreamReader(stream);
            return FromJson<T>(reader.ReadToEnd(), converter);
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
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                TypeInfoResolver = PopulateTypeInfoResolver,

            };

            if (converter != null)
            {
                // Add additional converters first (they take precedence over the factory)
                foreach (var additionalConverter in converter.AdditionalConverters)
                {
                    options.Converters.Add(additionalConverter);
                }

                options.Converters.Add(converter);
            }

            // Caller and property converters retain precedence over this schema-only leniency.
            options.Converters.Add(new LenientBooleanConverter());
            return options;
        }

        /// <summary>Walks the deserialized object tree and converts extension data values
        /// that look like schemas (have "type" or "properties") into JsonSchema instances.</summary>
        internal static void PostProcessExtensionData(object obj)
        {
            PostProcessExtensionData(obj, new HashSet<object>(JsonObjectGraphUtilities.ReferenceIdentityComparer.Instance));
        }

        private static void PostProcessExtensionData(object obj, HashSet<object> visited)
        {
            if (obj == null || obj is string || obj is JsonNode || obj.GetType().IsValueType || !visited.Add(obj))
            {
                return;
            }

            if (obj is IJsonExtensionObject extensionObj && extensionObj.ExtensionData != null)
            {
                foreach (var pair in extensionObj.ExtensionData.ToArray())
                {
                    var value = TryDeserializeValueSchemas(pair.Value);
                    extensionObj.ExtensionData[pair.Key] = value;
                    if (value != null)
                    {
                        PostProcessExtensionData(value, visited);
                    }
                }
            }

            if (obj is JsonSchema schema)
            {
                // Unwrap JsonElement values in schema properties that hold object?
                // STJ deserializes these as JsonElement instead of primitive types
                if (schema.Default is JsonElement defaultElement)
                {
                    schema.Default = ConvertJsonElement(defaultElement);
                }

                if (schema.Example is JsonElement exampleElement)
                {
                    schema.Example = ConvertJsonElement(exampleElement);
                }

                if (schema.ExclusiveMinimumRaw is JsonElement exMinElement)
                {
                    schema.ExclusiveMinimumRaw = ConvertJsonElement(exMinElement);
                }

                if (schema.ExclusiveMaximumRaw is JsonElement exMaxElement)
                {
                    schema.ExclusiveMaximumRaw = ConvertJsonElement(exMaxElement);
                }

                for (var i = 0; i < schema.Enumeration.Count; i++)
                {
                    var items = schema.Enumeration as IList<object?>;
                    if (items != null && items[i] is JsonElement enumElement)
                    {
                        items[i] = ConvertJsonElement(enumElement);
                    }
                }

                // Only structural schema members are traversed; default/example/enum remain literal data.
                if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(schema.GetType(), "properties", out _) && schema.Properties != null)
                {
                    PostProcessExtensionData(schema.Properties, visited);
                }

                if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(schema.GetType(), "patternProperties", out _) && schema.PatternProperties != null)
                {
                    PostProcessExtensionData(schema.PatternProperties, visited);
                }

                if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(schema.GetType(), "definitions", out _) && schema.Definitions != null)
                {
                    PostProcessExtensionData(schema.Definitions, visited);
                }

                if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(schema.GetType(), "items", out _) && schema.Items != null)
                {
                    PostProcessExtensionData(schema.Items, visited);
                }

                if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(schema.GetType(), "allOf", out _) && schema.AllOf != null)
                {
                    PostProcessExtensionData(schema.AllOf, visited);
                }

                if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(schema.GetType(), "anyOf", out _) && schema.AnyOf != null)
                {
                    PostProcessExtensionData(schema.AnyOf, visited);
                }

                if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(schema.GetType(), "oneOf", out _) && schema.OneOf != null)
                {
                    PostProcessExtensionData(schema.OneOf, visited);
                }

                if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(schema.GetType(), "items", out _) && schema.Item != null)
                {
                    PostProcessExtensionData(schema.Item, visited);
                }

                if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(schema.GetType(), "x-dictionaryKey", out _) && schema.DictionaryKey != null)
                {
                    PostProcessExtensionData(schema.DictionaryKey, visited);
                }

                if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(schema.GetType(), "additionalProperties", out _) && schema.AdditionalPropertiesSchema != null)
                {
                    PostProcessExtensionData(schema.AdditionalPropertiesSchema, visited);
                }

                if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(schema.GetType(), "additionalItems", out _) && schema.AdditionalItemsSchema != null)
                {
                    PostProcessExtensionData(schema.AdditionalItemsSchema, visited);
                }

                if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(schema.GetType(), "not", out _) && schema.Not != null)
                {
                    PostProcessExtensionData(schema.Not, visited);
                }

                if (schema.Reference != null) PostProcessExtensionData(schema.Reference, visited);

                if (JsonObjectGraphUtilities.TryGetSerializedPropertyName(schema.GetType(), "discriminator", out _) && schema.DiscriminatorRaw != null)
                {
                    PostProcessExtensionData(schema.DiscriminatorRaw, visited);
                }
            }

            var isDictionary = JsonObjectGraphUtilities.TryGetDictionaryEntries(obj, out var entries);
            if (isDictionary)
            {
                foreach (var entry in entries)
                {
                    if (entry.Value != null) PostProcessExtensionData(entry.Value, visited);
                }
            }
            else if (obj is IEnumerable enumerable)
            {
                foreach (var child in enumerable)
                {
                    if (child != null) PostProcessExtensionData(child, visited);
                }
                return;
            }

            // Match the visitors' existing exclusions for typed documents and derived schemas.
            foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetMethod == null || property.GetIndexParameters().Length != 0 ||
                    property.PropertyType == typeof(string) || property.PropertyType.IsValueType ||
                    property.GetCustomAttribute<JsonIgnoreAttribute>() is { Condition: JsonIgnoreCondition.Always } ||
                    property.GetCustomAttribute<JsonExtensionDataAttribute>() != null ||
                    (obj is JsonSchema && JsonSchema.JsonSchemaPropertiesCache.Contains(property.Name)) ||
                    (isDictionary && (property.DeclaringType != obj.GetType() ||
                        obj.GetType().GetCustomAttribute<JsonConverterAttribute>(true) == null)))
                {
                    continue;
                }

                var originalName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
                if (!JsonObjectGraphUtilities.TryGetSerializedPropertyName(obj.GetType(), originalName, out _))
                {
                    continue;
                }

                var child = property.GetValue(obj);
                if (child != null) PostProcessExtensionData(child, visited);
            }
        }

        private static object? TryDeserializeValueSchemas(object? value)
        {
            if (value is not JsonElement element)
            {
                return value;
            }

            if (element.ValueKind == JsonValueKind.Object)
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
                        var options = CurrentSerializerOptions
                            ?? throw new InvalidOperationException(
                                "JsonSchemaSerialization.CurrentSerializerOptions must be set before converting "
                                + "extension-data schemas. Use JsonSchema.FromJsonAsync / JsonSchemaSerialization.FromJsonAsync "
                                + "to deserialize.");
                        return element.Deserialize<JsonSchema>(options);
                    }
                    catch (InvalidOperationException)
                    {
                        throw;
                    }
                    catch
                    {
                        // object was probably not a JSON Schema
                    }
                }

                var dictionary = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    dictionary[property.Name] = TryDeserializeValueSchemas(property.Value);
                }
                return dictionary;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                return element.EnumerateArray().Select(item => TryDeserializeValueSchemas(item)).ToArray();
            }

            return ConvertJsonElement(element);
        }

        // Literal values must never be interpreted as schemas, including nested objects.
        internal static object? ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                {
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
                    return element.GetString();

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                    {
                        return intValue;
                    }
                    if (element.TryGetInt64(out var longValue))
                    {
                        return longValue;
                    }
                    // Retain the existing double mapping only when writing it cannot change the
                    // JSON number. A cloned element preserves other spellings and arbitrary precision
                    // independently of the source document's lifetime.
                    if (element.TryGetDouble(out var doubleValue) &&
                        !double.IsInfinity(doubleValue) &&
                        JsonSerializer.Serialize(doubleValue) == element.GetRawText())
                    {
                        return doubleValue;
                    }
                    return element.Clone();

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
            return LenientJsonSyntaxNormalizer.Normalize(json);
        }

        private sealed class LenientBooleanConverter : JsonConverter<bool>
        {
            public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.True) return true;
                if (reader.TokenType == JsonTokenType.False) return false;
                if (reader.TokenType == JsonTokenType.String && bool.TryParse(reader.GetString(), out var value)) return value;
                throw new JsonException("Invalid boolean schema value.");
            }

            public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);
        }
    }
}
