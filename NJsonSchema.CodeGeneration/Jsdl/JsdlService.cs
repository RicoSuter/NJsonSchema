using System.Collections.Generic;
using Newtonsoft.Json;

namespace NJsonSchema.CodeGeneration.Jsdl
{
    public class JsdlService
    {
        /// <summary>Initializes a new instance of the <see cref="JsdlService"/> class.</summary>
        public JsdlService()
        {
            Operations = new Dictionary<string, JsdlOperation>();
            Types = new List<JsonSchema4>();
        }

        [JsonProperty(PropertyName = "title", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Title { get; set; }

        /// <summary>Gets or sets the operations.</summary>
        [JsonProperty(PropertyName = "operations", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public Dictionary<string, JsdlOperation> Operations { get; set; }

        /// <summary>Gets or sets the types.</summary>
        [JsonProperty(PropertyName = "types", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public List<JsonSchema4> Types { get; set; }

        public string ToJson()
        {
            var settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                Formatting = Formatting.Indented
            };
            return JsonConvert.SerializeObject(this, settings);
        }

        public static JsdlService FromJson(string data)
        {
            return JsonConvert.DeserializeObject<JsdlService>(data);
        }
    }
}