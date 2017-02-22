//-----------------------------------------------------------------------
// <copyright file="PropertyModelBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.CodeGeneration.Models
{
    /// <summary>The property template model base class.</summary>
    public abstract class PropertyModelBase
    {
        private readonly ClassTemplateModelBase _classTemplateModel;
        private readonly JsonProperty _property;
        private readonly DefaultValueGenerator _defaultValueGenerator;
        private readonly CodeGeneratorSettingsBase _settings;

        /// <summary>Initializes a new instance of the <see cref="PropertyModelBase"/> class.</summary>
        /// <param name="property">The property.</param>
        /// <param name="classTemplateModel">The class template model.</param>
        /// <param name="defaultValueGenerator">The default value generator.</param>
        /// <param name="settings">The settings.</param>
        protected PropertyModelBase(
            JsonProperty property,
            ClassTemplateModelBase classTemplateModel,
            DefaultValueGenerator defaultValueGenerator,
            CodeGeneratorSettingsBase settings)
        {
            _classTemplateModel = classTemplateModel;
            _property = property;
            _defaultValueGenerator = defaultValueGenerator;
            _settings = settings;

            PropertyName = _settings.PropertyNameGenerator.Generate(_property);
        }

        /// <summary>Gets a value indicating whether the property has default value.</summary>
        public bool HasDefaultValue => !string.IsNullOrEmpty(DefaultValue);

        /// <summary>Gets the type of the property.</summary>
        public abstract string Type { get; }

        /// <summary>Gets the default value as string.</summary>
        public string DefaultValue => _defaultValueGenerator.GetDefaultValue(_property, 
            _property.IsNullable(_settings.NullHandling), Type, _property.Name, _settings.GenerateDefaultValues);

        /// <summary>Gets the name of the property.</summary>
        public string PropertyName { get; set; }

        /// <summary>Gets the type name hint for the property.</summary>
        protected string GetTypeNameHint()
        {
            var propertyName = PropertyName;
            if (_property.IsEnumeration == false)
                return propertyName;

            var className = _classTemplateModel.ActualClass;
            if (className.Contains("Anonymous"))
                return propertyName;

            if (propertyName.StartsWith(className, StringComparison.OrdinalIgnoreCase))
                return propertyName;

            return className + ConversionUtilities.ConvertToUpperCamelCase(PropertyName, false);
        }
    }
}