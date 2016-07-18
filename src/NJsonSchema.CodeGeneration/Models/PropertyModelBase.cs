namespace NJsonSchema.CodeGeneration.Models
{
    internal class PropertyModelBase
    {
        private readonly JsonProperty _property;
        private readonly DefaultValueGenerator _defaultValueGenerator;
        private readonly CodeGeneratorSettingsBase _settings;

        public PropertyModelBase(JsonProperty property, DefaultValueGenerator defaultValueGenerator, CodeGeneratorSettingsBase settings)
        {
            _property = property;
            _defaultValueGenerator = defaultValueGenerator;
            _settings = settings;
        }

        public bool HasDefaultValue => !string.IsNullOrEmpty(DefaultValue);

        public string DefaultValue => _settings.GenerateDefaultValues ? _defaultValueGenerator.GetDefaultValue(_property, _property.Name) : null;
    }
}