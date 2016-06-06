//-----------------------------------------------------------------------
// <copyright file="PropertyModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using NJsonSchema.CodeGeneration.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Models
{
    internal class PropertyModel : PropertyModelBase
    {
        private readonly JsonProperty _property;
        private readonly CSharpGeneratorSettings _settings;
        private readonly CSharpTypeResolver _resolver;

        internal PropertyModel(JsonProperty property, CSharpTypeResolver resolver, CSharpGeneratorSettings settings) : base(property)
        {
            _property = property;
            _settings = settings;
            _resolver = resolver;
        }

        public string Name => _property.Name;

        public string Type => _resolver.Resolve(_property.ActualPropertySchema, _property.IsNullable(_settings.PropertyNullHandling), GetGeneratedPropertyName());

        public bool HasDescription => !string.IsNullOrEmpty(_property.Description);

        public string Description => _property.Description;

        public string PropertyName => ConversionUtilities.ConvertToUpperCamelCase(GetGeneratedPropertyName());

        public string FieldName => ConversionUtilities.ConvertToLowerCamelCase(GetGeneratedPropertyName());

        public string Required => _property.IsRequired && _settings.RequiredPropertiesMustBeDefined ? "Required.Always" : "Required.Default";

        public bool IsStringEnum => _property.ActualPropertySchema.IsEnumeration && _property.ActualPropertySchema.Type == JsonObjectType.String;

        private string GetGeneratedPropertyName()
        {
            if (_settings.PropertyNameGenerator != null)
                return _settings.PropertyNameGenerator.Generate(_property);

            return _property.Name;
        }
    }
}