//-----------------------------------------------------------------------
// <copyright file="JsonSchema4.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Collections;
using NJsonSchema.Generation;
using NJsonSchema.Infrastructure;
using NJsonSchema.Validation;

namespace NJsonSchema
{
    /// <summary>A base class for describing a JSON schema. </summary>
    public partial class JsonSchema4 : IDocumentPathProvider
    {
        private IDictionary<string, JsonProperty> _properties;
        private IDictionary<string, JsonSchema4> _patternProperties;
        private IDictionary<string, JsonSchema4> _definitions;

        private ICollection<JsonSchema4> _allOf;
        private ICollection<JsonSchema4> _anyOf;
        private ICollection<JsonSchema4> _oneOf;
        private JsonSchema4 _not;

        private JsonSchema4 _item;
        private ICollection<JsonSchema4> _items;

        private bool _allowAdditionalItems = true;
        private JsonSchema4 _additionalItemsSchema = null;

        private bool _allowAdditionalProperties = true;
        private JsonSchema4 _additionalPropertiesSchema = null;
        private JsonSchema4 _schemaReference;

        /// <summary>Initializes a new instance of the <see cref="JsonSchema4"/> class. </summary>
        public JsonSchema4()
        {
            Initialize();
        }

        /// <summary>Creates a schema which matches any data.</summary>
        /// <returns>The any schema.</returns>
        public static JsonSchema4 CreateAnySchema()
        {
            return new JsonSchema4();
        }

        /// <summary>Creates a schema which matches any data.</summary>
        /// <returns>The any schema.</returns>
        public static TSchemaType CreateAnySchema<TSchemaType>()
            where TSchemaType : JsonSchema4, new()
        {
            return new TSchemaType();
        }

        /// <summary>Gets the NJsonSchema toolchain version.</summary>
        public static string ToolchainVersion => typeof(JsonSchema4).GetTypeInfo().Assembly.GetName().Version.ToString();

        /// <summary>Creates a <see cref="JsonSchema4" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <returns>The <see cref="JsonSchema4" />.</returns>
        public static async Task<JsonSchema4> FromTypeAsync<TType>()
        {
            return await FromTypeAsync<TType>(new JsonSchemaGeneratorSettings()).ConfigureAwait(false);
        }

        /// <summary>Creates a <see cref="JsonSchema4" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <returns>The <see cref="JsonSchema4" />.</returns>
        public static async Task<JsonSchema4> FromTypeAsync(Type type)
        {
            return await FromTypeAsync(type, new JsonSchemaGeneratorSettings()).ConfigureAwait(false);
        }

        /// <summary>Creates a <see cref="JsonSchema4" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema4" />.</returns>
        public static async Task<JsonSchema4> FromTypeAsync<TType>(JsonSchemaGeneratorSettings settings)
        {
            var generator = new JsonSchemaGenerator(settings);
            return await generator.GenerateAsync(typeof(TType)).ConfigureAwait(false);
        }

        /// <summary>Creates a <see cref="JsonSchema4" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema4" />.</returns>
        public static async Task<JsonSchema4> FromTypeAsync(Type type, JsonSchemaGeneratorSettings settings)
        {
            var generator = new JsonSchemaGenerator(settings);
            return await generator.GenerateAsync(type).ConfigureAwait(false);
        }

        /// <summary>Creates a <see cref="JsonSchema4" /> from sample JSON data.</summary>
        /// <returns>The JSON Schema.</returns>
        public static JsonSchema4 FromData(string data)
        {
            var generator = new DataToJsonSchemaGenerator();
            return generator.Generate(data);
        }

        /// <summary>Loads a JSON Schema from a given file path (only available in .NET 4.x).</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema4> FromFileAsync(string filePath)
        {
            Func<JsonSchema4, JsonReferenceResolver> referenceResolverFactory =
                schema => new JsonReferenceResolver(new JsonSchemaResolver(schema, new JsonSchemaGeneratorSettings()));

            return await FromFileAsync(filePath, referenceResolverFactory).ConfigureAwait(false);
        }

        /// <summary>Loads a JSON Schema from a given file path (only available in .NET 4.x).</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <returns>The JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The System.IO.File API is not available on this platform.</exception>
        public static async Task<JsonSchema4> FromFileAsync(string filePath, Func<JsonSchema4, JsonReferenceResolver> referenceResolverFactory)
        {
            var data = await DynamicApis.FileReadAllTextAsync(filePath);
            return await FromJsonAsync(data, filePath, referenceResolverFactory).ConfigureAwait(false);
        }

