//-----------------------------------------------------------------------
// <copyright file="PropertyModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using NJsonSchema.CodeGeneration.Models;

namespace NJsonSchema.CodeGeneration.TypeScript.Models
{
    internal class PropertyModel : PropertyModelBase
    {
        private readonly string _parentTypeName;
        private readonly TypeScriptGeneratorSettings _settings;
        private readonly JsonProperty _property;
        private readonly TypeScriptTypeResolver _resolver;

        public PropertyModel(JsonProperty property, string parentTypeName, TypeScriptTypeResolver resolver, TypeScriptGeneratorSettings settings)
            : base(property)
        {
            _property = property;
            _resolver = resolver;
            _parentTypeName = parentTypeName;
            _settings = settings;
        }
        
        public string InterfaceName => _property.Name.Contains("-") ? $"\"{_property.Name}\"" : _property.Name;

        public string PropertyName => ConversionUtilities.ConvertToLowerCamelCase(GetConvertedPropertyName()).Replace("-", "_");

        public string Type => _resolver.Resolve(_property.ActualPropertySchema, _property.IsNullable(_settings.PropertyNullHandling), GetConvertedPropertyName());

        public string Description => _property.Description;

        public bool HasDescription => !string.IsNullOrEmpty(Description);

        public bool IsArray => _property.ActualPropertySchema.Type.HasFlag(JsonObjectType.Array);

        public string ArrayItemType => _resolver.TryResolve(_property.ActualPropertySchema.Item, GetConvertedPropertyName());

        public bool IsReadOnly => _property.IsReadOnly && _settings.GenerateReadOnlyKeywords;

        public bool IsOptional => !_property.IsRequired;

        public string DataConversionCode
        {
            get
            {
                var typeStyle = _settings.GetTypeStyle(_parentTypeName);
                if (typeStyle != TypeScriptTypeStyle.Interface)
                {
                    return DataConversionGenerator.RenderConvertToClassCode(new DataConversionParameters
                    {
                        Variable = typeStyle == TypeScriptTypeStyle.Class ? "this." + PropertyName : PropertyName + "_",
                        Value = "data[\"" + _property.Name + "\"]",
                        Schema = _property.ActualPropertySchema,
                        IsPropertyNullable = _property.IsNullable(_settings.PropertyNullHandling),
                        TypeNameHint = GetConvertedPropertyName(),
                        Resolver = _resolver
                    });
                }
                return string.Empty;
            }
        }

        public string DataBackConversionCode
        {
            get
            {
                var typeStyle = _settings.GetTypeStyle(_parentTypeName);
                if (typeStyle != TypeScriptTypeStyle.Interface)
                {
                    return DataConversionGenerator.RenderConvertToJavaScriptCode(new DataConversionParameters
                    {
                        Variable = "data[\"" + _property.Name + "\"]",
                        Value = typeStyle == TypeScriptTypeStyle.Class ? "this." + PropertyName : PropertyName + "_",
                        Schema = _property.ActualPropertySchema,
                        IsPropertyNullable = _property.IsNullable(_settings.PropertyNullHandling),
                        TypeNameHint = GetConvertedPropertyName(),
                        Resolver = _resolver
                    });
                }
                return string.Empty;
            }
        }

        private string GetConvertedPropertyName()
        {
            // TODO: Implement conversion, see https://github.com/NJsonSchema/NJsonSchema/issues/96
            return _property.Name;
        }

    }
}