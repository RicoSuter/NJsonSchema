//-----------------------------------------------------------------------
// <copyright file="SchemaSerializationConverter.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace NJsonSchema.Infrastructure
{
    /// <summary>A converter factory that supports property renaming and ignoring per type,
    /// replacing the Newtonsoft PropertyRenameAndIgnoreSerializerContractResolver pattern.</summary>
    public class SchemaSerializationConverter : JsonConverterFactory
    {
        private readonly Dictionary<string, HashSet<string>> _ignores = [];
        private readonly Dictionary<string, Dictionary<string, string>> _renames = [];
        private readonly bool _ignoreEmptyCollections;

        /// <summary>Initializes a new instance of the <see cref="SchemaSerializationConverter"/> class.</summary>
        /// <param name="ignoreEmptyCollections">Whether to skip empty collections during serialization.</param>
        public SchemaSerializationConverter(bool ignoreEmptyCollections = true)
        {
            _ignoreEmptyCollections = ignoreEmptyCollections;
        }

        /// <summary>Ignore the given property/properties of the given type.</summary>
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
        }

        /// <summary>Determines whether this converter can convert the specified type.</summary>
        public override bool CanConvert(Type typeToConvert)
        {
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
            private static readonly ConcurrentDictionary<JsonSerializerOptions, JsonSerializerOptions> StrippedOptionsCache = new();

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
                // For read, parse to JsonNode, apply reverse renames, then deserialize without this converter
                var node = JsonNode.Parse(ref reader);
                if (node is not JsonObject obj)
                {
                    return default;
                }

                // Apply reverse renames (new name -> original name)
                if (_renames != null)
                {
                    foreach (var kvp in _renames)
                    {
                        if (obj.ContainsKey(kvp.Value) && !obj.ContainsKey(kvp.Key))
                        {
                            var value = obj[kvp.Value];
                            obj.Remove(kvp.Value);
                            obj[kvp.Key] = value?.DeepClone();
                        }
                    }
                }

                var optionsWithout = GetOrCreateOptionsWithout(options);
                return node.Deserialize<T>(optionsWithout);
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                var optionsWithout = GetOrCreateOptionsWithout(options);
                var node = JsonSerializer.SerializeToNode(value, optionsWithout);

                if (node is JsonObject obj)
                {
                    // Apply ignores
                    if (_ignores != null)
                    {
                        foreach (var prop in _ignores)
                        {
                            obj.Remove(prop);
                        }
                    }

                    // Apply renames
                    if (_renames != null)
                    {
                        foreach (var kvp in _renames)
                        {
                            if (obj.ContainsKey(kvp.Key))
                            {
                                var val = obj[kvp.Key];
                                obj.Remove(kvp.Key);
                                obj[kvp.Value] = val?.DeepClone();
                            }
                        }
                    }
                }

                node?.WriteTo(writer, options);
            }

            private static JsonSerializerOptions GetOrCreateOptionsWithout(JsonSerializerOptions options)
            {
                return StrippedOptionsCache.GetOrAdd(options, static parentOptions =>
                {
                    var newOptions = new JsonSerializerOptions(parentOptions);
                    for (var i = newOptions.Converters.Count - 1; i >= 0; i--)
                    {
                        if (newOptions.Converters[i] is SchemaSerializationConverter)
                        {
                            newOptions.Converters.RemoveAt(i);
                        }
                    }
                    return newOptions;
                });
            }
        }
    }
}
