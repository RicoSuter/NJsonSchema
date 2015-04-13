using System.Collections.Generic;
using Newtonsoft.Json;

namespace NJsonSchema.CodeGeneration.Jsdl
{
    public class JsdlOperation
	{
        /// <summary>Initializes a new instance of the <see cref="JsdlOperation"/> class.</summary>
		public JsdlOperation()
		{
			Parameters = new List<JsdlParameter>();
		}

        /// <summary>Gets or sets the partial URL to call this operation.</summary>
        [JsonProperty(PropertyName = "target", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string Target { get; set; }

        /// <summary>Gets or sets the operation's HTTP method.</summary>
        [JsonProperty(PropertyName = "method", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public JsdlOperationMethod Method { get; set; }

        /// <summary>Gets or sets the description.</summary>
        [JsonProperty(PropertyName = "description", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string Description { get; set; }

        /// <summary>Gets or sets the parameters.</summary>
        [JsonProperty(PropertyName = "parameters", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<JsdlParameter> Parameters { get; set; }

        /// <summary>Gets the type of the content type.</summary>
		[JsonIgnore]
		public string ContentType
		{
			get { return ContentTypeRaw ?? "application/json"; }
            set { ContentTypeRaw = value; }
		}

        /// <summary>Gets or sets the type of the returned value.</summary>
        [JsonProperty(PropertyName = "returns", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public JsonSchema4 Returns { get; set; }

        /// <summary>Gets or sets the maximum age.</summary>
		[JsonIgnore]
		public int? MaxAge
		{
			get { return MaxAgeRaw.HasValue ? MaxAgeRaw.Value : 0; }
            set { MaxAgeRaw = value; }
		}

        [JsonProperty(PropertyName = "contentType", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal string ContentTypeRaw { get; set; }

        [JsonProperty(PropertyName = "maxAge", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal int? MaxAgeRaw { get; set; }
    }
}