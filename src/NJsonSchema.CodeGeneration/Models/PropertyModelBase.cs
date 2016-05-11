namespace NJsonSchema.CodeGeneration.Models
{
    internal class PropertyModelBase
    {
        public PropertyModelBase(JsonProperty property)
        {
            ProcessDefaultValue(property);
        }

        private void ProcessDefaultValue(JsonProperty property)
        {
            if (property.Default != null)
            {
                if (property.Type.HasFlag(JsonObjectType.String))
                    DefaultValue = "\"" + property.Default + "\"";
                else if (property.Type.HasFlag(JsonObjectType.Integer) ||
                         property.Type.HasFlag(JsonObjectType.Number) ||
                         property.Type.HasFlag(JsonObjectType.Boolean) ||
                         property.Type.HasFlag(JsonObjectType.Integer))
                    DefaultValue = property.Default.ToString();
            }
        }

        public bool HasDefaultValue => !string.IsNullOrEmpty(DefaultValue);

        public string DefaultValue { get; set; }
    }
}