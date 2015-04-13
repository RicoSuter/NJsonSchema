using Newtonsoft.Json;

namespace NJsonSchema.CodeGeneration.Jsdl
{
    public class JsdlParameter : JsonSchema4
    {
        /// <summary>Gets or sets the type of the parameter.</summary>
        [JsonProperty(PropertyName = "parameterType")]
        public JsdlParameterType ParameterType { get; set; }

        /// <summary>Gets or sets the segment position of the parameter value in the URL.</summary>
        [JsonProperty(PropertyName = "segmentPosition", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int? SegmentPosition { get; set; }

        /// <summary>Gets or sets a value indicating whether the parameter is required.</summary>
        [JsonIgnore]
        public bool IsRequired
        {
            get { return IsRequiredRaw.HasValue ? IsRequiredRaw.Value : true; }
            set { IsRequiredRaw = value; }
        }

        /// <summary>Gets or sets the raw required value.</summary>
        [JsonProperty(PropertyName = "isRequired", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        internal bool? IsRequiredRaw { get; set; }
    }
}