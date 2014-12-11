using Newtonsoft.Json;

namespace NJsonSchema.DraftV4
{
    /// <summary>A description of a JSON property of a JSON object. </summary>
    public class JsonProperty : JsonSchemaBase
    {
        /// <summary>Initializes a new instance of the <see cref="JsonProperty"/> class. </summary>
        public JsonProperty()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JsonProperty"/> class. </summary>
        /// <param name="key">The property key. </param>
        /// <param name="type">The type. </param>
        public JsonProperty(string key, JsonSchemaBase type)
        {
            Key = key;

            // TODO: What to do here?
            Schema = type.Schema;
            Id = type.Id;
            Title = type.Title;
            Description = type.Description;
            Type = type.Type;
            Format = type.Format;
            Default = type.Default;
            MultipleOf = type.MultipleOf;
            Maximum = type.Maximum;
            IsExclusiveMaximum = type.IsExclusiveMaximum;
            Minimum = type.Minimum;
            IsExclusiveMinimum = type.IsExclusiveMinimum;
            MaxLength = type.MaxLength;
            MinLength = type.MinLength;
            Pattern = type.Pattern;
            MaxLength = type.MaxLength;
            Items = type.Items;
            MaxItems = type.MaxItems;
            MinItems = type.MinItems;
            UniqueItems = type.UniqueItems;
            MaxProperties = type.MaxProperties;
            MinProperties = type.MinProperties;

            RequiredProperties = type.RequiredProperties;
            Properties = type.Properties;
            Definitions = type.Definitions;
            AllOf = type.AllOf;
            AnyOf = type.AnyOf;
            OneOf = type.OneOf;
            Not = type.Not;
        }
        
        /// <summary>Gets or sets the key of the property. </summary>
        [JsonIgnore]
        public string Key { get; internal set; }

        /// <summary>Gets the parent schema if this is a property schema. </summary>
        [JsonIgnore]
        public JsonSchemaBase Parent { get; internal set; }

        /// <summary>Gets or sets a value indicating whether the property is required. </summary>
        [JsonIgnore]
        public bool IsRequired
        {
            get { return Parent.RequiredProperties.Contains(Key); }
            set
            {
                if (value)
                {
                    if (!Parent.RequiredProperties.Contains(Key))
                        Parent.RequiredProperties.Add(Key);
                }
                else
                {
                    if (Parent.RequiredProperties.Contains(Key))
                        Parent.RequiredProperties.Remove(Key);
                }
            }
        }
    }
}