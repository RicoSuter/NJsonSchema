namespace NJsonSchema.CodeGeneration.Models
{
    internal class PropertyModelBase
    {
        public PropertyModelBase(JsonProperty property)
        {
            DefaultValue = GetDefaultValue(property);
        }

        internal static string GetDefaultValue(JsonSchema4 property)
        {
            if (property.Default == null)
                return null;

            if (property.Type.HasFlag(JsonObjectType.String))
                return "\"" + property.Default + "\"";
            else if (property.Type.HasFlag(JsonObjectType.Boolean))
                return property.Default.ToString().ToLower();
            else if (property.Type.HasFlag(JsonObjectType.Integer) ||
                     property.Type.HasFlag(JsonObjectType.Number) ||
                     property.Type.HasFlag(JsonObjectType.Integer))
                return property.Default.ToString();
            return null;
        }

        public bool HasDefaultValue => !string.IsNullOrEmpty(DefaultValue);

        public string DefaultValue { get; set; }
    }
}