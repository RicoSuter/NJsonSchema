//-----------------------------------------------------------------------
// <copyright file="PropertyModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Globalization;
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
        /// <param name="typeResolver">The type resolver.</param>
        /// <param name="settings">The settings.</param>
        public PropertyModel(
            ClassTemplateModel classTemplateModel,
            JsonProperty property,
            CSharpTypeResolver typeResolver,
            CSharpGeneratorSettings settings)
            : base(property, classTemplateModel, typeResolver, settings)
        {
            _property = property;
            _settings = settings;
            _resolver = typeResolver;
        }

        /// <summary>Gets the name of the property.</summary>
        public string Name => _property.Name;

        /// <summary>Gets the type of the property.</summary>
        public override string Type => _resolver.Resolve(_property.ActualTypeSchema, _property.IsNullable(_settings.SchemaType), GetTypeNameHint());

        /// <summary>Gets a value indicating whether the property has a description.</summary>
        public bool HasDescription => !string.IsNullOrEmpty(_property.Description);

        /// <summary>Gets the description.</summary>
        public string Description => _property.Description;

        /// <summary>Gets the name of the field.</summary>
        public string FieldName => "_" + ConversionUtilities.ConvertToLowerCamelCase(PropertyName, true);

        /// <summary>Gets or sets a value indicating whether empty strings are allowed.</summary>
        public bool AllowEmptyStrings =>
            _property.ActualTypeSchema.Type.HasFlag(JsonObjectType.String) &&
            (_property.MinLength == null || _property.MinLength == 0);

        /// <summary>Gets a value indicating whether this is an array property which cannot be null.</summary>
        public bool HasSetter =>
            (_property.IsNullable(_settings.SchemaType) == false && (
                (_property.ActualTypeSchema.IsArray && _settings.GenerateImmutableArrayProperties) ||
                (_property.ActualTypeSchema.IsDictionary && _settings.GenerateImmutableDictionaryProperties)
            )) == false;

        /// <summary>Gets the json property required.</summary>
        public string JsonPropertyRequiredCode
        {
            get
            {
                if (_settings.RequiredPropertiesMustBeDefined && _property.IsRequired)
                {
                    if (!_property.IsNullable(_settings.SchemaType))
                        return "Newtonsoft.Json.Required.Always";
                    else
                        return "Newtonsoft.Json.Required.AllowNull";
                }
                else
                {
                    if (!_property.IsNullable(_settings.SchemaType))
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
                if (!_settings.GenerateDataAnnotations || !_property.IsRequired || _property.IsNullable(_settings.SchemaType))
                    return false;

                return _property.ActualTypeSchema.IsAnyType ||
                       _property.ActualTypeSchema.Type.HasFlag(JsonObjectType.Object) ||
                       _property.ActualTypeSchema.Type.HasFlag(JsonObjectType.String) ||
                       _property.ActualTypeSchema.Type.HasFlag(JsonObjectType.Array);
            }
        }

        /// <summary>Gets a value indicating whether to render a range attribute.</summary>
        public bool RenderRangeAttribute
        {
            get
            {
                if (!_settings.GenerateDataAnnotations)
                    return false;

                if (!_property.ActualTypeSchema.Type.HasFlag(JsonObjectType.Number) && !_property.ActualTypeSchema.Type.HasFlag(JsonObjectType.Integer))
                    return false;

                return _property.Maximum.HasValue || _property.Minimum.HasValue;
            }
        }

        /// <summary>Gets the minimum value of the range attribute.</summary>
        public string RangeMinimumValue
        {
            get
            {
                var format =
                    _property.Format == JsonFormatStrings.Double ||
                    _property.Format == JsonFormatStrings.Float ||
                    _property.Format == JsonFormatStrings.Decimal ||
                    _property.Format == JsonFormatStrings.Long ?
                        JsonFormatStrings.Double : JsonFormatStrings.Integer;
                var type =
                    _property.Format == JsonFormatStrings.Double ||
                    _property.Format == JsonFormatStrings.Decimal ?
                        "double" : "int";

                return _property.Minimum.HasValue
                    ? ValueGenerator.GetNumericValue(_property.Type, _property.Minimum.Value, format)
                    : type + "." + nameof(double.MinValue);
            }
        }

        /// <summary>Gets the maximum value of the range attribute.</summary>
        public string RangeMaximumValue
        {
            get
            {
                var format =
                    _property.Format == JsonFormatStrings.Double ||
                    _property.Format == JsonFormatStrings.Float ||
                    _property.Format == JsonFormatStrings.Decimal ||
                    _property.Format == JsonFormatStrings.Long ?
                        JsonFormatStrings.Double : JsonFormatStrings.Integer;
                var type =
                    _property.Format == JsonFormatStrings.Double ||
                    _property.Format == JsonFormatStrings.Decimal ?
                        "double" : "int";

                return _property.Maximum.HasValue
                    ? ValueGenerator.GetNumericValue(_property.Type, _property.Maximum.Value, format)
                    : type + "." + nameof(double.MaxValue);
            }
        }

        /// <summary>Gets a value indicating whether to render a string length attribute.</summary>
        public bool RenderStringLengthAttribute
        {
            get
            {
                if (!_settings.GenerateDataAnnotations)
                    return false;

                if (_property.IsRequired && _property.MinLength == 1 && _property.MaxLength == null)
                    return false; // handled by RequiredAttribute

                return _property.ActualTypeSchema.Type.HasFlag(JsonObjectType.String) &&
                       (_property.MinLength.HasValue || _property.MaxLength.HasValue);
            }
        }

        /// <summary>Gets the minimum value of the string length attribute.</summary>
        public int StringLengthMinimumValue => _property.MinLength ?? 0;

        /// <summary>Gets the maximum value of the string length attribute.</summary>
        public string StringLengthMaximumValue => _property.MaxLength.HasValue ? _property.MaxLength.Value.ToString(CultureInfo.InvariantCulture) : $"int.{nameof(int.MaxValue)}";

        /// <summary>Gets a value indicating whether to render a regular expression attribute.</summary>
        public bool RenderRegularExpressionAttribute
        {
            get
            {
                if (!_settings.GenerateDataAnnotations)
                    return false;

                return _property.ActualTypeSchema.Type.HasFlag(JsonObjectType.String) &&
                       !string.IsNullOrEmpty(_property.Pattern);
            }
        }

        /// <summary>Gets the regular expression value for the regular expression attribute.</summary>
        public string RegularExpressionValue => _property.Pattern?.Replace("\"", "\"\"");

        /// <summary>Gets a value indicating whether the property type is string enum.</summary>
        public bool IsStringEnum => _property.ActualTypeSchema.IsEnumeration && _property.ActualTypeSchema.Type == JsonObjectType.String;

        /// <summary>Gets a value indicating whether the property should be formatted like a date.</summary>
        public bool IsDate => _property.Format == JsonFormatStrings.Date;
    }
}
