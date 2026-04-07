//-----------------------------------------------------------------------
// <copyright file="SchemaSerializationConverter.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections;
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
        private readonly Dictionary<string, HashSet<string>> _ignores = [];
        private readonly Dictionary<string, Dictionary<string, string>> _renames = [];
        private readonly List<JsonConverter> _additionalConverters = [];
        private readonly bool _ignoreEmptyCollections;

        private Dictionary<string, string>? _allReverseRenamesCache;

        /// <summary>Gets all reverse renames (newName → originalName) across all types.
        /// Used by Read to recursively fix property names before deserialization.</summary>
        internal Dictionary<string, string> GetAllReverseRenames()
        {
            if (_allReverseRenamesCache != null)
            {
                return _allReverseRenamesCache;
            }

            var result = new Dictionary<string, string>();
            foreach (var typeRenames in _renames.Values)
            {
                foreach (var kvp in typeRenames)
                {
                    if (!result.ContainsKey(kvp.Value))
                    {
                        result[kvp.Value] = kvp.Key;
                    }
                }
            }
            _allReverseRenamesCache = result;
            return result;
        }

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
            if (!_ignores.TryGetValue(type.FullName!, out HashSet<string>? value))
            {
                value = [];
                _ignores[type.FullName!] = value;
            }

            foreach (var prop in jsonPropertyNames)
            {
                value.Add(prop);
            }
        }

        /// <summary>Rename a property of the given type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="propertyName">The JSON property name to rename.</param>
        /// <param name="newJsonPropertyName">The new JSON property name.</param>
        public void RenameProperty(Type type, string propertyName, string newJsonPropertyName)
        {
            if (!_renames.TryGetValue(type.FullName!, out Dictionary<string, string>? value))
            {
                value = [];
                _renames[type.FullName!] = value;
            }

            value[propertyName] = newJsonPropertyName;
            _allReverseRenamesCache = null;
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
                if (t.FullName != null && _ignores.TryGetValue(t.FullName, out var ignored) && ignored.Contains(jsonPropertyName))
                {
                    return true;
                }
            }

            return false;
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
                if (type.FullName != null && (_ignores.ContainsKey(type.FullName) || _renames.ContainsKey(type.FullName)))
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
                if (type.FullName != null && _ignores.TryGetValue(type.FullName, out var typeIgnores))
                {
                    mergedIgnores ??= [];
                    mergedIgnores.UnionWith(typeIgnores);
                }

                if (type.FullName != null && _renames.TryGetValue(type.FullName, out var typeRenames))
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
                // Parse to JsonNode, apply ALL reverse renames recursively through the
                // entire tree, then deserialize with stripped options. The recursive rename
                // ensures nested objects (e.g., nested JsonSchema within allOf/properties)
                // also get their property names fixed.
                var node = JsonNode.Parse(ref reader);
                if (node is not JsonObject obj)
                {
                    return default;
                }

                // Apply ALL reverse renames from the factory (not just this type's renames)
                // so that nested types (e.g., JsonSchema within OpenApiDocument) also get
                // their property names fixed (e.g., "nullable" → "x-nullable").
                var allReverseRenames = _factory.GetAllReverseRenames();
                if (allReverseRenames.Count > 0)
                {
                    ApplyReverseRenamesRecursively(obj, allReverseRenames);
                }

                // Remove null values for properties that STJ cannot assign null to
                // (e.g., non-nullable IDictionary/ICollection). This handles YAML documents
                // where empty keys (e.g., "paths:") deserialize as "paths": null in JSON.
                RemoveNullCollectionProperties(obj, typeToConvert);

                var optionsWithout = GetOrCreateOptionsWithout(options);
                return node.Deserialize<T>(optionsWithout);
            }

            /// <summary>Recursively applies reverse renames to all JSON objects in the tree.
            /// The renames dict maps renamedName → originalName.</summary>
            private static void ApplyReverseRenamesRecursively(JsonObject obj, Dictionary<string, string> reverseRenames)
            {
                foreach (var kvp in reverseRenames)
                {
                    // kvp.Key = renamed name (in JSON), kvp.Value = original name (in code)
                    if (obj.ContainsKey(kvp.Key) && !obj.ContainsKey(kvp.Value))
                    {
                        var value = obj[kvp.Key];
                        obj.Remove(kvp.Key);
                        obj[kvp.Value] = value?.DeepClone();
                    }
                }

                foreach (var property in obj)
                {
                    if (property.Value is JsonObject childObj)
                    {
                        ApplyReverseRenamesRecursively(childObj, reverseRenames);
                    }
                    else if (property.Value is JsonArray childArr)
                    {
                        foreach (var item in childArr)
                        {
                            if (item is JsonObject arrObj)
                            {
                                ApplyReverseRenamesRecursively(arrObj, reverseRenames);
                            }
                        }
                    }
                }
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                // Get the type info from stripped options to enumerate properties
                // without re-entering this converter for type T.
                var optionsWithout = GetOrCreateOptionsWithout(options);
                var typeInfo = (JsonTypeInfo<T>)optionsWithout.GetTypeInfo(typeof(T));

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
                    if (_ignores?.Contains(jsonName) == true)
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
                    if (_renames?.TryGetValue(jsonName, out var renamed) == true)
                    {
                        jsonName = renamed;
                    }

                    writer.WritePropertyName(jsonName);

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
                    var clrProp = targetType.GetProperty(key,
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (clrProp == null)
                    {
                        continue;
                    }

                    // Remove null for getter-only collection properties (e.g., Paths, Definitions)
                    // that are initialized in the constructor — STJ can't assign null to them.
                    var hasSetter = clrProp.GetSetMethod(true) != null;
                    if (!hasSetter && typeof(IEnumerable).IsAssignableFrom(clrProp.PropertyType) && clrProp.PropertyType != typeof(string))
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
