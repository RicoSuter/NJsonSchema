using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Collections;
using NJsonSchema.Validation;

namespace NJsonSchema
{
    /// <summary>A base class for describing a JSON schema. </summary>
    public class JsonSchema4
    {
        private IDictionary<string, JsonProperty> _properties;
        private IDictionary<string, JsonSchema4> _definitions;
        private ICollection<JsonSchema4> _allOf;
        private ICollection<JsonSchema4> _anyOf;
        private ICollection<JsonSchema4> _oneOf;
        private JsonSchema4 _items;
        private JsonSchema4 _not;

        /// <summary>Initializes a new instance of the <see cref="JsonSchema4"/> class. </summary>
        public JsonSchema4()
        {
            Initialize();
        }

        /// <summary>Creates a <see cref="JsonSchema4"/> from a given type. </summary>
        /// <typeparam name="TType">The type to create the schema for. </typeparam>
        /// <returns>The <see cref="JsonSchema4"/>. </returns>
        public static JsonSchema4 FromType<TType>()
        {
            var generator = new JsonSchemaGenerator();
            return generator.Generate<JsonSchema4>(typeof(TType));
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema4"/>. </summary>
        /// <param name="data">The JSON string. </param>
        /// <returns></returns>
        public static JsonSchema4 FromJson(string data)
        {
            return JsonConvert.DeserializeObject<JsonSchema4>(data, new JsonSerializerSettings { ConstructorHandling = ConstructorHandling.Default });
        }

        /// <summary>Gets or sets the schema. </summary>
        [JsonProperty("$schema", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Schema { get; set; }

        /// <summary>Gets or sets the id. </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Id { get; set; }

        /// <summary>Gets or sets the title. </summary>
        [JsonProperty("title", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Title { get; set; }

        /// <summary>Gets or sets the description. </summary>
        [JsonProperty("description", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Description { get; set; }

        /// <summary>Gets the object type. </summary>
        [JsonIgnore]
        public JsonObjectType Type { get; internal set; }

        /// <summary>Gets the parent schema of this schema. </summary>
        [JsonIgnore]
        public virtual JsonSchema4 Parent { get; internal set; }

        /// <summary>Gets or sets the format string. </summary>
        [JsonProperty("format", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Format { get; set; } // TODO: This is missing in JSON Schema schema

        /// <summary>Gets or sets the default value. </summary>
        [JsonProperty("default", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public object Default { get; set; }

        [JsonProperty("multipleOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public double? MultipleOf { get; set; } // TODO: Whats MultipleOf?

        /// <summary>Gets or sets the maximum allowed value. </summary>
        [JsonProperty("maximum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public double? Maximum { get; set; }

        /// <summary>Gets or sets a value indicating whether the maximum value is excluded. </summary>
        [JsonProperty("exclusiveMaximum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool IsExclusiveMaximum { get; set; }

        /// <summary>Gets or sets the minimum allowed value. </summary>
        [JsonProperty("minimum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public double? Minimum { get; set; }

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

        [JsonProperty("maxProperties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MaxProperties { get; set; }

        [JsonProperty("minProperties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MinProperties { get; set; }

        /// <summary>Gets the collection of required properties. </summary>
        [JsonIgnore]
        public ICollection<string> Enumerations { get; internal set; }

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

        /// <summary>Gets or sets the schema of an array item. </summary>
        [JsonProperty("items", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonSchema4 Items
        {
            get { return _items; }
            set
            {
                _items = value;
                if (_items != null)
                    _items.Parent = this;
            }
        }

        [JsonProperty("not", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonSchema4 Not
        {
            get { return _not; }
            internal set
            {
                _not = value;
                if (_not != null)
                    _not.Parent = this;
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

        #endregion

        #region Raw properties

        [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal object TypeRaw
        {
            get
            {
                var flags = Enum.GetValues(Type.GetType())
                    .Cast<Enum>().Where(v => Type.HasFlag(v))
                    .OfType<JsonObjectType>()
                    .Where(v => v != JsonObjectType.None)
                    .ToArray();

                if (flags.Length > 1)
                    return new JArray(flags.Select(f => new JValue(f.ToString().ToLower())));
                if (flags.Length == 1)
                    return new JValue(flags[0].ToString().ToLower());

                return null;
            }
            set
            {
                if (value is JArray)
                    Type = ((JArray)value).Aggregate(JsonObjectType.None, (type, token) => type | ConvertSimpleTypeFromString(token.ToString()));
                else if (value != null)
                    Type = ConvertSimpleTypeFromString(value.ToString());
                else
                    Type = JsonObjectType.None;
            }
        }

        [JsonProperty("required", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<string> RequiredPropertiesRaw
        {
            get { return RequiredProperties != null && RequiredProperties.Count > 0 ? RequiredProperties : null; }
            set { RequiredProperties = value; }
        }

        [JsonProperty("properties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal IDictionary<string, JsonSchema4> PropertiesRaw
        {
            get
            {
                return Properties != null && Properties.Count > 0 ?
                    Properties.ToDictionary(p => p.Key, p => (JsonSchema4)p.Value) :
                    null;
            }
            set
            {
                Properties = value != null ?
                    new ObservableDictionary<string, JsonProperty>(value.ToDictionary(p => p.Key, p => JsonProperty.FromJsonSchema(p.Key, p.Value))) :
                    new ObservableDictionary<string, JsonProperty>();
            }
        }

        [JsonProperty("definitions", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal IDictionary<string, JsonSchema4> DefinitionsRaw
        {
            get { return Definitions != null && Definitions.Count > 0 ? Definitions : null; }
            set { Definitions = value ?? new ObservableDictionary<string, JsonSchema4>(); }
        }

        [JsonProperty("enum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<string> EnumerationsRaw
        {
            get { return Enumerations != null && Enumerations.Count > 0 ? Enumerations : null; }
            set { Enumerations = value ?? new ObservableCollection<string>(); }
        }

        [JsonProperty("allOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<JsonSchema4> AllOfRaw
        {
            get { return AllOf != null && AllOf.Count > 0 ? AllOf : null; }
            set { AllOf = value ?? new ObservableCollection<JsonSchema4>(); }
        }

        [JsonProperty("anyOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<JsonSchema4> AnyOfRaw
        {
            get { return AnyOf != null && AnyOf.Count > 0 ? AnyOf : null; }
            set { AnyOf = value ?? new ObservableCollection<JsonSchema4>(); }
        }

        [JsonProperty("oneOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<JsonSchema4> OneOfRaw
        {
            get { return OneOf != null && OneOf.Count > 0 ? OneOf : null; }
            set { OneOf = value ?? new ObservableCollection<JsonSchema4>(); }
        }

        #endregion

        /// <summary>Serializes the <see cref="JsonSchema4"/> to a JSON string. </summary>
        /// <returns>The JSON string. </returns>
        public string ToJson()
        {
            var oldSchema = Schema;
            Schema = "http://json-schema.org/draft-04/schema#";
            var data = JsonConvert.SerializeObject(this, Formatting.Indented);
            Schema = oldSchema;
            return data;
        }

        /// <summary>Validates the given JSON token against this schema. </summary>
        /// <param name="token">The token to validate. </param>
        /// <returns>The collection of validation errors. </returns>
        public ICollection<ValidationError> Validate(JToken token)
        {
            var validator = new JsonSchemaValidator(this);
            return validator.Validate(token, null, null);
        }

        public JsonSchema4 FindRootParent()
        {
            var parent = Parent;
            if (parent == null)
                return this;

            while (parent.Parent != null)
                parent = parent.Parent;
            return parent;
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext ctx)
        {
            Initialize();
        }

        private static JsonObjectType ConvertSimpleTypeFromString(string value)
        {
            // TODO: Improve performance
            return JsonConvert.DeserializeObject<JsonObjectType>("\"" + value + "\""); 
        }

        private void Initialize()
        {
            if (Properties == null)
                Properties = new ObservableDictionary<string, JsonProperty>();

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

            if (Enumerations == null)
                Enumerations = new ObservableCollection<string>();
        }

        private void RegisterProperties(IDictionary<string, JsonProperty> oldCollection, IDictionary<string, JsonProperty> newCollection)
        {
            if (oldCollection != null)
                ((ObservableDictionary<string, JsonProperty>)oldCollection).CollectionChanged -= InitializeSchemaCollection;
           
            if (newCollection != null)
            {
                ((ObservableDictionary<string, JsonProperty>)newCollection).CollectionChanged += InitializeSchemaCollection;
                InitializeSchemaCollection(newCollection, null);
            }
        }

        private void RegisterSchemaDictionary(IDictionary<string, JsonSchema4> oldCollection, IDictionary<string, JsonSchema4> newCollection)
        {
            if (oldCollection != null)
                ((ObservableDictionary<string, JsonSchema4>)oldCollection).CollectionChanged -= InitializeSchemaCollection;
            
            if (newCollection != null)
            {
                ((ObservableDictionary<string, JsonSchema4>)newCollection).CollectionChanged += InitializeSchemaCollection;
                InitializeSchemaCollection(newCollection, null);
            }
        }

        private void RegisterSchemaCollection(ICollection<JsonSchema4> oldCollection, ICollection<JsonSchema4> newCollection)
        {
            if (oldCollection != null)
                ((ObservableCollection<JsonSchema4>)oldCollection).CollectionChanged -= InitializeSchemaCollection;
            
            if (newCollection != null)
            {
                ((ObservableCollection<JsonSchema4>)newCollection).CollectionChanged += InitializeSchemaCollection;
                InitializeSchemaCollection(newCollection, null);
            }
        }

        private void InitializeSchemaCollection(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is ObservableDictionary<string, JsonProperty>)
            {
                var properties = (ObservableDictionary<string, JsonProperty>) sender;
                foreach (var property in properties)
                {
                    property.Value.Key = property.Key;
                    property.Value.Parent = this;
                }
            }
            else if (sender is ObservableCollection<JsonSchema4>)
            {
                var collection = (ObservableCollection<JsonSchema4>)sender;
                foreach (var item in collection)
                    item.Parent = this;
            }
            else if (sender is ObservableDictionary<string, JsonSchema4>)
            {
                var collection = (ObservableDictionary<string, JsonSchema4>)sender;
                foreach (var item in collection.Values)
                    item.Parent = this;
            }
        }
    }
}