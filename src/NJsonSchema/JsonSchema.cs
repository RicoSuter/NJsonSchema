//-----------------------------------------------------------------------
// <copyright file="JsonSchema4.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Collections;
using NJsonSchema.Generation;
using NJsonSchema.Infrastructure;
using NJsonSchema.Validation;

namespace NJsonSchema
{
    /// <summary>A base class for describing a JSON schema. </summary>
    public partial class JsonSchema : IDocumentPathProvider
    {
        internal static readonly HashSet<string> JsonSchemaPropertiesCache =
            [..typeof(JsonSchema).GetContextualProperties().Select(p => p.Name).ToArray()];

        private const SchemaType SerializationSchemaType = SchemaType.JsonSchema;

        private static readonly Lazy<PropertyRenameAndIgnoreSerializerContractResolver> ContractResolver = new(() => CreateJsonSerializerContractResolver(SerializationSchemaType));

        private ObservableDictionary<string, JsonSchemaProperty> _properties;
        private ObservableDictionary<string, JsonSchemaProperty> _patternProperties;
        private ObservableDictionary<string, JsonSchema> _definitions;

        internal ObservableCollection<JsonSchema> _allOf;
        internal ObservableCollection<JsonSchema> _anyOf;
        internal ObservableCollection<JsonSchema> _oneOf;
        private JsonSchema? _not;
        private JsonSchema? _dictionaryKey;

        private JsonObjectType _type;
        private JsonSchema? _item;
        internal ObservableCollection<JsonSchema> _items;

        private bool _allowAdditionalItems = true;
        private JsonSchema? _additionalItemsSchema;

        private bool _allowAdditionalProperties = true;
        private JsonSchema? _additionalPropertiesSchema;

        /// <summary>Initializes a new instance of the <see cref="JsonSchema"/> class. </summary>
        public JsonSchema()
        {
            _initializeSchemaCollectionEventHandler = InitializeSchemaCollection;

            Initialize();

            if (JsonSchemaSerialization.CurrentSchemaType == SchemaType.Swagger2)
            {
                _allowAdditionalProperties = false; // the default for Swagger2 is false (change required when deserializing)
            }
        }

        /// <summary>Creates a schema which matches any data.</summary>
        /// <returns>The any schema.</returns>
        public static JsonSchema CreateAnySchema()
        {
            return new JsonSchema();
        }

        /// <summary>Creates a schema which matches any data.</summary>
        /// <returns>The any schema.</returns>
        public static TSchemaType CreateAnySchema<TSchemaType>()
            where TSchemaType : JsonSchema, new()
        {
            return new TSchemaType();
        }

        /// <summary>Gets the NJsonSchema toolchain version.</summary>
        public static string ToolchainVersion => version;

        private static readonly string version = typeof(JsonSchema).GetTypeInfo().Assembly.GetName().Version +
            " (Newtonsoft.Json v" + typeof(JToken).GetTypeInfo().Assembly.GetName().Version + ")";

        /// <summary>Loads a JSON Schema from a given file path (only available in .NET 4.x).</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="cancellationToken">Cancellation token instance</param>
        /// <returns>The JSON Schema.</returns>
        public static Task<JsonSchema> FromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            return FromFileAsync(filePath, factory, cancellationToken);
        }

        /// <summary>Loads a JSON Schema from a given file path (only available in .NET 4.x).</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The System.IO.File API is not available on this platform.</exception>
        public static Task<JsonSchema> FromFileAsync(string filePath, Func<JsonSchema, JsonReferenceResolver> referenceResolverFactory, CancellationToken cancellationToken = default)
        {
            using var stream = File.OpenRead(filePath);
            return FromJsonAsync(stream, filePath, referenceResolverFactory, cancellationToken);
        }

