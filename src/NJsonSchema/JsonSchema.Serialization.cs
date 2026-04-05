//-----------------------------------------------------------------------
// <copyright file="JsonSchema4.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Namotion.Reflection;
using System.Text.Json.Serialization;
using NJsonSchema.Collections;
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    public partial class JsonSchema : IJsonExtensionObject
    {
        internal static readonly List<JsonObjectType> JsonObjectTypes =
#if NET8_0_OR_GREATER
            Enum.GetValues<JsonObjectType>()
#else
            Enum.GetValues(typeof(JsonObjectType)).Cast<JsonObjectType>()
#endif
            .Where(static v => v != JsonObjectType.None)
            .ToList();


        // keep a reference so we don't need to create a delegate each time
        private readonly NotifyCollectionChangedEventHandler _initializeSchemaCollectionEventHandler;

        /// <summary>Creates the serializer options with schema type-specific property renaming.</summary>
        /// <param name="schemaType">The schema type.</param>
        /// <returns>The converter (which can be further configured with IgnoreProperty/RenameProperty).</returns>
        public static SchemaSerializationConverter CreateSchemaSerializationConverter(SchemaType schemaType)
        {
            var converter = new SchemaSerializationConverter();

            if (schemaType == SchemaType.OpenApi3)
            {
                converter.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readOnly");
                converter.RenameProperty(typeof(JsonSchemaProperty), "x-writeOnly", "writeOnly");

                converter.RenameProperty(typeof(JsonSchema), "x-nullable", "nullable");
                converter.RenameProperty(typeof(JsonSchema), "x-example", "example");
                converter.RenameProperty(typeof(JsonSchema), "x-deprecated", "deprecated");
            }
            else if (schemaType == SchemaType.Swagger2)
            {
                converter.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readOnly");
                converter.RenameProperty(typeof(JsonSchema), "x-example", "example");
            }
            else
            {
                converter.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readonly");
            }

            return converter;
        }

        /// <summary>Gets or sets the extension data (i.e. additional properties which are not directly defined by JSON Schema).</summary>
        [JsonExtensionData]
        public IDictionary<string, object?>? ExtensionData { get; set; }

        /// <summary>Gets the discriminator property (Swagger only).</summary>
        [JsonIgnore]
        public string? ActualDiscriminator => ActualTypeSchema.Discriminator;

        /// <summary>Gets or sets the discriminator property (Swagger only, should not be used in internal tooling).</summary>
        [JsonIgnore]
        public string? Discriminator
        {
            get => DiscriminatorObject?.PropertyName;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    DiscriminatorObject = new OpenApiDiscriminator
                    {
                        PropertyName = value
                    };
                }
                else
                {
                    DiscriminatorObject = null;
                }
            }
        }

        /// <summary>Gets the actual resolved discriminator of this schema (no inheritance, OpenApi only).</summary>
        [JsonIgnore]
        public OpenApiDiscriminator? ActualDiscriminatorObject => DiscriminatorObject ?? ActualTypeSchema.DiscriminatorObject;

        /// <summary>Gets or sets the discriminator of this schema (OpenApi only).</summary>
        [JsonIgnore]
        public OpenApiDiscriminator? DiscriminatorObject { get; set; }

        /// <summary>Gets or sets the discriminator.</summary>
        [JsonPropertyName("discriminator")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyOrder(-95)]
        [JsonInclude]
        internal object? DiscriminatorRaw
        {
            get
            {
                if (JsonSchemaSerialization.CurrentSchemaType != SchemaType.Swagger2)
                {
                    return DiscriminatorObject;
                }
                else
                {
                    return Discriminator;
                }
            }
            set
            {
                if (value is string stringValue)
                {
                    Discriminator = stringValue;
                }
                else if (value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        Discriminator = element.GetString();
                    }
                    else if (element.ValueKind == JsonValueKind.Object)
                    {
                        DiscriminatorObject = element.Deserialize<OpenApiDiscriminator>();
                    }
                }
                else if (value != null)
                {
                    DiscriminatorObject = JsonSerializer.Deserialize<OpenApiDiscriminator>(
                        JsonSerializer.Serialize(value));
                }
            }
        }

        /// <summary>Gets or sets the enumeration names (optional, draft v5). </summary>
        [JsonIgnore]
        public Collection<string> EnumerationNames { get; set; }

        /// <summary>Gets or sets the enumeration descriptions (optional, draft v5). </summary>
        [JsonIgnore]
        public Collection<string?> EnumerationDescriptions { get; set; }

        /// <summary>Gets or sets a value indicating whether the maximum value is excluded. </summary>
        [JsonPropertyName("exclusiveMaximum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal object? ExclusiveMaximumRaw
        {
            get => ExclusiveMaximum ?? (IsExclusiveMaximum ? (object) true : null);
            set
            {
                if (value is bool boolValue)
                {
                    IsExclusiveMaximum = boolValue;
                }
                else if (value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.True)
                    {
                        IsExclusiveMaximum = true;
                    }
                    else if (element.ValueKind == JsonValueKind.False)
                    {
                        IsExclusiveMaximum = false;
                    }
                    else if (element.ValueKind == JsonValueKind.Number)
                    {
                        ExclusiveMaximum = element.GetDecimal();
                    }
                }
                else if (value != null && (value.Equals("true") || value.Equals("false")))
                {
                    IsExclusiveMaximum = value.Equals("true");
                }
                else if (value != null)
                {
                    ExclusiveMaximum = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>Gets or sets a value indicating whether the minimum value is excluded. </summary>
        [JsonPropertyName("exclusiveMinimum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal object? ExclusiveMinimumRaw
        {
            get => ExclusiveMinimum ?? (IsExclusiveMinimum ? (object) true : null);
            set
            {
                if (value is bool boolValue)
                {
                    IsExclusiveMinimum = boolValue;
                }
                else if (value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.True)
                    {
                        IsExclusiveMinimum = true;
                    }
                    else if (element.ValueKind == JsonValueKind.False)
                    {
                        IsExclusiveMinimum = false;
                    }
                    else if (element.ValueKind == JsonValueKind.Number)
                    {
                        ExclusiveMinimum = element.GetDecimal();
                    }
                }
                else if (value != null && (value.Equals("true") || value.Equals("false")))
                {
                    IsExclusiveMinimum = value.Equals("true");
                }
                else if (value != null)
                {
                    ExclusiveMinimum = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                }
            }
        }

        [JsonPropertyName("additionalItems")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal object? AdditionalItemsRaw
        {
            get
            {
                if (AdditionalItemsSchema != null)
                {
                    return AdditionalItemsSchema;
                }

                if (!AllowAdditionalItems)
                {
                    return false;
                }

                return null;
            }
            set
            {
                if (value is bool boolValue)
                {
                    AllowAdditionalItems = boolValue;
                }
                else if (value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.True)
                    {
                        AllowAdditionalItems = true;
                    }
                    else if (element.ValueKind == JsonValueKind.False)
                    {
                        AllowAdditionalItems = false;
                    }
                    else
                    {
                        AdditionalItemsSchema = FromJsonWithCurrentSettings(element);
                    }
                }
                else if (value != null && (value.Equals("true") || value.Equals("false")))
                {
                    AllowAdditionalItems = value.Equals("true");
                }
                else if (value != null)
                {
                    AdditionalItemsSchema = FromJsonWithCurrentSettings(value);
                }
            }
        }

        [JsonPropertyName("additionalProperties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal object? AdditionalPropertiesRaw
        {
            get
            {
                if (AdditionalPropertiesSchema != null)
                {
                    return AdditionalPropertiesSchema;
                }

                if (JsonSchemaSerialization.CurrentSchemaType == SchemaType.Swagger2)
                {
                    if (AllowAdditionalProperties &&
                        (Type.IsObject() || Type == JsonObjectType.None) &&
                        !HasReference &&
                        !_allOf.Any() &&
                        !GetType().IsAssignableToTypeName("OpenApiParameter", TypeNameStyle.Name))
                    {
                        return new JsonObject(); // bool is not allowed in Swagger2
                    }
                    else
                    {
                        return null; // default in Swagger2 is to not allow additional properties
                    }
                }
                else
                {
                    if (!AllowAdditionalProperties)
                    {
                        return false;
                    }
                    else
                    {
                        return null; // default in JSON Schema/OpenAPI3 is to allow additional properties
                    }
                }
            }
            set
            {
                if (value is bool boolValue)
                {
                    AllowAdditionalProperties = boolValue;
                }
                else if (value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.True)
                    {
                        AllowAdditionalProperties = true;
                    }
                    else if (element.ValueKind == JsonValueKind.False)
                    {
                        AllowAdditionalProperties = false;
                    }
                    else if (element.ValueKind == JsonValueKind.Object && element.EnumerateObject().Any() == false)
                    {
                        AllowAdditionalProperties = true; // empty object = allow in Swagger2
                    }
                    else
                    {
                        AdditionalPropertiesSchema = FromJsonWithCurrentSettings(element);
                    }
                }
                else if (value != null && (value.Equals("true") || value.Equals("false")))
                {
                    AllowAdditionalProperties = value.Equals("true");
                }
                else if (value != null)
                {
                    AdditionalPropertiesSchema = FromJsonWithCurrentSettings(value);
                }
            }
        }

        [JsonPropertyName("items")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal object? ItemsRaw
        {
            get
            {
                if (Item != null)
                {
                    return Item;
                }

                if (Items.Count > 0)
                {
                    return Items;
                }

                return null;
            }
            set
            {
                if (value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        Items = new ObservableCollection<JsonSchema>(
                            element.EnumerateArray().Select(e => FromJsonWithCurrentSettings(e)));
                    }
                    else
                    {
                        Item = FromJsonWithCurrentSettings(element);
                    }
                }
                else if (value is IList<JsonSchema> list)
                {
                    Items = new ObservableCollection<JsonSchema>(list);
                }
                else if (value != null)
                {
                    Item = FromJsonWithCurrentSettings(value);
                }
            }
        }

        private Lazy<object?>? _typeRaw;

        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyOrder(-97)]
        [JsonInclude]
        internal object? TypeRaw
        {
            get
            {
                if (_typeRaw is null)
                {
                    ResetTypeRaw();
                }

                return _typeRaw.Value;
            }
            set
            {
                if (value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        Type = element.EnumerateArray()
                            .Aggregate(JsonObjectType.None, (type, el) => type | ConvertStringToJsonObjectType(el.GetString()));
                    }
                    else
                    {
                        Type = ConvertStringToJsonObjectType(element.GetString());
                    }
                }
                else if (value is string[] array)
                {
                    Type = array.Aggregate(JsonObjectType.None, (type, s) => type | ConvertStringToJsonObjectType(s));
                }
                else
                {
                    Type = ConvertStringToJsonObjectType(value as string);
                }
            }
        }

        [MemberNotNull(nameof(_typeRaw))]
        private void ResetTypeRaw()
        {
            _typeRaw = new Lazy<object?>(() =>
            {
                var flags = JsonObjectTypes
                    .Where(x => Type.HasFlag(x))
                    .Select(x => x.ToString().ToLowerInvariant())
                    .ToArray();

                return flags.Length switch
                {
                    > 1 => flags, // string array → serializes as JSON array
                    1 => flags[0], // single string → serializes as JSON string
                    _ => null
                };
            });
        }

        [JsonPropertyName("required")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal object? RequiredPropertiesRaw
        {
            get => RequiredProperties is { Count: > 0 } ? RequiredProperties : null;
            set
            {
                // Handle both JSON Schema "required" (string array) and OpenAPI parameter
                // "required" (boolean) which share the same JSON property name. When loaded
                // as a plain JsonSchema (e.g., external file references), the boolean value
                // should be silently ignored.
                if (value is ICollection<string> stringCollection)
                {
                    RequiredProperties = stringCollection;
                }
                else if (value is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    RequiredProperties = element.EnumerateArray().Select(e => e.GetString()!).ToList();
                }
                else if (value is IEnumerable<object?> objects)
                {
                    RequiredProperties = objects.Select(o => o?.ToString() ?? "").ToList();
                }
                else
                {
                    // Boolean or other non-array value — silently ignore (parameter "required": true)
                    RequiredProperties = [];
                }
            }
        }

        [JsonPropertyName("properties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal IDictionary<string, JsonSchemaProperty>? PropertiesRaw
        {
            get => _properties is { Count: > 0 } ? Properties : null;
            set => Properties = value != null ? new ObservableDictionary<string, JsonSchemaProperty>(value!) : [];
        }

        [JsonPropertyName("patternProperties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal IDictionary<string, JsonSchemaProperty>? PatternPropertiesRaw
        {
            get => _patternProperties is { Count: > 0 }
                ? PatternProperties.ToDictionary(p => p.Key, p => p.Value)
                : null;
            set => PatternProperties = value != null ? new ObservableDictionary<string, JsonSchemaProperty>(value!) : [];
        }

        [JsonPropertyName("definitions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal IDictionary<string, JsonSchema>? DefinitionsRaw
        {
            get => Definitions is { Count: > 0 } ? Definitions : null;
            set => Definitions = value != null ? new ObservableDictionary<string, JsonSchema>(value!) : [];
        }

        /// <summary>Gets or sets the enumeration names (used for deserialization only).</summary>
        [JsonPropertyName("x-enum-names")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal Collection<string>? EnumerationNamesDashedRaw
        {
            get => null;
            set
            {
                if (EnumerationNamesRaw?.Count == 0 && value?.Count > 0)
                {
                    EnumerationNamesRaw = new Collection<string>(value);
                }
            }
        }

        /// <summary>Gets or sets the enumeration names (used for deserialization only).</summary>
        [JsonPropertyName("x-enum-varnames")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal Collection<string>? EnumerationVarNamesRaw
        {
            get => null;
            set
            {
                if (EnumerationNamesRaw?.Count == 0 && value?.Count > 0)
                {
                    EnumerationNamesRaw = new Collection<string>(value);
                }
            }
        }

        /// <summary>Gets or sets the enumeration names (optional, draft v5).</summary>
        [JsonPropertyName("x-enumNames")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal Collection<string>? EnumerationNamesRaw
        {
            get => EnumerationNames is { Count: > 0 } ? EnumerationNames : null;
            set => EnumerationNames = value != null ? new ObservableCollection<string>(value) : [];
        }

        /// <summary>Gets or sets the enumeration descriptions (used for deserialization only).</summary>
        [JsonPropertyName("x-enumDescriptions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal string[]? EnumerationDescriptionsRaw
        {
            get => null;
            set => EnumerationDescriptionsDashedRaw = value;
        }

        /// <summary>Gets or sets the enumeration descriptions (optional, draft v5).</summary>
        [JsonPropertyName("x-enum-descriptions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal string?[]? EnumerationDescriptionsDashedRaw
        {
            get => EnumerationDescriptions is { Count: > 0 } ? EnumerationDescriptions.ToArray() : null;
            set
            {
                var converted = ConvertPossibleStringArray(value);
                if (converted != null)
                {
                    EnumerationDescriptions = new ObservableCollection<string?>(converted);
                }
            }
        }

        [JsonPropertyName("enum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal ICollection<object?>? EnumerationRaw
        {
            get => Enumeration is { Count: > 0 } ? Enumeration : null;
            set => Enumeration = value != null ? new ObservableCollection<object?>(value) : [];
        }

        [JsonPropertyName("allOf")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal ICollection<JsonSchema>? AllOfRaw
        {
            get => _allOf is { Count: > 0 } ? AllOf : null;
            set => AllOf = value != null ? new ObservableCollection<JsonSchema>(value) : [];
        }

        [JsonPropertyName("anyOf")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal ICollection<JsonSchema>? AnyOfRaw
        {
            get => _anyOf is { Count: > 0 } ? AnyOf : null;
            set => AnyOf = value != null ? new ObservableCollection<JsonSchema>(value) : [];
        }

        [JsonPropertyName("oneOf")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonInclude]
        internal ICollection<JsonSchema>? OneOfRaw
        {
            get => _oneOf is { Count: > 0 } ? OneOf : null;
            set => OneOf = value != null ? new ObservableCollection<JsonSchema>(value) : [];
        }

        private void RegisterProperties(ObservableDictionary<string, JsonSchemaProperty>? oldCollection, ObservableDictionary<string, JsonSchemaProperty>? newCollection)
        {
            if (oldCollection != null)
            {
                oldCollection.CollectionChanged -= _initializeSchemaCollectionEventHandler;
            }

            if (newCollection != null)
            {
                newCollection.CollectionChanged += _initializeSchemaCollectionEventHandler;
                InitializeSchemaCollection(newCollection, null);
            }
        }

        private void RegisterSchemaDictionary<T>(ObservableDictionary<string, T>? oldCollection, ObservableDictionary<string, T>? newCollection)
            where T : JsonSchema
        {
            if (oldCollection != null)
            {
                oldCollection.CollectionChanged -= _initializeSchemaCollectionEventHandler;
            }

            if (newCollection != null)
            {
                newCollection.CollectionChanged += _initializeSchemaCollectionEventHandler;
                InitializeSchemaCollection(newCollection, null);
            }
        }

        private void RegisterSchemaCollection(ObservableCollection<JsonSchema>? oldCollection, ObservableCollection<JsonSchema>? newCollection)
        {
            if (oldCollection != null)
            {
                oldCollection.CollectionChanged -= _initializeSchemaCollectionEventHandler;
            }

            if (newCollection != null)
            {
                newCollection.CollectionChanged += _initializeSchemaCollectionEventHandler;
                InitializeSchemaCollection(newCollection, null);
            }
        }

        private void InitializeSchemaCollection(object? sender, NotifyCollectionChangedEventArgs? args)
        {
            if (sender is ObservableDictionary<string, JsonSchemaProperty> { Count: > 0 } properties)
            {
                foreach (var property in properties)
                {
                    property.Value!.Name = property.Key;
                    property.Value.Parent = this;
                }
            }
            else if (sender is ObservableCollection<JsonSchema> { Count: > 0 } items)
            {
                foreach (var item in items)
                {
                    item.Parent = this;
                }
            }
            else if (sender is ObservableDictionary<string, JsonSchema> { Count: > 0 } collection)
            {
                List<string>? keysToRemove = null;
                foreach (var pair in collection)
                {
                    if (pair.Value == null)
                    {
                        keysToRemove ??= [];
                        keysToRemove.Add(pair.Key);
                    }
                    else
                    {
                        pair.Value.Parent = this;
                    }
                }

                if (keysToRemove != null)
                {
                    foreach (var key in keysToRemove)
                    {
                        collection.Remove(key);
                    }
                }
            }
        }

        private static List<string?>? ConvertPossibleStringArray(string?[]? array)
        {
            if (array is { Length: > 0 })
            {
                return new List<string?>(array);
            }

            return null;
        }
    }
}
