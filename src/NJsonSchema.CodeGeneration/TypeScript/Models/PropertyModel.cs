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
    /// <summary>The TypeScript property template model.</summary>
    public class PropertyModel : PropertyModelBase
    {
        private readonly string _parentTypeName;
        private readonly TypeScriptGeneratorSettings _settings;
        private readonly JsonProperty _property;
        private readonly TypeScriptTypeResolver _resolver;

        /// <summary>Initializes a new instance of the <see cref="PropertyModel"/> class.</summary>
        /// <param name="property">The property.</param>
        /// <param name="parentTypeName">Name of the parent type.</param>
        /// <param name="resolver">The resolver.</param>
        /// <param name="settings">The settings.</param>
        public PropertyModel(JsonProperty property, string parentTypeName, TypeScriptTypeResolver resolver, TypeScriptGeneratorSettings settings)
            : base(property, new DefaultValueGenerator(resolver), settings)
        {
            _property = property;
            _resolver = resolver;
            _parentTypeName = parentTypeName;
            _settings = settings;
        }

        /// <summary>Gets the name of the property in an interface.</summary>
        public string InterfaceName => _property.Name.Contains("-") ? $"\"{_property.Name}\"" : _property.Name;

        /// <summary>Gets the name of the property.</summary>
        public string PropertyName => ConversionUtilities.ConvertToLowerCamelCase(GetGeneratedPropertyName(), true).Replace("-", "_");

        /// <summary>Gets the type of the property.</summary>
        public string Type => _resolver.Resolve(_property.ActualPropertySchema, _property.IsNullable(_settings.NullHandling), GetGeneratedPropertyName());

        /// <summary>Gets a value indicating whether the property has description.</summary>
        public bool HasDescription => !string.IsNullOrEmpty(Description);

        /// <summary>Gets the description.</summary>
        public string Description => _property.Description;

        /// <summary>Gets a value indicating whether the property type is an array.</summary>
        public bool IsArray => _property.ActualPropertySchema.Type.HasFlag(JsonObjectType.Array);

        /// <summary>Gets the type of the array item.</summary>
        public string ArrayItemType => _resolver.TryResolve(_property.ActualPropertySchema.Item, GetGeneratedPropertyName()) ?? "any";

        /// <summary>Gets a value indicating whether the property is read only.</summary>
        public bool IsReadOnly => _property.IsReadOnly && _settings.GenerateReadOnlyKeywords;

        /// <summary>Gets a value indicating whether the property is an inheritance discriminator.</summary>
        public bool IsDiscriminator => _property.IsInheritanceDiscriminator;

        /// <summary>Gets the data conversion code.</summary>
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
                        IsPropertyNullable = _property.IsNullable(_settings.NullHandling),
                        TypeNameHint = GetGeneratedPropertyName(),
                        Resolver = _resolver,
                        Settings = _settings
                    });
                }
                return string.Empty;
            }
        }

        /// <summary>Gets the data back conversion code.</summary>
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
                        IsPropertyNullable = _property.IsNullable(_settings.NullHandling),
                        TypeNameHint = GetGeneratedPropertyName(),
                        Resolver = _resolver,
                        Settings = _settings
                    });
                }
                return string.Empty;
            }
        }

        private string GetGeneratedPropertyName()
        {
            if (_settings.PropertyNameGenerator != null)
                return _settings.PropertyNameGenerator.Generate(_property);

            return _property.Name;
        }
    }
}