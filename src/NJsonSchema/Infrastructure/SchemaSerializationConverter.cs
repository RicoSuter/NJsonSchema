//-----------------------------------------------------------------------
// <copyright file="SchemaSerializationConverter.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace NJsonSchema.Infrastructure
{
    /// <summary>A converter factory that supports property renaming and ignoring per type,
    /// replacing the Newtonsoft PropertyRenameAndIgnoreSerializerContractResolver pattern.</summary>
    public class SchemaSerializationConverter : JsonConverterFactory
    {
        private readonly Dictionary<Type, HashSet<string>> _ignores = [];
        private readonly Dictionary<Type, Dictionary<string, string>> _renames = [];
        private readonly List<JsonConverter> _additionalConverters = [];
        private readonly bool _ignoreEmptyCollections;

        /// <summary>Initializes a new instance of the <see cref="SchemaSerializationConverter"/> class.</summary>
        /// <param name="ignoreEmptyCollections">Whether to skip empty collections during serialization.</param>
        public SchemaSerializationConverter(bool ignoreEmptyCollections = true)
        {
            _ignoreEmptyCollections = ignoreEmptyCollections;
        }

        /// <summary>Ignore the given property/properties of the given type.
        /// Also registers the type with the converter (CanConvert returns true).
        /// Call with no property names to just register the type for empty collection filtering.</summary>
        /// <param name="type">The type.</param>
        /// <param name="jsonPropertyNames">One or more JSON properties to ignore.</param>
        public void IgnoreProperty(Type type, params string[] jsonPropertyNames)
        {
            if (!_ignores.TryGetValue(type, out var value))
            {
                value = [];
                _ignores[type] = value;
            }

            foreach (var property in jsonPropertyNames)
            {
                value.Add(property);
            }
        }

        /// <summary>Rename a property of the given type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="propertyName">The JSON property name to rename.</param>
        /// <param name="newJsonPropertyName">The new JSON property name.</param>
        public void RenameProperty(Type type, string propertyName, string newJsonPropertyName)
        {
            if (!_renames.TryGetValue(type, out var value))
            {
                value = [];
                _renames[type] = value;
            }

            value[propertyName] = newJsonPropertyName;
        }

        /// <summary>Adds a converter that will be included in the serializer options alongside this factory.
        /// These converters are added to both the full and stripped options, and take precedence
        /// over this factory for types they handle.</summary>
        /// <param name="converter">The converter to add.</param>
        public void AddConverter(JsonConverter converter)
        {
            _additionalConverters.Add(converter);
        }

        /// <summary>Gets the additional converters registered with this factory.</summary>
        internal IReadOnlyList<JsonConverter> AdditionalConverters => _additionalConverters;

        /// <summary>Checks whether a JSON property is ignored for the given type (used by JsonPathUtilities).</summary>
        public bool IsPropertyIgnored(Type type, string jsonPropertyName)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                if (_ignores.TryGetValue(t, out var ignored) && ignored.Contains(jsonPropertyName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns the merged renames (base types included) for the given type,
        /// or null if no renames are registered for it or any base.</summary>
        internal Dictionary<string, string>? GetMergedRenames(Type type)
        {
            Dictionary<string, string>? merged = null;
            for (var t = type; t != null; t = t.BaseType)
            {
                if (_renames.TryGetValue(t, out var typeRenames))
                {
                    merged ??= [];
                    foreach (var rename in typeRenames)
                    {
                        if (!merged.ContainsKey(rename.Key))
                        {
                            merged[rename.Key] = rename.Value;
                        }
                    }
                }
            }
            return merged;
        }

        /// <summary>Determines whether this converter can convert the specified type.</summary>
        public override bool CanConvert(Type typeToConvert)
        {
            // Don't override types that have their own [JsonConverter] attribute —
            // attribute converters should handle their own serialization.
            if (typeToConvert.IsDefined(typeof(JsonConverterAttribute), false))
            {
                return false;
            }

            for (var type = typeToConvert; type != null; type = type.BaseType)
            {
                if (_ignores.ContainsKey(type) || _renames.ContainsKey(type))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Creates a converter for the specified type.</summary>
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            HashSet<string>? mergedIgnores = null;
            Dictionary<string, string>? mergedRenames = null;

            for (var type = typeToConvert; type != null; type = type.BaseType)
            {
                if (_ignores.TryGetValue(type, out var typeIgnores))
                {
                    mergedIgnores ??= [];
                    mergedIgnores.UnionWith(typeIgnores);
                }

                if (_renames.TryGetValue(type, out var typeRenames))
                {
                    mergedRenames ??= [];
                    foreach (var rename in typeRenames)
                    {
                        if (!mergedRenames.ContainsKey(rename.Key))
                        {
                            mergedRenames[rename.Key] = rename.Value;
                        }
                    }
                }
            }

            var converterType = typeof(PropertyFilterConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter?)Activator.CreateInstance(converterType, this, mergedIgnores, mergedRenames, _ignoreEmptyCollections);
        }

        private sealed class PropertyFilterConverter<T> : JsonConverter<T>
        {
            [ThreadStatic]
            private static JsonSerializerOptions? _cachedParentOptions;
            [ThreadStatic]
            private static JsonSerializerOptions? _cachedStrippedOptions;

            private readonly SchemaSerializationConverter _factory;
            private readonly HashSet<string>? _ignores;
            private readonly Dictionary<string, string>? _renames;
            private readonly bool _ignoreEmptyCollections;

            public PropertyFilterConverter(
                SchemaSerializationConverter factory,
                HashSet<string>? ignores,
                Dictionary<string, string>? renames,
                bool ignoreEmptyCollections)
            {
                _factory = factory;
                _ignores = ignores;
                _renames = renames;
                _ignoreEmptyCollections = ignoreEmptyCollections;
            }

            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                // Parse to JsonNode so we can apply reverse renames per-type. The nested
                // deserialization uses stripped options (no factory) so it doesn't re-enter
                // this converter; renames for nested registered types have already been
                // applied in the walk below, guided by the CLR type model.
                var node = JsonNode.Parse(ref reader);
                if (node is not JsonObject obj)
                {
                    return default;
                }

                var optionsWithout = GetOrCreateOptionsWithout(options);

                // Apply reverse renames at every level, scoped to the CLR type at that level.
                // Unlike the prior global walk, this leaves unrelated keys (NSwag's
                // OpenApiOperation.IsDeprecated, vendor extensions, etc.) alone.
                ApplyTypedReverseRenames(obj, typeToConvert, _factory, optionsWithout, []);

                // Remove null values for properties that STJ cannot assign null to
                // (e.g., non-nullable IDictionary/ICollection). This handles YAML documents
                // where empty keys (e.g., "paths:") deserialize as "paths": null in JSON.
                RemoveNullCollectionProperties(obj, typeToConvert);

                return obj.Deserialize<T>(optionsWithout);
            }

            /// <summary>Walks the JSON tree following the CLR type model, applying each type's
            /// reverse renames to the corresponding <see cref="JsonObject"/>.</summary>
            private static void ApplyTypedReverseRenames(JsonNode? node, Type targetType,
                SchemaSerializationConverter factory, JsonSerializerOptions strippedOptions,
                HashSet<JsonObject> visited)
            {
                JsonTypeInfo typeInfo;
                try
                {
                    typeInfo = strippedOptions.GetTypeInfo(targetType);
                }
                catch
                {
                    return;
                }

                // Explicit converters own their entire JSON representation, including
                // keys that coincide with the CLR type's ignore or rename rules.
                if (typeInfo.Kind == JsonTypeInfoKind.None)
                {
                    return;
                }

                if (node is JsonObject obj)
                {
                    if (!visited.Add(obj))
                    {
                        return;
                    }

                    // Drop ignored inputs before examining their values, including both
                    // original and renamed wire names. They must not become extension data.
                    var comparison = strippedOptions.PropertyNameCaseInsensitive
                        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                    var renames = factory.GetMergedRenames(targetType);
                    foreach (var key in obj.Select(property => property.Key).ToArray())
                    {
                        var originalName = renames?.FirstOrDefault(rename =>
                            string.Equals(rename.Value, key, comparison)).Key ?? key;
                        var effectiveName = renames?.FirstOrDefault(rename =>
                            string.Equals(rename.Key, originalName, comparison)).Value ?? originalName;
                        var ignored = false;
                        for (var type = targetType; type != null; type = type.BaseType)
                        {
                            if (factory._ignores.TryGetValue(type, out var names) &&
                                names.Any(name => string.Equals(name, key, comparison) ||
                                    string.Equals(name, originalName, comparison) ||
                                    string.Equals(name, effectiveName, comparison)))
                            {
                                ignored = true;
                                break;
                            }
                        }
                        if (ignored)
                        {
                            obj.Remove(key);
                        }
                    }

                    // Apply this type's reverse renames: the JSON key is the renamed form,
                    // the CLR property expects the original form.
                    if (renames != null)
                    {
                        foreach (var kvp in renames)
                        {
                            if (obj.ContainsKey(kvp.Value) && !obj.ContainsKey(kvp.Key))
                            {
                                var value = obj[kvp.Value];
                                obj.Remove(kvp.Value);
                                obj[kvp.Key] = value?.DeepClone();
                            }
                        }
                    }

                    // Recurse into typed properties, but skip extension data (which is
                    // free-form user content, not part of our type model).
                    foreach (var propertyInfo in typeInfo.Properties)
                    {
                        if (propertyInfo.IsExtensionData || propertyInfo.CustomConverter != null)
                        {
                            continue;
                        }

                        if (!obj.TryGetPropertyValue(propertyInfo.Name, out var child) || child == null)
                        {
                            continue;
                        }

                        RecurseInto(child, propertyInfo.PropertyType, factory, strippedOptions, visited);
                    }
                }
                else if (node is JsonArray array)
                {
                    foreach (var item in array)
                    {
                        ApplyTypedReverseRenames(item, targetType, factory, strippedOptions, visited);
                    }
                }
            }

            private static void RecurseInto(JsonNode? child, Type propertyType,
                SchemaSerializationConverter factory, JsonSerializerOptions strippedOptions,
                HashSet<JsonObject> visited)
            {
                if (child == null)
                {
                    return;
                }

                // Check ownership before classifying collections: their converter may
                // use an object shape unrelated to the declared dictionary/element types.
                if (strippedOptions.GetTypeInfo(propertyType).Kind == JsonTypeInfoKind.None)
                {
                    return;
                }

                // IDictionary<,> — values share the value type
                if (TryGetDictionaryValueType(propertyType, out var dictValueType) && child is JsonObject dictObj)
                {
                    foreach (var entry in dictObj)
                    {
                        RecurseInto(entry.Value, dictValueType, factory, strippedOptions, visited);
                    }
                    return;
                }

                // IEnumerable<T> (excluding string) — elements share the element type
                if (propertyType != typeof(string) && TryGetEnumerableElementType(propertyType, out var elementType))
                {
                    if (child is JsonArray arr)
                    {
                        foreach (var item in arr)
                        {
                            RecurseInto(item, elementType, factory, strippedOptions, visited);
                        }
                    }
                    return;
                }

                // Direct nested object / array of objects of the declared property type
                ApplyTypedReverseRenames(child, propertyType, factory, strippedOptions, visited);
            }

            private static bool TryGetDictionaryValueType(Type type, out Type valueType)
            {
                foreach (var interfaceType in EnumerateTypeAndInterfaces(type))
                {
                    if (interfaceType.IsGenericType &&
                        (interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                         interfaceType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)))
                    {
                        valueType = interfaceType.GetGenericArguments()[1];
                        return true;
                    }
                }
                valueType = null!;
                return false;
            }

            private static bool TryGetEnumerableElementType(Type type, out Type elementType)
            {
                if (type.IsArray)
                {
                    elementType = type.GetElementType()!;
                    return true;
                }
                foreach (var interfaceType in EnumerateTypeAndInterfaces(type))
                {
                    if (interfaceType.IsGenericType &&
                        interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        elementType = interfaceType.GetGenericArguments()[0];
                        return true;
                    }
                }
                elementType = null!;
                return false;
            }

            private static IEnumerable<Type> EnumerateTypeAndInterfaces(Type type)
            {
                yield return type;
                foreach (var i in type.GetInterfaces())
                {
                    yield return i;
                }
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                var runtimeType = value is JsonSchema ? value.GetType() : typeof(T);
                if (runtimeType != typeof(T) &&
                    (runtimeType.IsDefined(typeof(JsonConverterAttribute), false) ||
                     options.Converters.Any(converter => converter is not SchemaSerializationConverter && converter.CanConvert(runtimeType))))
                {
                    JsonSerializer.Serialize(writer, value, runtimeType, options);
                    return;
                }

                // Inspect the runtime schema contract without constructing a converter-owned
                // JsonTypeInfo, whose property list is empty for resolver modifiers.
                var optionsWithout = GetOrCreateOptionsWithout(options);
                var typeInfo = optionsWithout.GetTypeInfo(runtimeType);
                var renames = runtimeType == typeof(T) ? _renames : _factory.GetMergedRenames(runtimeType);

                writer.WriteStartObject();

                JsonPropertyInfo? extensionDataProperty = null;

                foreach (var property in typeInfo.Properties)
                {
                    // Defer extension data to write after all regular properties
                    if (property.IsExtensionData)
                    {
                        extensionDataProperty = property;
                        continue;
                    }

                    var jsonName = property.Name;

                    // Apply ignores
                    if (_ignores?.Contains(jsonName) == true || _factory.IsPropertyIgnored(runtimeType, jsonName))
                    {
                        continue;
                    }

                    // Skip properties without getter
                    if (property.Get == null)
                    {
                        continue;
                    }

                    var propValue = property.Get(value!);

                    // Respect STJ's ShouldSerialize (handles [JsonIgnore(Condition = ...)])
                    if (property.ShouldSerialize != null && !property.ShouldSerialize(value!, propValue))
                    {
                        continue;
                    }

                    // Skip null values
                    if (propValue == null)
                    {
                        continue;
                    }

                    // Skip empty collections/dictionaries when configured.
                    // Exclude JsonNode subtypes (JsonObject/JsonArray) — they represent
                    // JSON values, not .NET collections (e.g., "additionalProperties": {}).
                    if (_ignoreEmptyCollections && propValue is not string && propValue is not JsonNode && propValue is IEnumerable enumerable)
                    {
                        if (propValue is ICollection collection)
                        {
                            if (collection.Count == 0)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            var enumerator = enumerable.GetEnumerator();
                            try
                            {
                                if (!enumerator.MoveNext())
                                {
                                    continue;
                                }
                            }
                            finally
                            {
                                (enumerator as IDisposable)?.Dispose();
                            }
                        }
                    }

                    // Apply renames
                    if (renames?.TryGetValue(jsonName, out var renamed) == true)
                    {
                        jsonName = renamed;
                    }

                    writer.WritePropertyName(jsonName);

                    if (property.CustomConverter != null)
                    {
                        WriteConvertedProperty(writer, propValue, property, options);
                        continue;
                    }

                    // Serialize the property value using FULL options so nested types
                    // get their own SchemaSerializationConverter filters applied.
                    // Use runtime type for object-typed properties so values like int/string
                    // serialize correctly (STJ would serialize declared type 'object' as {}).
                    var serializeType = property.PropertyType == typeof(object)
                        ? propValue.GetType()
                        : property.PropertyType;
                    JsonSerializer.Serialize(writer, propValue, serializeType, options);
                }

                // Write extension data AFTER all regular properties
                if (extensionDataProperty?.Get != null &&
                    extensionDataProperty.Get(value!) is IEnumerable<KeyValuePair<string, object?>> extDict)
                {
                    foreach (var pair in extDict)
                    {
                        writer.WritePropertyName(pair.Key);
                        if (pair.Value == null)
                        {
                            writer.WriteNullValue();
                        }
                        else
                        {
                            JsonSerializer.Serialize(writer, pair.Value, pair.Value.GetType(), options);
                        }
                    }
                }

                writer.WriteEndObject();
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1869:Cache and reuse JsonSerializerOptions instances",
                Justification = "Property converters require isolated options to preserve precedence without affecting siblings.")]
            private static void WriteConvertedProperty(Utf8JsonWriter writer, object value,
                JsonPropertyInfo property, JsonSerializerOptions options)
            {
                // Isolate property converters (including factories) from sibling properties.
                // STJ resolves the converter against the declared property type.
                var propertyOptions = new JsonSerializerOptions(options);
                propertyOptions.Converters.Insert(0, property.CustomConverter!);
                JsonSerializer.Serialize(writer, value, property.PropertyType, propertyOptions);
            }

            private static void RemoveNullCollectionProperties(JsonObject obj, Type targetType)
            {
                var nullKeys = new List<string>();
                foreach (var property in obj)
                {
                    if (property.Value == null)
                    {
                        nullKeys.Add(property.Key);
                    }
                }

                if (nullKeys.Count == 0)
                {
                    return;
                }

                foreach (var key in nullKeys)
                {
                    // Find the CLR property (case-insensitive to handle naming policies)
                    var clrProperty = targetType.GetProperty(key,
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (clrProperty == null)
                    {
                        continue;
                    }

                    // Remove null for getter-only collection properties (e.g., Paths, Definitions)
                    // that are initialized in the constructor — STJ can't assign null to them.
                    var hasSetter = clrProperty.GetSetMethod(true) != null;
                    if (!hasSetter && typeof(IEnumerable).IsAssignableFrom(clrProperty.PropertyType) && clrProperty.PropertyType != typeof(string))
                    {
                        obj.Remove(key);
                    }
                }
            }

            private static JsonSerializerOptions GetOrCreateOptionsWithout(JsonSerializerOptions options)
            {
                if (ReferenceEquals(_cachedParentOptions, options) && _cachedStrippedOptions != null)
                {
                    return _cachedStrippedOptions;
                }

                var newOptions = new JsonSerializerOptions(options);
                for (var i = newOptions.Converters.Count - 1; i >= 0; i--)
                {
                    if (newOptions.Converters[i] is SchemaSerializationConverter)
                    {
                        newOptions.Converters.RemoveAt(i);
                    }
                }

                _cachedParentOptions = options;
                _cachedStrippedOptions = newOptions;
                return newOptions;
            }
        }
    }
}
