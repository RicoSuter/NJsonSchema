using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.DraftV4
{
    /// <summary>A description of a JSON property of a JSON object. </summary>
    public class JsonProperty : JsonSchemaBase
    {
        /// <summary>Initializes a new instance of the <see cref="JsonProperty"/> class. </summary>
        public JsonProperty()
        {
        }

        internal static JsonProperty FromJsonSchema(string key, JsonSchemaBase type)
        {
            var data = JsonConvert.SerializeObject(type);
            var property = JsonConvert.DeserializeObject<JsonProperty>(data);
            property.Key = key;
            return property;
        }
        
        /// <summary>Gets or sets the key of the property. </summary>
        [JsonIgnore]
        public string Key { get; internal set; }

        /// <summary>Gets the parent schema of this property schema. </summary>
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