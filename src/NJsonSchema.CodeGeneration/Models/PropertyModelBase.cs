//-----------------------------------------------------------------------
// <copyright file="PropertyModelBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Globalization;
using System.Linq;

namespace NJsonSchema.CodeGeneration.Models
{
    /// <summary>The property template model base class.</summary>
    public abstract class PropertyModelBase
    {
        private readonly ClassTemplateModelBase _classTemplateModel;
        private readonly JsonSchemaProperty _property;
        private readonly TypeResolverBase _typeResolver;
        private readonly CodeGeneratorSettingsBase _settings;

        /// <summary>Initializes a new instance of the <see cref="PropertyModelBase"/> class.</summary>
        /// <param name="property">The property.</param>
        /// <param name="classTemplateModel">The class template model.</param>
        /// <param name="typeResolver">The type resolver.</param>
        /// <param name="settings">The settings.</param>
        protected PropertyModelBase(
            JsonSchemaProperty property,
            ClassTemplateModelBase classTemplateModel,
            TypeResolverBase typeResolver,
            CodeGeneratorSettingsBase settings)
        {
            _classTemplateModel = classTemplateModel;
            _property = property;
            _settings = settings;
            _typeResolver = typeResolver;

            PropertyName = _settings.PropertyNameGenerator.Generate(_property);
        }

        /// <summary>Gets the type of the property.</summary>
        public abstract string Type { get; }

        /// <summary>Gets the default value generator.</summary>
        public ValueGeneratorBase ValueGenerator => _settings.ValueGenerator;

        /// <summary>Gets a value indicating whether the property has default value.</summary>
        public bool HasDefaultValue => !string.IsNullOrEmpty(DefaultValue);

        /// <summary>Gets the default value as string.</summary>
        public string? DefaultValue => ValueGenerator.GetDefaultValue(_property,
            _property.IsNullable(_settings.SchemaType), Type, _property.Name, _settings.GenerateDefaultValues, _typeResolver);

        /// <summary>Gets the name of the property.</summary>
        public string PropertyName { get; set; }

        /// <summary>Gets a value indicating whether the property is nullable.</summary>
        public virtual bool IsNullable => _property.IsNullable(_settings.SchemaType);

        /// <summary>Gets a value indicating whether the property is required.</summary>
        public bool IsRequired => _property.IsRequired;

        /// <summary>Gets a value indicating whether the property is a string enum array.</summary>
        public bool IsStringEnumArray =>
            _property.ActualTypeSchema.IsArray &&
            _property.ActualTypeSchema.Item != null &&
            _property.ActualTypeSchema.Item.ActualTypeSchema.IsEnumeration &&
            _property.ActualTypeSchema.Item.ActualTypeSchema.Type.IsString() &&
            !_settings.GenerateEnumAsString;

        /// <summary>Gets the property extension data.</summary>
        public IDictionary<string, object?>? ExtensionData => _property.ExtensionData;

        /// <summary>Gets the property description with the declared enum values appended when requested.</summary>
        protected string? GetDescriptionWithEnumValues(bool generateEnumAsString)
        {
            var description = _property.Description;
            if (!generateEnumAsString)
            {
                return description;
            }

            var enumSchema = _property.ActualTypeSchema;
            if (enumSchema.IsArray)
            {
                enumSchema = enumSchema.Item?.ActualTypeSchema;
            }
            else if (enumSchema.IsDictionary)
            {
                enumSchema = enumSchema.AdditionalPropertiesSchema?.ActualTypeSchema;
            }

            if (enumSchema?.IsEnumeration != true || !IsStringEnumSchema(enumSchema))
            {
                return description;
            }

            var values = string.Join(", ", enumSchema.Enumeration.Select(FormatEnumValue));
            var allowedValues = "Allowed values: " + values + ".";
            return string.IsNullOrEmpty(description) ? allowedValues : description + " " + allowedValues;
        }

        /// <summary>Checks whether the schema represents a string-valued enumeration.</summary>
        /// <param name="schema">The schema to check.</param>
        /// <returns><see langword="true"/> when the schema is a string-valued enumeration.</returns>
        protected static bool IsStringEnumSchema(JsonSchema schema)
        {
            return schema.Type.IsString() ||
                   schema.Type == JsonObjectType.None && !schema.Enumeration.All(value => value is int);
        }

        /// <summary>Gets the JSON Schema format property.</summary>
        public string? Format => _property.ActualSchema.Format;

        /// <summary>Gets the type name hint for the property.</summary>
        protected string GetTypeNameHint()
        {
            var propertyName = PropertyName;
            if (!_property.IsEnumeration)
            {
                return propertyName;
            }

            var className = _classTemplateModel.ClassName;
            if (className.Contains("Anonymous"))
            {
                return propertyName;
            }

            if (propertyName.StartsWith(className, StringComparison.OrdinalIgnoreCase))
            {
                return propertyName;
            }

            return className + ConversionUtilities.ConvertToUpperCamelCase(PropertyName, false);
        }

        private static string FormatEnumValue(object? value)
        {
            if (value is null)
            {
                return "null";
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return "\"" + text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("*/", "*\\/") + "\"";
        }
    }
}