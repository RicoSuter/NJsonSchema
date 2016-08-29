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
    /// <summary>The CSharp property template model.</summary>
    public class PropertyModel : PropertyModelBase
    {
        private readonly JsonProperty _property;
        private readonly CSharpGeneratorSettings _settings;
        private readonly CSharpTypeResolver _resolver;

        /// <summary>Initializes a new instance of the <see cref="PropertyModel"/> class.</summary>
        /// <param name="property">The property.</param>
        /// <param name="resolver">The resolver.</param>
        /// <param name="settings">The settings.</param>
        public PropertyModel(JsonProperty property, CSharpTypeResolver resolver, CSharpGeneratorSettings settings)
            : base(property, new DefaultValueGenerator(resolver), settings)
        {
            _property = property;
            _settings = settings;
            _resolver = resolver;

            PropertyName = ConversionUtilities.ConvertToUpperCamelCase(GetGeneratedPropertyName(), true);
        }

        /// <summary>Gets the name of the property.</summary>
        public string Name => _property.Name;

        /// <summary>Gets the type of the property.</summary>
        public string Type => _resolver.Resolve(_property.ActualPropertySchema, _property.IsNullable(_settings.NullHandling), GetGeneratedPropertyName());

        /// <summary>Gets a value indicating whether the property has a description.</summary>
        public bool HasDescription => !string.IsNullOrEmpty(_property.Description);

        /// <summary>Gets the description.</summary>
        public string Description => _property.Description;

        /// <summary>Gets or sets the name of the property.</summary>
        public string PropertyName { get; set; }

        /// <summary>Gets the name of the field.</summary>
        public string FieldName => ConversionUtilities.ConvertToLowerCamelCase(GetGeneratedPropertyName(), true);

        /// <summary>Gets the json property required.</summary>
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
                        return "Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore";
                    else
                        return "Required.Default, NullValueHandling = NullValueHandling.Ignore";
                }
            }
        }

        /// <summary>Gets a value indicating whether to render a required attribute.</summary>
        public bool RenderRequiredAttribute
        {
            get
            {
                if (!_settings.RequiredPropertiesMustBeDefined || !_property.IsRequired || _property.IsNullable(_settings.NullHandling))
                    return false;

                return _property.ActualPropertySchema.IsAnyType ||
                       _property.ActualPropertySchema.Type.HasFlag(JsonObjectType.Object) ||
                       _property.ActualPropertySchema.Type.HasFlag(JsonObjectType.String) ||
                       _property.ActualPropertySchema.Type.HasFlag(JsonObjectType.Array);
            }
        }

        /// <summary>Gets a value indicating whether the property type is string enum.</summary>
        public bool IsStringEnum => _property.ActualPropertySchema.IsEnumeration && _property.ActualPropertySchema.Type == JsonObjectType.String;

        private string GetGeneratedPropertyName()
        {
            if (_settings.PropertyNameGenerator != null)
                return _settings.PropertyNameGenerator.Generate(_property);

            return _property.Name;
        }
    }
}