//-----------------------------------------------------------------------
// <copyright file="JsonSchema4.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Collections;
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    [JsonConverter(typeof(ExtensionDataDeserializationConverter))]
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

        /// <summary>Creates the serializer contract resolver based on the <see cref="SchemaType"/>.</summary>
        /// <param name="schemaType">The schema type.</param>
        /// <returns>The settings.</returns>
        public static PropertyRenameAndIgnoreSerializerContractResolver CreateJsonSerializerContractResolver(SchemaType schemaType)
        {
            var resolver = new IgnoreEmptyCollectionsContractResolver();

            if (schemaType == SchemaType.OpenApi3)
            {
                resolver.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readOnly");
                resolver.RenameProperty(typeof(JsonSchemaProperty), "x-writeOnly", "writeOnly");

                resolver.RenameProperty(typeof(JsonSchema), "x-nullable", "nullable");
                resolver.RenameProperty(typeof(JsonSchema), "x-example", "example");
                resolver.RenameProperty(typeof(JsonSchema), "x-deprecated", "deprecated");
            }
            else if (schemaType == SchemaType.Swagger2)
            {
                resolver.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readOnly");
                resolver.RenameProperty(typeof(JsonSchema), "x-example", "example");
            }
            else
            {
                resolver.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readonly");
            }

            return resolver;
        }

        /// <summary>Gets or sets the extension data (i.e. additional properties which are not directly defined by JSON Schema).</summary>
        [JsonExtensionData]
        public IDictionary<string, object?>? ExtensionData { get; set; }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext ctx)
        {
            Initialize();
        }

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
        [JsonProperty("discriminator", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = -100 + 5)]
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
                if (value is string)
                {
                    Discriminator = (string) value;
                }
                else if (value != null)
                {
                    DiscriminatorObject = ((JObject) value).ToObject<OpenApiDiscriminator>();
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
        [JsonProperty("exclusiveMaximum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal object? ExclusiveMaximumRaw
        {
            get => ExclusiveMaximum ?? (IsExclusiveMaximum ? (object) true : null);
            set
            {
                if (value is bool)
                {
                    IsExclusiveMaximum = (bool) value;
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
        [JsonProperty("exclusiveMinimum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal object? ExclusiveMinimumRaw
        {
            get => ExclusiveMinimum ?? (IsExclusiveMinimum ? (object) true : null);
            set
            {
                if (value is bool)
                {
                    IsExclusiveMinimum = (bool) value;
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

        [JsonProperty("additionalItems", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
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
                if (value is bool)
                {
                    AllowAdditionalItems = (bool) value;
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

        [JsonProperty("additionalProperties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
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
                        return new JObject(); // bool is not allowed in Swagger2
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
                if (value is bool)
                {
                    AllowAdditionalProperties = (bool) value;
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

        [JsonProperty("items", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
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
                if (value is JArray)
                {
                    Items = new ObservableCollection<JsonSchema>(((JArray) value).Select(FromJsonWithCurrentSettings));
                }
                else if (value != null)
                {
                    Item = FromJsonWithCurrentSettings(value);
                }
            }
        }

        private Lazy<object?>? _typeRaw;

        [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = -100 + 3)]
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
                if (value is JArray)
                {
                    Type = ((JArray) value).Aggregate(JsonObjectType.None, (type, token) => type | ConvertStringToJsonObjectType(token.ToString()));
                }
                else
                {
                    Type = ConvertStringToJsonObjectType(value as string);
                }

                ResetTypeRaw();
            }
        }

        [MemberNotNull(nameof(_typeRaw))]
        private void ResetTypeRaw()
        {
            _typeRaw = new Lazy<object?>(() =>
            {
                var flags = JsonObjectTypes
                    .Where(v => Type.HasFlag(v))
                    .ToArray();

                if (flags.Length > 1)
                {
                    return new JArray(flags.Select(f => new JValue(f.ToString().ToLowerInvariant())));
                }

                if (flags.Length == 1)
                {
                    return new JValue(flags[0].ToString().ToLowerInvariant());
                }

                return null;
            });
        }

        [JsonProperty("required", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<string>? RequiredPropertiesRaw
        {
            get => RequiredProperties != null && RequiredProperties.Count > 0 ? RequiredProperties : null;
            set => RequiredProperties = value ?? [];
        }

        [JsonProperty("properties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal IDictionary<string, JsonSchemaProperty>? PropertiesRaw
        {
            get => _properties != null && _properties.Count > 0 ? Properties : null;
            set => Properties = value != null ? new ObservableDictionary<string, JsonSchemaProperty>(value!) : [];
        }

        [JsonProperty("patternProperties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal IDictionary<string, JsonSchemaProperty>? PatternPropertiesRaw
        {
            get => _patternProperties is { Count: > 0 }
                ? PatternProperties.ToDictionary(p => p.Key, p => p.Value)
                : null;
            set => PatternProperties = value != null ? new ObservableDictionary<string, JsonSchemaProperty>(value!) : [];
        }

        [JsonProperty("definitions", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal IDictionary<string, JsonSchema>? DefinitionsRaw
        {
            get => Definitions != null && Definitions.Count > 0 ? Definitions : null;
            set => Definitions = value != null ? new ObservableDictionary<string, JsonSchema>(value!) : [];
        }

        /// <summary>Gets or sets the enumeration names (optional, draft v5). </summary>
        [JsonProperty("x-enum-names", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal Collection<string>? EnumerationNamesDashedRaw
        {
            get => EnumerationNamesRaw;
            set
            {
                if (EnumerationNamesRaw?.Count == 0 && value != null) {
                    EnumerationNamesRaw = value;
                }
            }
        }

        /// <summary>Gets or sets the enumeration names (optional, draft v5). </summary>
        [JsonProperty("x-enum-varnames", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal Collection<string>? EnumerationVarnamesRaw
        {
            get => EnumerationNamesRaw;
            set
            {
                if (EnumerationNamesRaw?.Count == 0 && value != null) {
                    EnumerationNamesRaw = value;
                }
            }
        }

        /// <summary>Gets or sets the enumeration names (optional, draft v5). </summary>
        [JsonProperty("x-enumNames", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal Collection<string>? EnumerationNamesRaw
        {
            get => EnumerationNames != null && EnumerationNames.Count > 0 ? EnumerationNames : null;
            set => EnumerationNames = value != null ? new ObservableCollection<string>(value) : [];
        }

        /// <summary>Gets or sets the enumeration descriptions (optional, draft v5). </summary>
        [JsonProperty("x-enum-descriptions", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal Collection<string?>? EnumerationDescriptionsDashedRaw
        {
            get => EnumerationDescriptionsRaw;
            set
            {
                if (EnumerationDescriptionsRaw?.Count == 0 && value != null) {
                    EnumerationDescriptionsRaw = value;
                }
            }
        }

        /// <summary>Gets or sets the enumeration descriptions (optional, draft v5). </summary>
        [JsonProperty("x-enumDescriptions", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal Collection<string?>? EnumerationDescriptionsRaw
        {
            get => EnumerationDescriptions != null && EnumerationDescriptions.Count > 0 ? EnumerationDescriptions : null;
            set => EnumerationDescriptions = value != null ? new ObservableCollection<string?>(value) : [];
        }

        [JsonProperty("enum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<object?>? EnumerationRaw
        {
            get => Enumeration != null && Enumeration.Count > 0 ? Enumeration : null;
            set => Enumeration = value != null ? new ObservableCollection<object?>(value) : [];
        }

        [JsonProperty("allOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<JsonSchema>? AllOfRaw
        {
            get => _allOf != null && _allOf.Count > 0 ? AllOf : null;
            set => AllOf = value != null ? new ObservableCollection<JsonSchema>(value) : [];
        }

        [JsonProperty("anyOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<JsonSchema>? AnyOfRaw
        {
            get => _anyOf != null && _anyOf.Count > 0 ? AnyOf : null;
            set => AnyOf = value != null ? new ObservableCollection<JsonSchema>(value) : [];
        }

        [JsonProperty("oneOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<JsonSchema>? OneOfRaw
        {
            get => _oneOf != null && _oneOf.Count > 0 ? OneOf : null;
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
                    for (var i = 0; i < keysToRemove.Count; i++)
                    {
                        var key = keysToRemove[i];
                        collection.Remove(key);
                    }
                }
            }
        }
    }
}