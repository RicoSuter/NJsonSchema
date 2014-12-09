using Newtonsoft.Json;

namespace NJsonSchema.Version4
{
    public class JsonProperty : JsonSchemaBase
    {
        /// <summary>Initializes a new instance of the <see cref="JsonProperty"/> class. </summary>
        public JsonProperty()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JsonProperty"/> class. </summary>
        /// <param name="key">The property key. </param>
        public JsonProperty(string key)
        {
            Key = key;
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