//-----------------------------------------------------------------------
// <copyright file="PropertyModelBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.Models
{
    /// <summary>The property template model base class.</summary>
    public abstract class PropertyModelBase
    {
        private readonly JsonProperty _property;
        private readonly DefaultValueGenerator _defaultValueGenerator;
        private readonly CodeGeneratorSettingsBase _settings;

        /// <summary>Initializes a new instance of the <see cref="PropertyModelBase"/> class.</summary>
        /// <param name="property">The property.</param>
        /// <param name="defaultValueGenerator">The default value generator.</param>
        /// <param name="settings">The settings.</param>
        protected PropertyModelBase(JsonProperty property, DefaultValueGenerator defaultValueGenerator, CodeGeneratorSettingsBase settings)
        {
            _property = property;
            _defaultValueGenerator = defaultValueGenerator;
            _settings = settings;
        }

        /// <summary>Gets a value indicating whether the property has default value.</summary>
        public bool HasDefaultValue => !string.IsNullOrEmpty(DefaultValue);

        /// <summary>Gets the default value as string.</summary>
        public string DefaultValue => _settings.GenerateDefaultValues ? _defaultValueGenerator.GetDefaultValue(_property, _property.Name) : null;
    }
}