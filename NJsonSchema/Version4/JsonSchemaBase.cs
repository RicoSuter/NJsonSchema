using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Collections;

namespace NJsonSchema.Version4
{
    public class JsonSchemaBase
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchemaBase"/> class. </summary>
        public JsonSchemaBase()
        {
            Initialize();
        }

        [JsonProperty("$schema", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Schema { get; set; }

        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Id { get; set; }

        [JsonProperty("title", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Title { get; set; }

        [JsonProperty("description", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Description { get; set; }

        [JsonProperty("format", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Format { get; set; } // TODO: This is missing in JSON Schema schema

        [JsonProperty("default", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public object Default { get; set; }

        [JsonProperty("multipleOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MultipleOf { get; set; }

        [JsonProperty("maximum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int Maximum { get; set; }

        [JsonProperty("exclusiveMaximum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool ExclusiveMaximum { get; set; }

        [JsonProperty("minimum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int Minimum { get; set; }

        [JsonProperty("exclusiveMinimum", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool ExclusiveMinimum { get; set; }

        [JsonProperty("maxLength", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MaxLength { get; set; }

        [JsonProperty("minLength", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MinLength { get; set; }

        [JsonProperty("pattern", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Pattern { get; set; }

        [JsonProperty("items", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonSchemaBase Items { get; set; }

        [JsonProperty("maxItems", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MaxItems { get; set; }

        [JsonProperty("minItems", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MinItems { get; set; }

        [JsonProperty("uniqueItems", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool UniqueItems { get; set; }

        [JsonProperty("maxProperties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MaxProperties { get; set; }

        [JsonProperty("minProperties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int MinProperties { get; set; }
        
        [JsonIgnore]
        public Collection<string> RequiredProperties { get; private set; }

        [JsonIgnore]
        public IDictionary<string, JsonProperty> Properties { get; private set; }

        [JsonIgnore]
        public IDictionary<string, JsonSchemaBase> Definitions { get; private set; }

        [JsonIgnore]
        public ICollection<JsonSchemaBase> AllOf { get; private set; }

        [JsonIgnore]
        public ICollection<JsonSchemaBase> AnyOf { get; private set; }

        [JsonIgnore]
        public ICollection<JsonSchemaBase> OneOf { get; private set; }

        [JsonIgnore]
        public SimpleType Type { get; set; }

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
        public Collection<string> RequiredPropertiesRaw
        {
            get { return RequiredProperties != null && RequiredProperties.Count > 0 ? RequiredProperties : null; }
            set { RequiredProperties = value; }
        }

        [JsonProperty("properties", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public IDictionary<string, JsonProperty> PropertiesRaw
        {
            get { return Properties != null && Properties.Count > 0 ? Properties : null; }
            set { Properties = value ?? new Dictionary<string, JsonProperty>(); }
        }

        [JsonProperty("definitions", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public IDictionary<string, JsonSchemaBase> DefinitionsRaw
        {
            get { return Definitions != null && Definitions.Count > 0 ? Definitions : null; }
            set { Definitions = value ?? new Dictionary<string, JsonSchemaBase>(); }
        }

        [JsonProperty("allOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public ICollection<JsonSchemaBase> AllOfRaw
        {
            get { return AllOf != null && AllOf.Count > 0 ? AllOf : null; }
            set { AllOf = value ?? new Collection<JsonSchemaBase>(); }
        }

        [JsonProperty("anyOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public ICollection<JsonSchemaBase> AnyOfRaw
        {
            get { return AnyOf != null && AnyOf.Count > 0 ? AnyOf : null; }
            set { AnyOf = value ?? new Collection<JsonSchemaBase>(); }
        }

        [JsonProperty("oneOf", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public ICollection<JsonSchemaBase> OneOfRaw
        {
            get { return OneOf != null && OneOf.Count > 0 ? OneOf : null; }
            set { OneOf = value ?? new Collection<JsonSchemaBase>(); }
        }

        [JsonProperty("not", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public JsonSchemaBase Not { get; private set; }
        
        [OnDeserialized]
        internal void OnDeserialized(StreamingContext ctx)
        {
            Initialize();
        }

        private void Initialize()
        {
            if (Properties == null)
            {
                var properties = new ObservableDictionary<string, JsonProperty>();
                properties.CollectionChanged += (sender, args) => InitializeProperties();

                Properties = properties;
            }

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