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
        /// <param name="classTemplateModel">The class template model.</param>
        /// <param name="property">The property.</param>
        /// <param name="resolver">The resolver.</param>
        /// <param name="settings">The settings.</param>
        public PropertyModel(ClassTemplateModel classTemplateModel, JsonProperty property, CSharpTypeResolver resolver, CSharpGeneratorSettings settings)
            : base(property, classTemplateModel, new CSharpDefaultValueGenerator(resolver, settings), settings)
        {
            _property = property;
            _settings = settings;
            _resolver = resolver;
        }

        /// <summary>Gets the name of the property.</summary>
        public string Name => _property.Name;

        /// <summary>Gets the type of the property.</summary>
        public override string Type => _resolver.Resolve(_property.ActualPropertySchema, _property.IsNullable(_settings.NullHandling), GetTypeNameHint());

        /// <summary>Gets a value indicating whether the property has a description.</summary>
        public bool HasDescription => !string.IsNullOrEmpty(_property.Description);

        /// <summary>Gets the description.</summary>
        public string Description => _property.Description;

        /// <summary>Gets the name of the field.</summary>
        public string FieldName => "_" + ConversionUtilities.ConvertToLowerCamelCase(PropertyName, true);

        /// <summary>Gets the json property required.</summary>
        public string JsonPropertyRequired
        {
            get
            {
                if (_settings.RequiredPropertiesMustBeDefined && _property.IsRequired)
                {
                    if (!_property.IsNullable(_settings.NullHandling))
                        return "Newtonsoft.Json.Required.Always";
                    else
                        return "Newtonsoft.Json.Required.AllowNull";
                }
                else
                {
                    if (!_property.IsNullable(_settings.NullHandling))
                        return "Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore";
                    else
                        return "Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore";
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
    }
}