        /// <summary>Loads a JSON Schema from a given URL (only available in .NET 4.x).</summary>
        /// <param name="url">The URL to the document.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The HttpClient.GetAsync API is not available on this platform.</exception>
        public static Task<JsonSchema> FromUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            return FromUrlAsync(url, factory, cancellationToken);
        }

        /// <summary>Loads a JSON Schema from a given URL (only available in .NET 4.x).</summary>
        /// <param name="url">The URL to the document.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The HttpClient.GetAsync API is not available on this platform.</exception>
        public static async Task<JsonSchema> FromUrlAsync(string url, Func<JsonSchema, JsonReferenceResolver> referenceResolverFactory, CancellationToken cancellationToken = default)
        {
            var data = await DynamicApis.HttpGetAsync(url, cancellationToken).ConfigureAwait(false);
            return await FromJsonAsync(data, url, referenceResolverFactory,cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema"/>. </summary>
        /// <param name="data">The JSON string. </param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        public static Task<JsonSchema> FromJsonAsync(string data, CancellationToken cancellationToken = default)
        {
            return FromJsonAsync(data, null, cancellationToken);
        }

        /// <summary>Deserializes a JSON stream to a <see cref="JsonSchema"/>. </summary>
        /// <param name="stream">The JSON data stream.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        public static Task<JsonSchema> FromJsonAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            return FromJsonAsync(stream, null, factory, cancellationToken);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema"/>. </summary>
        /// <param name="data">The JSON string. </param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        public static Task<JsonSchema> FromJsonAsync(string data, string? documentPath, CancellationToken cancellationToken = default)
        {
            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            return FromJsonAsync(data, documentPath, factory, cancellationToken);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema" />.</summary>
        /// <param name="data">The JSON string.</param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        public static Task<JsonSchema> FromJsonAsync(string data, string? documentPath, Func<JsonSchema,
            JsonReferenceResolver> referenceResolverFactory, CancellationToken cancellationToken = default)
        {
            return JsonSchemaSerialization.FromJsonAsync(data, SerializationSchemaType, documentPath, referenceResolverFactory, ContractResolver.Value, cancellationToken);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema" />.</summary>
        /// <param name="stream">The JSON data stream.</param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        public static Task<JsonSchema> FromJsonAsync(Stream stream, string? documentPath, Func<JsonSchema,
            JsonReferenceResolver> referenceResolverFactory, CancellationToken cancellationToken = default)
        {
            return JsonSchemaSerialization.FromJsonAsync(stream, SerializationSchemaType, documentPath, referenceResolverFactory, ContractResolver.Value, cancellationToken);
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type (using System.Text.Json rules).</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>()
        {
            return FromType<TType>(new SystemTextJsonSchemaGeneratorSettings());
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type (using System.Text.Json rules).</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type)
        {
            return FromType(type, new SystemTextJsonSchemaGeneratorSettings());
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>(JsonSchemaGeneratorSettings settings)
        {
            var generator = new JsonSchemaGenerator(settings);
            return generator.Generate(typeof(TType));
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type, JsonSchemaGeneratorSettings settings)
        {
            var generator = new JsonSchemaGenerator(settings);
            return generator.Generate(type);
        }

        /// <summary>
        /// Generates a JSON Schema from sample JSON data.
        /// </summary>
        /// <param name="data">The sample JSON data.</param>
        /// <returns>The JSON Schema.</returns>
        public static JsonSchema FromSampleJson(string data)
        {
            var generator = new SampleJsonSchemaGenerator();
            return generator.Generate(data);
        }

        internal static JsonSchema FromJsonWithCurrentSettings(object obj)
        {
            var json = JsonConvert.SerializeObject(obj, JsonSchemaSerialization.CurrentSerializerSettings);
            return JsonConvert.DeserializeObject<JsonSchema>(json, JsonSchemaSerialization.CurrentSerializerSettings)!;
        }

        /// <summary>Gets a value indicating whether the schema is binary (file or binary format).</summary>
        [JsonIgnore]
        public bool IsBinary => Type.IsFile() || (Type.IsString() && Format == JsonFormatStrings.Binary);

        /// <summary>Gets the inherited/parent schema (most probable base schema in allOf).</summary>
        /// <remarks>Used for code generation.</remarks>
        [JsonIgnore]
        public JsonSchema? InheritedSchema
        {
            get
            {
                if (_allOf == null || _allOf.Count == 0 || HasReference)
                {
                    return null;
                }

                if (_allOf.Count == 1)
                {
                    return _allOf[0].ActualSchema;
                }

                var hasReference = _allOf.FirstOrDefault(s => s.HasReference);
                if (hasReference != null)
                {
                    return hasReference.ActualSchema;
                }

                var objectTyped = _allOf.FirstOrDefault(s => s.Type.IsObject());
                if (objectTyped != null)
                {
                    return objectTyped.ActualSchema;
                }

                return _allOf.FirstOrDefault()?.ActualSchema;
            }
        }

        /// <summary>Gets the inherited/parent schema which may also be inlined
        /// (the schema itself if it is a dictionary or array, otherwise <see cref="InheritedSchema"/>).</summary>
        /// <remarks>Used for code generation.</remarks>
        [JsonIgnore]
        public JsonSchema? InheritedTypeSchema
        {
            get
            {
                if (InheritedSchema == null && (ActualTypeSchema.IsDictionary || ActualTypeSchema.IsArray || ActualTypeSchema.IsTuple))
                {
                    return ActualTypeSchema;
                }

                return InheritedSchema;
            }
        }

        /// <summary>Gets the list of all inherited/parent schemas.</summary>
        /// <remarks>Used for code generation.</remarks>
        [JsonIgnore]
        public IReadOnlyCollection<JsonSchema> AllInheritedSchemas => InheritedSchema != null ? [InheritedSchema, ..InheritedSchema.AllInheritedSchemas] : [];

        /// <summary>Determines whether the given schema is the parent schema of this schema (i.e. super/base class).</summary>
        /// <param name="schema">The possible subtype schema.</param>
        /// <returns>true or false</returns>
        public bool Inherits(JsonSchema schema)
        {
            schema = schema.ActualSchema;
            return InheritedSchema?.ActualSchema == schema || InheritedSchema?.Inherits(schema) == true;
        }

        /// <summary>Gets the discriminator or discriminator of an inherited schema (or null).</summary>
        [JsonIgnore]
        public OpenApiDiscriminator? ResponsibleDiscriminatorObject =>
            ActualDiscriminatorObject ?? InheritedSchema?.ActualSchema.ResponsibleDiscriminatorObject;

        /// <summary>
        /// Calculates whether <see cref="ActualProperties"/> has elements without incurring collection building
        /// performance cost.
        /// </summary>
        [JsonIgnore]
        public bool HasActualProperties
        {
            get
            {
                if (_properties.Count > 0)
                {
                    return true;
                }

                for (var i = 0; i < _allOf.Count; i++)
                {
                    var s = _allOf[i];
                    if (s.ActualSchema != InheritedSchema && s.ActualSchema.HasActualProperties)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Gets all properties of this schema (i.e. all direct properties and properties from the schemas in allOf which do not have a type).</summary>
        /// <remarks>Used for code generation.</remarks>
        /// <exception cref="InvalidOperationException" accessor="get">Some properties are defined multiple times.</exception>
        [JsonIgnore]
        public IReadOnlyDictionary<string, JsonSchemaProperty> ActualProperties
        {
            get
            {
                // check fast case
                if (_allOf.Count == 0)
                {
                    return new Dictionary<string, JsonSchemaProperty>(_properties);
                }

                var properties = _properties
                    .Union(
                        _allOf
                            .Where(s => s.ActualSchema != InheritedSchema)
                            .SelectMany(s => s.ActualSchema.ActualProperties)
                    );

                try
                {
                    return properties.ToDictionary(p => p.Key, p => p.Value);
                }
                catch (ArgumentException)
                {
                    var duplicatedProperties = properties
                        .GroupBy(p => p.Key)
                        .Where(g => g.Count() > 1);

                    throw new InvalidOperationException("The properties " + string.Join(", ", duplicatedProperties.Select(g => "'" + g.Key + "'")) + " are defined multiple times.");
                }
            }
        }

        /// <summary>Gets or sets the schema. </summary>
        [JsonProperty("$schema", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = -100 + 1)]
        public string? SchemaVersion { get; set; }

        /// <summary>Gets or sets the id. </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = -100 + 2)]
        public string? Id { get; set; }

        /// <summary>Gets or sets the title. </summary>
        [JsonProperty("title", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = -100 + 3)]
        public string? Title { get; set; }

        /// <summary>Gets a value indicating whether the schema title can be used as type name.</summary>
        [JsonIgnore]
        public bool HasTypeNameTitle => !string.IsNullOrEmpty(Title) && Regex.IsMatch(Title, "^[a-zA-Z0-9_]*$");

        /// <summary>Gets or sets the description. </summary>
        [JsonProperty("description", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public virtual string? Description { get; set; }

        /// <summary>Gets the object types (as enum flags). </summary>
        [JsonIgnore]
        public JsonObjectType Type
        {
            get => _type; set
            {
                _type = value;
                ResetTypeRaw();
            }
        }

        /// <summary>Gets the parent schema of this schema. </summary>
        [JsonIgnore]
        public JsonSchema? ParentSchema => Parent as JsonSchema;

        /// <summary>Gets the parent schema of this schema. </summary>
        [JsonIgnore]
        public virtual object? Parent { get; set; }

        /// <summary>Gets or sets the format string. </summary>
        [JsonProperty("format", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string? Format { get; set; }

        /// <summary>Gets or sets the default value. </summary>
        [JsonProperty("default", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public object? Default { get; set; }

        /// <summary>Gets or sets the required multiple of for the number value.</summary>
        [JsonProperty("multipleOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public decimal? MultipleOf { get; set; }

        /// <summary>Gets or sets the maximum allowed value.</summary>
        [JsonProperty("maximum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public decimal? Maximum { get; set; }

        /// <summary>Gets or sets the exclusive maximum value (v6).</summary>
        [JsonIgnore]
        public decimal? ExclusiveMaximum { get; set; }

        /// <summary>Gets or sets a value indicating whether the minimum value is excluded (v4).</summary>
        [JsonIgnore]
        public bool IsExclusiveMaximum { get; set; }

        /// <summary>Gets or sets the minimum allowed value. </summary>
        [JsonProperty("minimum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public decimal? Minimum { get; set; }

        /// <summary>Gets or sets the exclusive minimum value (v6).</summary>
        [JsonIgnore]
        public decimal? ExclusiveMinimum { get; set; }

        /// <summary>Gets or sets a value indicating whether the minimum value is excluded (v4).</summary>
        [JsonIgnore]
        public bool IsExclusiveMinimum { get; set; }

        /// <summary>Gets or sets the maximum length of the value string. </summary>
        [JsonProperty("maxLength", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int? MaxLength { get; set; }

        /// <summary>Gets or sets the minimum length of the value string. </summary>
        [JsonProperty("minLength", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int? MinLength { get; set; }

        /// <summary>Gets or sets the validation pattern as regular expression. </summary>
        [JsonProperty("pattern", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string? Pattern { get; set; }

        /// <summary>Gets or sets the maximum length of the array. </summary>
        [JsonProperty("maxItems", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MaxItems { get; set; }

        /// <summary>Gets or sets the minimum length of the array. </summary>
        [JsonProperty("minItems", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MinItems { get; set; }

        /// <summary>Gets or sets a value indicating whether the items in the array must be unique. </summary>
        [JsonProperty("uniqueItems", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool UniqueItems { get; set; }

        /// <summary>Gets or sets the maximal number of allowed properties in an object. </summary>
        [JsonProperty("maxProperties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MaxProperties { get; set; }

        /// <summary>Gets or sets the minimal number of allowed properties in an object. </summary>
        [JsonProperty("minProperties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MinProperties { get; set; }

        /// <summary>Gets or sets a value indicating whether the schema is deprecated (native in Open API 'deprecated', custom in Swagger/JSON Schema 'x-deprecated').</summary>
        [JsonProperty("x-deprecated", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsDeprecated { get; set; }

        /// <summary>Gets or sets a message indicating why the schema is deprecated (custom extension, sets 'x-deprecatedMessage').</summary>
        [JsonProperty("x-deprecatedMessage", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? DeprecatedMessage { get; set; }

        /// <summary>Gets or sets a value indicating whether the type is abstract, i.e. cannot be instantiated directly (x-abstract).</summary>
        [JsonProperty("x-abstract", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool IsAbstract { get; set; }

        /// <summary>Gets or sets a value indicating whether the schema is nullable (native in Open API 'nullable', custom in Swagger 'x-nullable').</summary>
        [JsonProperty("x-nullable", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool? IsNullableRaw { get; set; }

        /// <summary>Gets or sets the example (Swagger and Open API only).</summary>
        [JsonProperty("x-example", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public object? Example { get; set; }

        /// <summary>Gets or sets a value indicating this is an bit flag enum (custom extension, sets 'x-enumFlags', default: false).</summary>
        [JsonProperty("x-enumFlags", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool IsFlagEnumerable { get; set; }

        /// <summary>Gets the collection of required properties. </summary>
        [JsonIgnore]
        public ICollection<object?> Enumeration { get; internal set; }

        /// <summary>Gets a value indicating whether this is enumeration.</summary>
        [JsonIgnore]
        public bool IsEnumeration => Enumeration.Count > 0;

        /// <summary>Gets the collection of required properties. </summary>
        /// <remarks>This collection can also be changed through the <see cref="JsonSchemaProperty.IsRequired"/> property. </remarks>>
        [JsonIgnore]
        public ICollection<string> RequiredProperties { get; internal set; }

        #region Child JSON schemas

        /// <summary>Gets or sets the dictionary key schema (x-dictionaryKey, only enum schemas are allowed).</summary>
        [JsonProperty("x-dictionaryKey", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonSchema? DictionaryKey
        {
            get => _dictionaryKey;
            set
            {
                _dictionaryKey = value;
                if (_dictionaryKey != null)
                {
                    _dictionaryKey.Parent = this;
                }
            }
        }

        /// <summary>Gets the properties of the type. </summary>
        [JsonIgnore]
        public IDictionary<string, JsonSchemaProperty> Properties
        {
            get => _properties;
            internal set
            {
                if (_properties != value)
                {
                    var newCollection = ToObservableDictionary(value);
                    RegisterProperties(_properties, newCollection);
                    _properties = newCollection;
                }
            }
        }

        /// <summary>Gets the xml object of the schema (used in Swagger specifications). </summary>
        [JsonProperty("xml", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonXmlObject? Xml
        {
            get => _xmlObject;
            set
            {
                _xmlObject = value;

                if (_xmlObject != null)
                {
                    _xmlObject.ParentSchema = this;
                }
            }
        }

        [JsonIgnore]
        private JsonXmlObject? _xmlObject;

        /// <summary>Gets the pattern properties of the type. </summary>
        [JsonIgnore]
        public IDictionary<string, JsonSchemaProperty> PatternProperties
        {
            get => _patternProperties;
            internal set
            {
                if (_patternProperties != value)
                {
                    var newCollection = ToObservableDictionary(value);
                    RegisterSchemaDictionary(_patternProperties, newCollection);
                    _patternProperties = newCollection;
                }
            }
        }

        /// <summary>Gets or sets the schema of an array item. </summary>
        [JsonIgnore]
        public JsonSchema? Item
        {
            get => _item;
            set
            {
                if (_item != value)
                {
                    _item = value;
                    if (_item != null)
                    {
                        _item.Parent = this;
                        Items.Clear();
                    }
                }
            }
        }

        /// <summary>Gets or sets the schemas of the array's tuple values.</summary>
        [JsonIgnore]
        public ICollection<JsonSchema> Items
        {
            get => _items;
            internal set
            {
                if (_items != value)
                {
                    var newCollection = ToObservableCollection(value);
                    RegisterSchemaCollection(_items, newCollection);
                    _items = newCollection;

                    if (_items != null)
                    {
                        Item = null;
                    }
                }
            }
        }

        /// <summary>Gets or sets the schema which must not be valid. </summary>
        [JsonProperty("not", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonSchema? Not
        {
            get => _not;
            set
            {
                _not = value;
                if (_not != null)
                {
                    _not.Parent = this;
                }
            }
        }

        /// <summary>Gets the other schema definitions of this schema. </summary>
        [JsonIgnore]
        public IDictionary<string, JsonSchema> Definitions
        {
            get => _definitions;
            internal set
            {
                if (_definitions != value)
                {
                    var newCollection = ToObservableDictionary(value);
                    RegisterSchemaDictionary(_definitions, newCollection);
                    _definitions = newCollection;
                }
            }
        }

        /// <summary>Gets the collection of schemas where each schema must be valid. </summary>
        [JsonIgnore]
        public ICollection<JsonSchema> AllOf
        {
            get => _allOf;
            internal set
            {
                if (_allOf != value)
                {
                    var newCollection = ToObservableCollection(value);
                    RegisterSchemaCollection(_allOf, newCollection);
                    _allOf = newCollection;
                }
            }
        }

        /// <summary>Gets the collection of schemas where at least one must be valid. </summary>
        [JsonIgnore]
        public ICollection<JsonSchema> AnyOf
        {
            get => _anyOf;
            internal set
            {
                if (_anyOf != value)
                {
                    var newCollection = ToObservableCollection(value);
                    RegisterSchemaCollection(_anyOf, newCollection);
                    _anyOf = newCollection;
                }
            }
        }

        /// <summary>Gets the collection of schemas where exactly one must be valid. </summary>
        [JsonIgnore]
        public ICollection<JsonSchema> OneOf
        {
            get => _oneOf;
            internal set
            {
                if (_oneOf != value)
                {
                    var newCollection = ToObservableCollection(value);
                    RegisterSchemaCollection(_oneOf, newCollection);
                    _oneOf = newCollection;
                }
            }
        }

        /// <summary>Gets or sets a value indicating whether additional items are allowed (default: true). </summary>
        /// <remarks>If this property is set to <c>false</c>, then <see cref="AdditionalItemsSchema"/> is set to <c>null</c>. </remarks>
        [JsonIgnore]
        public bool AllowAdditionalItems
        {
            get => _allowAdditionalItems;
            set
            {
                if (_allowAdditionalItems != value)
                {
                    _allowAdditionalItems = value;
                    if (!_allowAdditionalItems)
                    {
                        AdditionalItemsSchema = null;
                    }
                }
            }
        }

        /// <summary>Gets or sets the schema for the additional items. </summary>
        /// <remarks>If this property has a schema, then <see cref="AllowAdditionalItems"/> is set to <c>true</c>. </remarks>
        [JsonIgnore]
        public JsonSchema? AdditionalItemsSchema
        {
            get => _additionalItemsSchema;
            set
            {
                if (_additionalItemsSchema != value)
                {
                    _additionalItemsSchema = value;
                    if (_additionalItemsSchema != null)
                    {
                        AllowAdditionalItems = true;
                    }
                }
            }
        }

        /// <summary>Gets or sets a value indicating whether additional properties are allowed (default: true). </summary>
        /// <remarks>If this property is set to <c>false</c>, then <see cref="AdditionalPropertiesSchema"/> is set to <c>null</c>. </remarks>
        [JsonIgnore]
        public bool AllowAdditionalProperties
        {
            get => _allowAdditionalProperties;
            set
            {
                if (_allowAdditionalProperties != value)
                {
                    _allowAdditionalProperties = value;
                    if (!_allowAdditionalProperties)
                    {
                        AdditionalPropertiesSchema = null;
                    }
                }
            }
        }

        /// <summary>Gets or sets the schema for the additional properties. </summary>
        /// <remarks>If this property has a schema, then <see cref="AllowAdditionalProperties"/> is set to <c>true</c>. </remarks>
        [JsonIgnore]
        public JsonSchema? AdditionalPropertiesSchema
        {
            get => _additionalPropertiesSchema;
            set
            {
                if (_additionalPropertiesSchema != value)
                {
                    _additionalPropertiesSchema = value;
                    if (_additionalPropertiesSchema != null)
                    {
                        AllowAdditionalProperties = true;
                    }
                }
            }
        }

        /// <summary>Gets a value indicating whether the schema describes an object.</summary>
        [JsonIgnore]
        public bool IsObject => Type.IsObject();

        /// <summary>Gets a value indicating whether the schema represents an array type (an array where each item has the same type).</summary>
        [JsonIgnore]
        public bool IsArray => Type.IsArray() && (Items == null || Items.Count == 0);

        /// <summary>Gets a value indicating whether the schema represents an tuple type (an array where each item may have a different type).</summary>
        [JsonIgnore]
        public bool IsTuple => Type.IsArray() && Items?.Count > 0;

        /// <summary>Gets a value indicating whether the schema represents a dictionary type (no properties and AdditionalProperties or PatternProperties contain a schema).</summary>
        [JsonIgnore]
        public bool IsDictionary => Type.IsObject() &&
                                    !HasActualProperties &&
                                    (AdditionalPropertiesSchema != null || PatternProperties.Any());

        /// <summary>Gets a value indicating whether this is any type (e.g. any in TypeScript or object in CSharp).</summary>
        [JsonIgnore]
        public bool IsAnyType => (Type.IsObject() || Type == JsonObjectType.None) &&
                                 Reference == null &&
                                 _allOf.Count == 0 &&
                                 _anyOf.Count == 0 &&
                                 _oneOf.Count == 0 &&
                                 !HasActualProperties &&
                                 _patternProperties.Count == 0 &&
                                 AdditionalPropertiesSchema == null &&
                                 MultipleOf == null &&
                                !IsEnumeration;

        #endregion

        /// <summary>Gets a value indicating whether the validated data can be null.</summary>
        /// <param name="schemaType">The schema type.</param>
        /// <returns>true if the type can be null.</returns>
        public virtual bool IsNullable(SchemaType schemaType)
        {
            if (IsNullableRaw == true)
            {
                return true;
            }

            if (IsEnumeration && Enumeration.Contains(null))
            {
                return true;
            }

            if (Type.IsNull())
            {
                return true;
            }

            if ((Type == JsonObjectType.None || Type.IsNull()) && _oneOf.Any(o => o.IsNullable(schemaType)))
            {
                return true;
            }

            var actualSchema = ActualSchema;
            if (actualSchema != this && actualSchema.IsNullable(schemaType))
            {
                return true;
            }

            var actualTypeSchema = ActualTypeSchema;
            if (actualTypeSchema != this && actualTypeSchema.IsNullable(schemaType))
            {
                return true;
            }

            if (ExtensionData != null && ExtensionData.TryGetValue("nullable", out var value))
            {
                if (bool.TryParse(value?.ToString(), out var boolValue))
                {
                    return boolValue;
                }
            }

            return false;
        }

        /// <summary>Serializes the <see cref="JsonSchema" /> to a JSON string.</summary>
        /// <returns>The JSON string.</returns>
        public string ToJson()
        {
            return ToJson(Formatting.Indented);
        }

        /// <summary>Serializes the <see cref="JsonSchema" /> to a JSON string.</summary>
        /// <param name="formatting">The formatting.</param>
        /// <returns>The JSON string.</returns>
        public string ToJson(Formatting formatting)
        {
            var oldSchema = SchemaVersion;
            SchemaVersion = "http://json-schema.org/draft-04/schema#";

            var json = JsonSchemaSerialization.ToJson(this, SerializationSchemaType, ContractResolver.Value, formatting);

            SchemaVersion = oldSchema;
            return json;
        }

        /// <summary>Generates a sample JSON object from a JSON Schema.</summary>
        /// <returns>The JSON token.</returns>
        public JToken ToSampleJson()
        {
            var generator = new SampleJsonDataGenerator();
            return generator.Generate(this);
        }

        ///// <summary>Serializes the <see cref="JsonSchema4" /> to a JSON string and removes externally loaded schemas.</summary>
        ///// <returns>The JSON string.</returns>
        //[Obsolete("Not ready yet as it has side-effects on the schema.")]
        //public string ToJsonWithExternalReferences()
        //{
        //    // TODO: Copy "this" schema first (high-prio)
        //    var oldSchema = SchemaVersion;
        //    SchemaVersion = "http://json-schema.org/draft-04/schema#";
        //    JsonSchemaReferenceUtilities.UpdateSchemaReferencePaths(this, true);
        //    var json = JsonSchemaReferenceUtilities.ConvertPropertyReferences(JsonConvert.SerializeObject(this, Formatting.Indented));
        //    SchemaVersion = oldSchema;
        //    return json;
        //}

        /// <summary>Gets a value indicating whether this schema inherits from the given parent schema.</summary>
        /// <param name="parentSchema">The parent schema.</param>
        /// <returns>true or false.</returns>
        public bool InheritsSchema(JsonSchema parentSchema)
        {
            return parentSchema != null && ActualSchema
                .AllInheritedSchemas.Concat([this])
                .Any(s => s.ActualSchema == parentSchema.ActualSchema);
        }

        /// <summary>Validates the given JSON data against this schema.</summary>
        /// <param name="jsonData">The JSON data to validate. </param>
        /// <param name="settings">The validator settings.</param>
        /// <exception cref="JsonReaderException">Could not deserialize the JSON data.</exception>
        /// <returns>The collection of validation errors. </returns>
        public ICollection<ValidationError> Validate(string jsonData, JsonSchemaValidatorSettings? settings = null)
        {
            var validator = new JsonSchemaValidator(settings);
            return validator.Validate(jsonData, ActualSchema);
        }
        /// <summary>Validates the given JSON token against this schema.</summary>
        /// <param name="token">The token to validate. </param>
        /// <param name="settings">The validator settings.</param>
        /// <returns>The collection of validation errors. </returns>
        public ICollection<ValidationError> Validate(JToken token, JsonSchemaValidatorSettings? settings = null)
        {
            var validator = new JsonSchemaValidator(settings);
            return validator.Validate(token, ActualSchema);
        }

        /// <summary>Validates the given JSON data against this schema.</summary>
        /// <param name="jsonData">The JSON data to validate. </param>
        /// <param name="schemaType">The type of the schema.</param>
        /// <param name="settings">The validator settings.</param>
        /// <exception cref="JsonReaderException">Could not deserialize the JSON data.</exception>
        /// <returns>The collection of validation errors. </returns>
        public ICollection<ValidationError> Validate(string jsonData, SchemaType schemaType, JsonSchemaValidatorSettings? settings = null)
        {
            var validator = new JsonSchemaValidator(settings);
            return validator.Validate(jsonData, ActualSchema, schemaType);
        }

        /// <summary>Validates the given JSON token against this schema.</summary>
        /// <param name="token">The token to validate. </param>
        /// <param name="schemaType">The type of the schema.</param>
        /// <param name="settings">The validator settings.</param>
        /// <returns>The collection of validation errors. </returns>
        public ICollection<ValidationError> Validate(JToken token, SchemaType schemaType, JsonSchemaValidatorSettings? settings = null)
        {
            var validator = new JsonSchemaValidator(settings);
            return validator.Validate(token, ActualSchema, schemaType);
        }

        private static JsonObjectType ConvertStringToJsonObjectType(string? value)
        {
            // Section 3.5:
            // http://json-schema.org/latest/json-schema-core.html#anchor8
            // The string must be one of the 7 primitive types

            switch (value)
            {
                case "array":
                    return JsonObjectType.Array;
                case "boolean":
                    return JsonObjectType.Boolean;
                case "integer":
                    return JsonObjectType.Integer;
                case "number":
                    return JsonObjectType.Number;
                case "null":
                    return JsonObjectType.Null;
                case "object":
                    return JsonObjectType.Object;
                case "string":
                    return JsonObjectType.String;
                case "file": // used for NSwag (special Swagger type)
                    return JsonObjectType.File;
                default:
                    return JsonObjectType.None;
            }
        }

        [MemberNotNull(nameof(Items))]
        [MemberNotNull(nameof(_items))]
        [MemberNotNull(nameof(Properties))]
        [MemberNotNull(nameof(_properties))]
        [MemberNotNull(nameof(PatternProperties))]
        [MemberNotNull(nameof(_patternProperties))]
        [MemberNotNull(nameof(Definitions))]
        [MemberNotNull(nameof(_definitions))]
        [MemberNotNull(nameof(RequiredProperties))]
        [MemberNotNull(nameof(AllOf))]
        [MemberNotNull(nameof(_allOf))]
        [MemberNotNull(nameof(AnyOf))]
        [MemberNotNull(nameof(_anyOf))]
        [MemberNotNull(nameof(OneOf))]
        [MemberNotNull(nameof(_oneOf))]
        [MemberNotNull(nameof(Enumeration))]
        [MemberNotNull(nameof(EnumerationNames))]
        [MemberNotNull(nameof(EnumerationDescriptions))]
        private void Initialize()
#pragma warning disable CS8774
        {
            Items ??= new ObservableCollection<JsonSchema>();
            Properties ??= new ObservableDictionary<string, JsonSchemaProperty>();
            PatternProperties ??= new ObservableDictionary<string, JsonSchemaProperty>();
            Definitions ??= new ObservableDictionary<string, JsonSchema>();
            RequiredProperties ??= [];
            AllOf ??= new ObservableCollection<JsonSchema>();
            AnyOf ??= new ObservableCollection<JsonSchema>();
            OneOf ??= new ObservableCollection<JsonSchema>();
            Enumeration ??= [];
            EnumerationNames ??= [];
            EnumerationDescriptions ??= [];
        }
#pragma warning restore CS8774

        private static ObservableCollection<T> ToObservableCollection<T>(ICollection<T> value)
        {
            return value as ObservableCollection<T> ?? new ObservableCollection<T>(value);
        }

        private static ObservableDictionary<string, T> ToObservableDictionary<T>(IDictionary<string, T> value)
        {
            return value as ObservableDictionary<string, T> ?? new ObservableDictionary<string, T>(value!);
        }
    }
}
