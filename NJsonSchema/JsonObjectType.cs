using System;
using Newtonsoft.Json;

namespace NJsonSchema
{
    [Flags]
    public enum JsonObjectType
    {
        [JsonProperty("none")]
        None = 0, 

        [JsonProperty("array")]
        Array = 1, 

        [JsonProperty("boolean")]
        Boolean = 2, 

        [JsonProperty("integer")]
        Integer = 4, 

        [JsonProperty("null")]
        Null = 8, 

        [JsonProperty("number")]
        Number = 16, 

        [JsonProperty("object")]
        Object = 32,

        [JsonProperty("string")]
        String = 64, 
    }
}