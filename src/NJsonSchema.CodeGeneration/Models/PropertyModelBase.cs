namespace NJsonSchema.CodeGeneration.Models
{
    internal class PropertyModelBase
    {
        private readonly JsonProperty _property;
        private readonly DefaultValueGeneratorBase _defaultValueGenerator;

        public PropertyModelBase(JsonProperty property, DefaultValueGeneratorBase defaultValueGenerator)
        {
            _property = property;
            _defaultValueGenerator = defaultValueGenerator; 
        }

        public bool HasDefaultValue => !string.IsNullOrEmpty(DefaultValue);

        public string DefaultValue => _defaultValueGenerator.GetDefaultValue(_property);
    }
}