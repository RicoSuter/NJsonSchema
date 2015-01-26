//-----------------------------------------------------------------------
// <copyright file="JsonProperty.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json;

namespace NJsonSchema
{
    /// <summary>A description of a JSON property of a JSON object. </summary>
    public class JsonProperty : JsonSchema4
    {
        private JsonSchema4 _parentSchema;

        internal static JsonProperty FromJsonSchema(string key, JsonSchema4 type)
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
        public override JsonSchema4 ParentSchema
        {
            get { return _parentSchema; }
            internal set
            {
                var initialize = _parentSchema == null;
                _parentSchema = value;

                if (initialize && InitialIsRequired)
                    IsRequired = InitialIsRequired;
            }
        }

        /// <summary>Gets or sets a value indicating whether the property is required. </summary>
        [JsonIgnore]
        public bool IsRequired
        {
            get { return ParentSchema.RequiredProperties.Contains(Key); }
            set
            {
                if (ParentSchema == null)
                    InitialIsRequired = value;
                else
                {
                    if (value)
                    {
                        if (!ParentSchema.RequiredProperties.Contains(Key))
                            ParentSchema.RequiredProperties.Add(Key);
                    }
                    else
                    {
                        if (ParentSchema.RequiredProperties.Contains(Key))
                            ParentSchema.RequiredProperties.Remove(Key);
                    }
                }
            }
        }

        /// <remarks>Value used to set <see cref="IsRequired"/> property even if parent is not set yet. </remarks>
        [JsonIgnore]
        internal bool InitialIsRequired { get; set; }
    }
}