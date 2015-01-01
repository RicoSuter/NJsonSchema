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
        private JsonSchema4 _parent;

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
        public override JsonSchema4 Parent
        {
            get { return _parent; }
            internal set
            {
                var initialize = _parent == null;
                _parent = value;

                if (initialize && InitialIsRequired)
                    IsRequired = InitialIsRequired;
            }
        }

        /// <summary>Gets or sets a value indicating whether the property is required. </summary>
        [JsonIgnore]
        public bool IsRequired
        {
            get { return Parent.RequiredProperties.Contains(Key); }
            set
            {
                if (Parent == null)
                    InitialIsRequired = value;
                else
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

        /// <remarks>Value used to set <see cref="IsRequired"/> property even if parent is not set yet. </remarks>
        [JsonIgnore]
        internal bool InitialIsRequired { get; set; }
    }
}