        /// <summary>Loads a JSON Schema from a given URL (only available in .NET 4.x).</summary>
        /// <param name="url">The URL to the document.</param>
        /// <returns>The JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The HttpClient.GetAsync API is not available on this platform.</exception>
        public static async Task<JsonSchema4> FromUrlAsync(string url)
        {
            Func<JsonSchema4, JsonReferenceResolver> referenceResolverFactory =
                schema => new JsonReferenceResolver(new JsonSchemaResolver(schema, new JsonSchemaGeneratorSettings()));

            return await FromUrlAsync(url, referenceResolverFactory).ConfigureAwait(false);
        }

        /// <summary>Loads a JSON Schema from a given URL (only available in .NET 4.x).</summary>
        /// <param name="url">The URL to the document.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <returns>The JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The HttpClient.GetAsync API is not available on this platform.</exception>
        public static async Task<JsonSchema4> FromUrlAsync(string url, Func<JsonSchema4, JsonReferenceResolver> referenceResolverFactory)
        {
            var data = await DynamicApis.HttpGetAsync(url);
            return await FromJsonAsync(data, url, referenceResolverFactory).ConfigureAwait(false);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema4"/>. </summary>
        /// <param name="data">The JSON string. </param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema4> FromJsonAsync(string data)
        {
            return await FromJsonAsync(data, null).ConfigureAwait(false);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema4"/>. </summary>
        /// <param name="data">The JSON string. </param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema4> FromJsonAsync(string data, string documentPath)
        {
            Func<JsonSchema4, JsonReferenceResolver> referenceResolverFactory =
                schema => new JsonReferenceResolver(new JsonSchemaResolver(schema, new JsonSchemaGeneratorSettings()));

            return await FromJsonAsync(data, documentPath, referenceResolverFactory).ConfigureAwait(false);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema4" />.</summary>
        /// <param name="data">The JSON string.</param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema4> FromJsonAsync(string data, string documentPath, Func<JsonSchema4, JsonReferenceResolver> referenceResolverFactory)
        {
            data = JsonSchemaReferenceUtilities.ConvertJsonReferences(data);
            var schema = JsonConvert.DeserializeObject<JsonSchema4>(data, new JsonSerializerSettings
            {
                ConstructorHandling = ConstructorHandling.Default,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });

            schema.DocumentPath = documentPath;

            var referenceResolver = referenceResolverFactory(schema);
            if (!string.IsNullOrEmpty(documentPath))
                referenceResolver.AddDocumentReference(documentPath, schema);

            await JsonSchemaReferenceUtilities.UpdateSchemaReferencesAsync(schema, referenceResolver).ConfigureAwait(false);
            return schema;
        }

        internal static JsonSchema4 FromJsonWithoutReferenceHandling(string data)
        {
            var schema = JsonConvert.DeserializeObject<JsonSchema4>(data, new JsonSerializerSettings
            {
                ConstructorHandling = ConstructorHandling.Default,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });
            return schema;
        }

        /// <summary>Gets the list of directly inherited/parent schemas (i.e. all schemas in allOf with a type of 'Object').</summary>
        /// <remarks>Used for code generation.</remarks>
        [JsonIgnore]
#if !LEGACY
        public IReadOnlyCollection<JsonSchema4> InheritedSchemas
#else
        public ICollection<JsonSchema4> InheritedSchemas
#endif
        {
            get
            {
                return new ReadOnlyCollection<JsonSchema4>(AllOf
                    .Where(s => s.ActualSchema.Type == JsonObjectType.Object)
                    .Select(s => s.ActualSchema)
                    .ToList());
            }
        }

        /// <summary>Gets the list of all inherited/parent schemas.</summary>
        /// <remarks>Used for code generation.</remarks>
        [JsonIgnore]
#if !LEGACY
        public IReadOnlyCollection<JsonSchema4> AllInheritedSchemas
#else
        public ICollection<JsonSchema4> AllInheritedSchemas
#endif
        {
            get
            {
                var inheritedSchemas = InheritedSchemas;
                return inheritedSchemas.Concat(inheritedSchemas.SelectMany(s => s.AllInheritedSchemas)).ToList();
            }
        }

        /// <summary>Determines whether the given schema is the parent schema of this schema (i.e. super/base class).</summary>
        /// <param name="schema">The possible subtype schema.</param>
        /// <returns>true or false</returns>
        public bool Inherits(JsonSchema4 schema)
        {
            schema = schema.ActualSchema;
            return InheritedSchemas.Any(s => s.ActualSchema == schema || s.Inherits(schema));
        }

        /// <summary>Gets the discriminator or discriminator of an inherited schema (or null).</summary>
        [JsonIgnore]
        public string BaseDiscriminator
        {
            get
            {
                if (!string.IsNullOrEmpty(Discriminator))
                    return Discriminator;

                foreach (var inheritedSchema in InheritedSchemas)
                {
                    var baseDiscriminator = inheritedSchema.ActualSchema.BaseDiscriminator;
                    if (!string.IsNullOrEmpty(baseDiscriminator))
                        return baseDiscriminator;
                }

                return null;
            }
        }

        /// <summary>Gets all properties of this schema (i.e. all direct properties and properties from the schemas in allOf which do not have a type).</summary>
        /// <remarks>Used for code generation.</remarks>
        [JsonIgnore]
#if !LEGACY
        public IReadOnlyDictionary<string, JsonProperty> ActualProperties
        {
            get
            {
                return new ReadOnlyDictionary<string, JsonProperty>(Properties
#else
        public IDictionary<string, JsonProperty> ActualProperties
        {
            get
            {
                return new Dictionary<string, JsonProperty>(Properties
#endif
                    .Union(AllOf.Where(s => s.ActualSchema.Type == JsonObjectType.None)
                    .SelectMany(s => s.ActualSchema.ActualProperties))
                    .ToDictionary(p => p.Key, p => p.Value));
            }
        }

        /// <summary>Gets or sets the schema. </summary>
        [JsonProperty("$schema", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = -100 + 1)]
        public string SchemaVersion { get; set; }

        /// <summary>Gets or sets the id. </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = -100 + 2)]
        public string Id { get; set; }

        /// <summary>Gets or sets the title. </summary>
        [JsonProperty("title", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = -100 + 3)]
        public string Title { get; set; }

        /// <summary>Gets or sets the description. </summary>
        [JsonProperty("description", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Description { get; set; }

        /// <summary>Gets the object types (as enum flags). </summary>
        [JsonIgnore]
        public JsonObjectType Type { get; set; }

        /// <summary>Gets the document path (URI or file path) for resolving relative references.</summary>
        [JsonIgnore]
        public string DocumentPath { get; set; }

        /// <summary>Gets or sets the type reference path ($ref). </summary>
        [JsonProperty("schemaReferencePath", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal string SchemaReferencePath { get; set; }

        /// <summary>Gets or sets the type reference.</summary>
        [JsonIgnore]
        public JsonSchema4 SchemaReference
        {
            get { return _schemaReference; }
            set
            {
                if (_schemaReference != value)
                {
                    _schemaReference = value;
                    SchemaReferencePath = null;

                    if (value != null)
                    {
                        // only $ref property is allowed when schema is a reference
                        // TODO: Fix all SchemaReference assignments so that this code is not needed 
                        Type = JsonObjectType.None;
                    }
                }
            }
        }

        /// <summary>Gets a value indicating whether this is a type reference.</summary>
        [JsonIgnore]
        public bool HasSchemaReference => SchemaReference != null;

        /// <summary>Gets the actual schema, either this or the reference schema.</summary>
        /// <exception cref="InvalidOperationException">Cyclic references detected.</exception>
        /// <exception cref="InvalidOperationException">The schema reference path has not been resolved.</exception>
        [JsonIgnore]
        public virtual JsonSchema4 ActualSchema => GetActualSchema(new List<JsonSchema4>());

        /// <exception cref="InvalidOperationException">Cyclic references detected.</exception>
        /// <exception cref="InvalidOperationException">The schema reference path has not been resolved.</exception>
        private JsonSchema4 GetActualSchema(IList<JsonSchema4> checkedSchemas)
        {
            if (checkedSchemas.Contains(this))
                throw new InvalidOperationException("Cyclic references detected.");

            if (SchemaReferencePath != null && SchemaReference == null)
                throw new InvalidOperationException("The schema reference path '" + SchemaReferencePath + "' has not been resolved.");

            if (HasSchemaReference)
            {
                checkedSchemas.Add(this);
                return SchemaReference.GetActualSchema(checkedSchemas);
            }

            return this;
        }

        /// <summary>Gets the parent schema of this schema. </summary>
        [JsonIgnore]
        public virtual JsonSchema4 ParentSchema { get; internal set; }

        /// <summary>Gets or sets the format string. </summary>
        [JsonProperty("format", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Format { get; set; }

        /// <summary>Gets or sets the default value. </summary>
        [JsonProperty("default", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public object Default { get; set; }

        /// <summary>Gets or sets the required multiple of for the number value. </summary>
        [JsonProperty("multipleOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public decimal? MultipleOf { get; set; }

        /// <summary>Gets or sets the maximum allowed value. </summary>
        [JsonProperty("maximum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public decimal? Maximum { get; set; }

        /// <summary>Gets or sets a value indicating whether the maximum value is excluded. </summary>
        [JsonProperty("exclusiveMaximum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool IsExclusiveMaximum { get; set; }

        /// <summary>Gets or sets the minimum allowed value. </summary>
        [JsonProperty("minimum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public decimal? Minimum { get; set; }

        /// <summary>Gets or sets a value indicating whether the minimum value is excluded. </summary>
        [JsonProperty("exclusiveMinimum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool IsExclusiveMinimum { get; set; }

        /// <summary>Gets or sets the maximum length of the value string. </summary>
        [JsonProperty("maxLength", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int? MaxLength { get; set; }

        /// <summary>Gets or sets the minimum length of the value string. </summary>
        [JsonProperty("minLength", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int? MinLength { get; set; }

        /// <summary>Gets or sets the validation pattern as regular expression. </summary>
        [JsonProperty("pattern", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Pattern { get; set; }

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

        /// <summary>Gets the collection of required properties. </summary>
        [JsonIgnore]
        public ICollection<object> Enumeration { get; internal set; }

        /// <summary>Gets a value indicating whether this is enumeration.</summary>
        [JsonIgnore]
        public bool IsEnumeration => Enumeration.Count > 0;

        /// <summary>Gets the collection of required properties. </summary>
        /// <remarks>This collection can also be changed through the <see cref="JsonProperty.IsRequired"/> property. </remarks>>
        [JsonIgnore]
        public ICollection<string> RequiredProperties { get; internal set; }

        #region Child JSON schemas

        /// <summary>Gets the properties of the type. </summary>
        [JsonIgnore]
        public IDictionary<string, JsonProperty> Properties
        {
            get { return _properties; }
            internal set
            {
                if (_properties != value)
                {
                    RegisterProperties(_properties, value);
                    _properties = value;
                }
            }
        }

        /// <summary>Gets the xml object of the schema (used in Swagger specifications). </summary>
        [JsonProperty("xml", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonXmlObject Xml
        {
            get { return _xmlObject; }
            set
            {
                _xmlObject = value;

                if (_xmlObject != null)
                    _xmlObject.ParentSchema = this;
            }
        }

        [JsonIgnore]
        private JsonXmlObject _xmlObject;

        /// <summary>Gets the pattern properties of the type. </summary>
        [JsonIgnore]
        public IDictionary<string, JsonSchema4> PatternProperties
        {
            get { return _patternProperties; }
            internal set
            {
                if (_patternProperties != value)
                {
                    RegisterSchemaDictionary(_patternProperties, value);
                    _patternProperties = value;
                }
            }
        }

        /// <summary>Gets or sets the schema of an array item. </summary>
        [JsonIgnore]
        public JsonSchema4 Item
        {
            get { return _item; }
            set
            {
                if (_item != value)
                {
                    _item = value;
                    if (_item != null)
                    {
                        _item.ParentSchema = this;
                        Items.Clear();
                    }
                }
            }
        }

        /// <summary>Gets or sets the schemas of the array's tuple values.</summary>
        [JsonIgnore]
        public ICollection<JsonSchema4> Items
        {
            get { return _items; }
            internal set
            {
                if (_items != value)
                {
                    RegisterSchemaCollection(_items, value);
                    _items = value;

                    if (_items != null)
                        Item = null;
                }
            }
        }

        /// <summary>Gets or sets the schema which must not be valid. </summary>
        [JsonProperty("not", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonSchema4 Not
        {
            get { return _not; }
            set
            {
                _not = value;
                if (_not != null)
                    _not.ParentSchema = this;
            }
        }

        /// <summary>Gets the other schema definitions of this schema. </summary>
        [JsonIgnore]
        public IDictionary<string, JsonSchema4> Definitions
        {
            get { return _definitions; }
            internal set
            {
                if (_definitions != value)
                {
                    RegisterSchemaDictionary(_definitions, value);
                    _definitions = value;
                }
            }
        }

        /// <summary>Gets or sets the resource definitions (not in the standard, needed in some JSON Schemas).</summary>
        [JsonProperty("resourceDefinitions", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal IDictionary<string, JsonSchema4> ResourceDefinitions { get; set; }

        /// <summary>Gets or sets the constraint definitions (not in the standard, needed in some JSON Schemas).</summary>
        [JsonProperty("constraints", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal IDictionary<string, JsonSchema4> ConstraintsDefinitions { get; set; }

        /// <summary>Gets the collection of schemas where each schema must be valid. </summary>
        [JsonIgnore]
        public ICollection<JsonSchema4> AllOf
        {
            get { return _allOf; }
            internal set
            {
                if (_allOf != value)
                {
                    RegisterSchemaCollection(_allOf, value);
                    _allOf = value;
                }
            }
        }

        /// <summary>Gets the collection of schemas where at least one must be valid. </summary>
        [JsonIgnore]
        public ICollection<JsonSchema4> AnyOf
        {
            get { return _anyOf; }
            internal set
            {
                if (_anyOf != value)
                {
                    RegisterSchemaCollection(_anyOf, value);
                    _anyOf = value;
                }
            }
        }

        /// <summary>Gets the collection of schemas where exactly one must be valid. </summary>
        [JsonIgnore]
        public ICollection<JsonSchema4> OneOf
        {
            get { return _oneOf; }
            internal set
            {
                if (_oneOf != value)
                {
                    RegisterSchemaCollection(_oneOf, value);
                    _oneOf = value;
                }
            }
        }

        /// <summary>Gets or sets a value indicating whether additional items are allowed (default: true). </summary>
        /// <remarks>If this property is set to <c>false</c>, then <see cref="AdditionalItemsSchema"/> is set to <c>null</c>. </remarks>
        [JsonIgnore]
        public bool AllowAdditionalItems
        {
            get { return _allowAdditionalItems; }
            set
            {
                if (_allowAdditionalItems != value)
                {
                    _allowAdditionalItems = value;
                    if (!_allowAdditionalItems)
                        AdditionalItemsSchema = null;
                }
            }
        }

        /// <summary>Gets or sets the schema for the additional items. </summary>
        /// <remarks>If this property has a schema, then <see cref="AllowAdditionalItems"/> is set to <c>true</c>. </remarks>
        [JsonIgnore]
        public JsonSchema4 AdditionalItemsSchema
        {
            get { return _additionalItemsSchema; }
            set
            {
                if (_additionalItemsSchema != value)
                {
                    _additionalItemsSchema = value;
                    if (_additionalItemsSchema != null)
                        AllowAdditionalItems = true;
                }
            }
        }

        /// <summary>Gets or sets a value indicating whether additional properties are allowed (default: true). </summary>
        /// <remarks>If this property is set to <c>false</c>, then <see cref="AdditionalPropertiesSchema"/> is set to <c>null</c>. </remarks>
        [JsonIgnore]
        public bool AllowAdditionalProperties
        {
            get { return _allowAdditionalProperties; }
            set
            {
                if (_allowAdditionalProperties != value)
                {
                    _allowAdditionalProperties = value;
                    if (!_allowAdditionalProperties)
                        AdditionalPropertiesSchema = null;
                }
            }
        }

        /// <summary>Gets or sets the schema for the additional properties. </summary>
        /// <remarks>If this property has a schema, then <see cref="AllowAdditionalProperties"/> is set to <c>true</c>. </remarks>
        [JsonIgnore]
        public JsonSchema4 AdditionalPropertiesSchema
        {
            get { return _additionalPropertiesSchema; }
            set
            {
                if (_additionalPropertiesSchema != value)
                {
                    _additionalPropertiesSchema = value;
                    if (_additionalPropertiesSchema != null)
                        AllowAdditionalProperties = true;
                }
            }
        }

        /// <summary>Gets a value indicating whether the schema represents a dictionary type (no properties and AdditionalProperties contains a schema).</summary>
        [JsonIgnore]
        public bool IsDictionary =>
            Type.HasFlag(JsonObjectType.Object) &&
            Properties.Count == 0 &&
            (AllowAdditionalProperties || PatternProperties.Any());

        /// <summary>Gets a value indicating whether this is any type (e.g. any in TypeScript or object in CSharp).</summary>
        [JsonIgnore]
        public bool IsAnyType => (Type.HasFlag(JsonObjectType.Object) || Type == JsonObjectType.None) &&
                                 Properties.Count == 0 &&
                                 PatternProperties.Count == 0 &&
                                 AnyOf.Count == 0 &&
                                 AllOf.Count == 0 &&
                                 OneOf.Count == 0 &&
                                 AllowAdditionalProperties &&
                                 AdditionalPropertiesSchema == null &&
                                 MultipleOf == null &&
                                 IsEnumeration == false;

        #endregion

        /// <summary>Gets a value indicating whether the validated data can be null.</summary>
        public virtual bool IsNullable(NullHandling nullHandling)
        {
            if (Type.HasFlag(JsonObjectType.Null) && OneOf.Count == 0)
                return true;

            return (Type == JsonObjectType.None || Type.HasFlag(JsonObjectType.Null)) && OneOf.Any(o => o.IsNullable(nullHandling));
        }

        /// <summary>Serializes the <see cref="JsonSchema4" /> to a JSON string.</summary>
        /// <returns>The JSON string.</returns>
        public string ToJson()
        {
            var settings = new JsonSchemaGeneratorSettings();
            return ToJson(settings);
        }

        /// <summary>Serializes the <see cref="JsonSchema4" /> to a JSON string.</summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The JSON string.</returns>
        public string ToJson(JsonSchemaGeneratorSettings settings)
        {
            var oldSchema = SchemaVersion;
            SchemaVersion = "http://json-schema.org/draft-04/schema#";
            JsonSchemaReferenceUtilities.UpdateSchemaReferencePaths(this);
            var json = JsonSchemaReferenceUtilities.ConvertPropertyReferences(JsonConvert.SerializeObject(this, Formatting.Indented));
            SchemaVersion = oldSchema;
            return json;
        }

        /// <summary>Validates the given JSON data against this schema.</summary>
        /// <param name="jsonData">The JSON data to validate. </param>
        /// <returns>The collection of validation errors. </returns>
        public ICollection<ValidationError> Validate(string jsonData)
        {
            var validator = new JsonSchemaValidator();
            return validator.Validate(jsonData, ActualSchema);
        }

        /// <summary>Validates the given JSON token against this schema.</summary>
        /// <param name="token">The token to validate. </param>
        /// <returns>The collection of validation errors. </returns>
        public ICollection<ValidationError> Validate(JToken token)
        {
            var validator = new JsonSchemaValidator();
            return validator.Validate(token, ActualSchema);
        }

        /// <summary>Finds the root parent of this schema.</summary>
        /// <returns>The parent schema or this when this is the root.</returns>
        public JsonSchema4 FindRootParent()
        {
            var parent = ParentSchema;
            if (parent == null)
                return this;

            while (parent.ParentSchema != null)
                parent = parent.ParentSchema;

            return parent;
        }

        private static JsonObjectType ConvertStringToJsonObjectType(string value)
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

        private void Initialize()
        {
            if (Items == null)
                Items = new ObservableCollection<JsonSchema4>();

            if (Properties == null)
                Properties = new ObservableDictionary<string, JsonProperty>();

            if (PatternProperties == null)
                PatternProperties = new ObservableDictionary<string, JsonSchema4>();

            if (Definitions == null)
                Definitions = new ObservableDictionary<string, JsonSchema4>();

            if (RequiredProperties == null)
                RequiredProperties = new ObservableCollection<string>();

            if (AllOf == null)
                AllOf = new ObservableCollection<JsonSchema4>();

            if (AnyOf == null)
                AnyOf = new ObservableCollection<JsonSchema4>();

            if (OneOf == null)
                OneOf = new ObservableCollection<JsonSchema4>();

            if (Enumeration == null)
                Enumeration = new Collection<object>();

            if (EnumerationNames == null)
                EnumerationNames = new Collection<string>();
        }
    }
}