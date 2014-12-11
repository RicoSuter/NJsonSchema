using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Collections;

namespace NJsonSchema.DraftV4
{
    /// <summary>A base class for describing a JSON schema. </summary>
    public class JsonSchemaBase
    {
        private IDictionary<string, JsonProperty> _properties;

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaBase"/> class. </summary>
        public JsonSchemaBase()
        {
            Initialize();
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

        /// <summary>Gets or sets the format string. </summary>
        [JsonProperty("format", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Format { get; set; } // TODO: This is missing in JSON Schema schema

        /// <summary>Gets or sets the default value. </summary>
        [JsonProperty("default", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public object Default { get; set; }

        [JsonProperty("multipleOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MultipleOf { get; set; }

        /// <summary>Gets or sets the maximum allowed value. </summary>
        [JsonProperty("maximum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int Maximum { get; set; }

        /// <summary>Gets or sets a value indicating whether the maximum value is excluded. </summary>
        [JsonProperty("exclusiveMaximum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool IsExclusiveMaximum { get; set; }

        /// <summary>Gets or sets the minimum allowed value. </summary>
        [JsonProperty("minimum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int Minimum { get; set; }

        /// <summary>Gets or sets a value indicating whether the minimum value is excluded. </summary>
        [JsonProperty("exclusiveMinimum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool IsExclusiveMinimum { get; set; }

        /// <summary>Gets or sets the maximum length of the value string. </summary>
        [JsonProperty("maxLength", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MaxLength { get; set; }

        /// <summary>Gets or sets the minimum length of the value string. </summary>
        [JsonProperty("minLength", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MinLength { get; set; }

        /// <summary>Gets or sets the validation pattern as regular expression. </summary>
        [JsonProperty("pattern", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Pattern { get; set; }

        /// <summary>Gets or sets the schema of an array item. </summary>
        [JsonProperty("items", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonSchemaBase Items { get; set; }

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
        /// <remarks>This collection can also be changed through the <see cref="JsonProperty.IsRequired"/> property. </remarks>>
        [JsonIgnore]
        public ICollection<string> RequiredProperties { get; internal set; }

        /// <summary>Gets the properties of the type. </summary>
        [JsonIgnore]
        public IDictionary<string, JsonProperty> Properties
        {
            get { return _properties; }
            internal set
            {
                if (_properties != value)
                {
                    _properties = value;
                    ((ObservableDictionary<string, JsonProperty>)_properties).CollectionChanged += (sender, args) => InitializeProperties();
                }
            }
        }

        /// <summary>Gets the other schema definitions of this schema. </summary>
        [JsonIgnore]
        public IDictionary<string, JsonSchemaBase> Definitions { get; internal set; }

        [JsonIgnore]
        public ICollection<JsonSchemaBase> AllOf { get; internal set; }

        [JsonIgnore]
        public ICollection<JsonSchemaBase> AnyOf { get; internal set; }

        [JsonIgnore]
        public ICollection<JsonSchemaBase> OneOf { get; internal set; }

        [JsonIgnore]
        public SimpleType Type { get; internal set; }

        [JsonProperty("not", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonSchemaBase Not { get; internal set; }

        #region Raw properties

        [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal object TypeRaw
        {
            get
            {
                var flags = Enum.GetValues(Type.GetType())
                    .Cast<Enum>().Where(v => Type.HasFlag(v))
                    .OfType<SimpleType>()
                    .Where(v => v != SimpleType.None)
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
                    Type = ((JArray)value).Aggregate(SimpleType.None, (type, token) => type | ConvertSimpleTypeFromString(token.ToString()));
                else if (value != null)
                    Type = ConvertSimpleTypeFromString(value.ToString());
                else
                    Type = SimpleType.None;
            }
        }

        [JsonProperty("required", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<string> RequiredPropertiesRaw
        {
            get { return RequiredProperties != null && RequiredProperties.Count > 0 ? RequiredProperties : null; }
            set { RequiredProperties = value; }
        }

        [JsonProperty("properties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal IDictionary<string, JsonSchemaBase> PropertiesRaw
        {
            get
            {
                return Properties != null && Properties.Count > 0 ? 
                    Properties.ToDictionary(p => p.Key, p => (JsonSchemaBase)p.Value) : 
                    null;
            }
            set
            {
                Properties = value != null ?
                    new ObservableDictionary<string, JsonProperty>(value.ToDictionary(p => p.Key, p => new JsonProperty(p.Key, p.Value))) : 
                    new ObservableDictionary<string, JsonProperty>();
            }
        }

        [JsonProperty("definitions", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal IDictionary<string, JsonSchemaBase> DefinitionsRaw
        {
            get { return Definitions != null && Definitions.Count > 0 ? Definitions : null; }
            set { Definitions = value ?? new Dictionary<string, JsonSchemaBase>(); }
        }

        [JsonProperty("allOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<JsonSchemaBase> AllOfRaw
        {
            get { return AllOf != null && AllOf.Count > 0 ? AllOf : null; }
            set { AllOf = value ?? new Collection<JsonSchemaBase>(); }
        }

        [JsonProperty("anyOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<JsonSchemaBase> AnyOfRaw
        {
            get { return AnyOf != null && AnyOf.Count > 0 ? AnyOf : null; }
            set { AnyOf = value ?? new Collection<JsonSchemaBase>(); }
        }

        [JsonProperty("oneOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal ICollection<JsonSchemaBase> OneOfRaw
        {
            get { return OneOf != null && OneOf.Count > 0 ? OneOf : null; }
            set { OneOf = value ?? new Collection<JsonSchemaBase>(); }
        }

        #endregion

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext ctx)
        {
            Initialize();
        }

        private void Initialize()
        {
            if (Properties == null)
                Properties = new ObservableDictionary<string, JsonProperty>();

            if (Definitions == null)
                Definitions = new Dictionary<string, JsonSchemaBase>();

            if (RequiredProperties == null)
                RequiredProperties = new Collection<string>();

            if (AllOf == null)
                AllOf = new Collection<JsonSchemaBase>();

            if (AnyOf == null)
                AnyOf = new Collection<JsonSchemaBase>();

            if (OneOf == null)
                OneOf = new Collection<JsonSchemaBase>();

            InitializeProperties();
        }

        private void InitializeProperties()
        {
            if (Properties != null)
            {
                foreach (var property in Properties)
                {
                    property.Value.Key = property.Key;
                    property.Value.Parent = this;
                }
            }
        }

        private static SimpleType ConvertSimpleTypeFromString(string value)
        {
            return JsonConvert.DeserializeObject<SimpleType>("\"" + value + "\""); // TODO: Improve performance
        }
    }
}