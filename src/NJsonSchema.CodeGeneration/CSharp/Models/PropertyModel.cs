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

        internal PropertyModel(JsonProperty property, CSharpTypeResolver resolver, CSharpGeneratorSettings settings) 
            : base(property, new DefaultValueGenerator(resolver))
        {
            _property = property;
            _settings = settings;
            _resolver = resolver;
        }

        public string Name => _property.Name;

        public string Type => _resolver.Resolve(_property.ActualPropertySchema, _property.IsNullable(_settings.NullHandling), GetGeneratedPropertyName());

        public bool HasDescription => !string.IsNullOrEmpty(_property.Description);

        public string Description => _property.Description;

        public string PropertyName => ConversionUtilities.ConvertToUpperCamelCase(GetGeneratedPropertyName(), true);

        public string FieldName => ConversionUtilities.ConvertToLowerCamelCase(GetGeneratedPropertyName(), true);

        public string JsonPropertyRequired
        {
            get
            {
                if (_settings.RequiredPropertiesMustBeDefined && _property.IsRequired)
                {
                    if (!_property.IsNullable(_settings.NullHandling))
                        return "Required.Always";
                    else
                        return "Required.AllowNull";
                }
                else
                {
                    if (!_property.IsNullable(_settings.NullHandling))
                        return "Required.DisallowNull";
                    else
                        return "Required.Default";
                }
            }
        }

        public bool RenderRequiredAttribute
        {
            get
            {
                if (_property.IsNullable(_settings.NullHandling))
                    return false;

                return _property.ActualPropertySchema.IsAnyType ||
                       _property.ActualPropertySchema.Type.HasFlag(JsonObjectType.Object) ||
                       _property.ActualPropertySchema.Type.HasFlag(JsonObjectType.String) ||
                       _property.ActualPropertySchema.Type.HasFlag(JsonObjectType.Array);
            }
        }

        public bool IsStringEnum => _property.ActualPropertySchema.IsEnumeration && _property.ActualPropertySchema.Type == JsonObjectType.String;

        private string GetGeneratedPropertyName()
        {
            if (_settings.PropertyNameGenerator != null)
                return _settings.PropertyNameGenerator.Generate(_property);

            return _property.Name;
        }
    }
}