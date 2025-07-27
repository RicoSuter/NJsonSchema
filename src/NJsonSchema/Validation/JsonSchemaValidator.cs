//-----------------------------------------------------------------------
// <copyright file="JsonSchemaValidator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation.FormatValidators;

namespace NJsonSchema.Validation
{
    /// <summary>Class to validate a JSON schema against a given <see cref="JToken"/>. </summary>
    public class JsonSchemaValidator
    {
        private readonly Dictionary<string, IFormatValidator[]> _formatValidatorsMap;
        private readonly JsonSchemaValidatorSettings _settings;

        /// <summary>
        /// Initializes JsonSchemaValidator
        /// </summary>
        public JsonSchemaValidator(params IFormatValidator[] customValidators)
            : this(new JsonSchemaValidatorSettings() { FormatValidators = customValidators })
        {
        }

        /// <summary>
        /// Initializes JsonSchemaValidator
        /// </summary>
        public JsonSchemaValidator(JsonSchemaValidatorSettings? settings)
        {
            _settings = settings ?? new JsonSchemaValidatorSettings();
            _formatValidatorsMap = _settings.FormatValidators.GroupBy(x => x.Format).ToDictionary(v => v.Key, v => v.ToArray());
        }

        /// <summary>Validates the given JSON data.</summary>
        /// <param name="jsonData">The json data.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="schemaType">The type of the schema.</param>
        /// <exception cref="JsonReaderException">Could not deserialize the JSON data.</exception>
        /// <returns>The list of validation errors.</returns>
        public ICollection<ValidationError> Validate(string jsonData, JsonSchema schema, SchemaType schemaType = SchemaType.JsonSchema)
        {
            using var reader = new StringReader(jsonData);
            using var jsonReader = new JsonTextReader(reader)
            {
                DateParseHandling = DateParseHandling.None
            };
            var jsonObject = JToken.ReadFrom(jsonReader);
            return Validate(jsonObject, schema, schemaType);
        }

        /// <summary>Validates the given JSON token.</summary>
        /// <param name="token">The token.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="schemaType">The type of the schema.</param>
        /// <returns>The list of validation errors.</returns>
        public ICollection<ValidationError> Validate(JToken token, JsonSchema schema, SchemaType schemaType = SchemaType.JsonSchema)
        {
            return Validate(token, schema.ActualSchema, schemaType, null, token.Path);
        }

        /// <summary>Validates the given JSON token.</summary>
        /// <param name="token">The token.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="schemaType">The type of the schema.</param>
        /// <param name="propertyName">The current property name.</param>
        /// <param name="propertyPath">The current property path.</param>
        /// <returns>The list of validation errors.</returns>
        protected virtual ICollection<ValidationError> Validate(JToken token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath)
        {
            var errors = new List<ValidationError>();

            ValidateAnyOf(token, schema, propertyName, propertyPath, errors);
            ValidateAllOf(token, schema, propertyName, propertyPath, errors);
            ValidateOneOf(token, schema, propertyName, propertyPath, errors);
            ValidateNot(token, schema, propertyName, propertyPath, errors);
            ValidateType(token, schema, schemaType, propertyName, propertyPath, errors);
            JsonSchemaValidator.ValidateEnum(token, schema, schemaType, propertyName, propertyPath, errors);
            ValidateProperties(token, schema, schemaType, propertyName, propertyPath, errors);

            return errors;
        }

        private void ValidateType(JToken token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (token.Type == JTokenType.Null && schema.IsNullable(schemaType))
            {
                return;
            }

            var types = GetTypes(schema).ToDictionary(t => t, ICollection<ValidationError> (t) => []);
            if (types.Count > 1)
            {
                foreach (var type in types)
                {
                    ValidateArray(token, schema, schemaType, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                    ValidateString(token, schema, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                    ValidateNumber(token, schema, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                    JsonSchemaValidator.ValidateInteger(token, schema, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                    ValidateBoolean(token, schema, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                    ValidateNull(token, schema, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                    ValidateObject(token, schema, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                }

                // just one has to validate when multiple types are defined
                if (types.All(t => t.Value.Count > 0))
                {
                    errors.Add(new MultiTypeValidationError(
                        ValidationErrorKind.NoTypeValidates, propertyName, propertyPath, types, token, schema));
                }
            }
            else
            {
                ValidateArray(token, schema, schemaType, schema.Type, propertyName, propertyPath, errors);
                ValidateString(token, schema, schema.Type, propertyName, propertyPath, errors);
                ValidateNumber(token, schema, schema.Type, propertyName, propertyPath, errors);
                JsonSchemaValidator.ValidateInteger(token, schema, schema.Type, propertyName, propertyPath, errors);
                ValidateBoolean(token, schema, schema.Type, propertyName, propertyPath, errors);
                ValidateNull(token, schema, schema.Type, propertyName, propertyPath, errors);
                ValidateObject(token, schema, schema.Type, propertyName, propertyPath, errors);
            }
        }

        private static IEnumerable<JsonObjectType> GetTypes(JsonSchema schema)
        {
            return JsonSchema.JsonObjectTypes.Where(t => schema.Type.HasFlag(t));
        }

        private void ValidateAnyOf(JToken token, JsonSchema schema, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema._anyOf.Count > 0)
            {
                var propertyErrors = schema._anyOf.ToDictionary(s => s, s => Validate(token, s));
                if (propertyErrors.All(s => s.Value.Count != 0))
                {
                    errors.Add(new ChildSchemaValidationError(ValidationErrorKind.NotAnyOf, propertyName, propertyPath, propertyErrors, token, schema));
                }
            }
        }

        private void ValidateAllOf(JToken token, JsonSchema schema, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema._allOf.Count > 0)
            {
                var propertyErrors = schema._allOf.ToDictionary(s => s, s => Validate(token, s));
                if (propertyErrors.Any(s => s.Value.Count != 0))
                {
                    errors.Add(new ChildSchemaValidationError(ValidationErrorKind.NotAllOf, propertyName, propertyPath, propertyErrors, token, schema));
                }
            }
        }

        private void ValidateOneOf(JToken token, JsonSchema schema, string? propertyName, string? propertyPath, List<ValidationError> errors)
        {
            if (schema._oneOf.Count > 0)
            {
                var propertyErrors = schema._oneOf.ToDictionary(s => s, s => Validate(token, s));
                if (propertyErrors.Count(s => s.Value.Count == 0) != 1)
                {
                    errors.Add(new ChildSchemaValidationError(ValidationErrorKind.NotOneOf, propertyName, propertyPath, propertyErrors, token, schema));
                }
            }
        }

        private void ValidateNot(JToken token, JsonSchema schema, string? propertyName, string? propertyPath, List<ValidationError> errors)
        {
            if (schema.Not != null && Validate(token, schema.Not).Count == 0)
            {
                errors.Add(new ValidationError(ValidationErrorKind.ExcludedSchemaValidates, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateNull(JToken token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (type.IsNull() && token != null && token.Type != JTokenType.Null)
            {
                errors.Add(new ValidationError(ValidationErrorKind.NullExpected, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateEnum(JToken token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema.IsNullable(schemaType) && token?.Type == JTokenType.Null)
            {
                return;
            }

            if (schema.Enumeration.Count > 0 && schema.Enumeration.All(v => v?.ToString() != token?.ToString()))
            {
                errors.Add(new ValidationError(ValidationErrorKind.NotInEnumeration, propertyName, propertyPath, token, schema));
            }
        }

        private void ValidateString(JToken token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            var isString = token.Type is JTokenType.String or JTokenType.Date or JTokenType.Guid or JTokenType.TimeSpan or JTokenType.Uri;

            if (isString)
            {
                var value = token.Type switch
                {
                    JTokenType.Date => (token as JValue)?.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture),
                    JTokenType.Uri => (token as JValue)?.ToString(CultureInfo.InvariantCulture),
                    _ => token.Value<string>()
                };

                if (value != null)
                {
                    if (!string.IsNullOrEmpty(schema.Pattern))
                    {
                        if (!Regex.IsMatch(value, schema.Pattern))
                        {
                            errors.Add(new ValidationError(ValidationErrorKind.PatternMismatch, propertyName, propertyPath, token, schema));
                        }
                    }
                    if (schema.MinLength.HasValue && value.Length < schema.MinLength)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.StringTooShort, propertyName, propertyPath, token, schema));
                    }

                    if (schema.MaxLength.HasValue && value.Length > schema.MaxLength)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.StringTooLong, propertyName, propertyPath, token, schema));
                    }

                    if (!string.IsNullOrEmpty(schema.Format)
                        && _formatValidatorsMap.TryGetValue(schema.Format!, out var formatValidators)
                        && !formatValidators.Any(x => x.IsValid(value, token.Type)))
                    {
                        errors.AddRange(formatValidators.Select(x => x.ValidationErrorKind).Distinct()
                            .Select(validationErrorKind => new ValidationError(validationErrorKind, propertyName, propertyPath, token, schema)));
                    }
                }
            }
            else if (type.IsString())
            {
                errors.Add(new ValidationError(ValidationErrorKind.StringExpected, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateNumber(JToken token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (type.IsNumber() && token.Type != JTokenType.Float && token.Type != JTokenType.Integer)
            {
                errors.Add(new ValidationError(ValidationErrorKind.NumberExpected, propertyName, propertyPath, token, schema));
            }

            if (token.Type is JTokenType.Float or JTokenType.Integer)
            {
                try
                {
                    var value = token.Value<decimal>();

                    if (schema.Minimum.HasValue && (schema.IsExclusiveMinimum ? value <= schema.Minimum : value < schema.Minimum))
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooSmall, propertyName, propertyPath, token, schema));
                    }

                    if (schema.Maximum.HasValue && (schema.IsExclusiveMaximum ? value >= schema.Maximum : value > schema.Maximum))
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooBig, propertyName, propertyPath, token, schema));
                    }

                    if (schema.ExclusiveMinimum.HasValue && value <= schema.ExclusiveMinimum)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooSmall, propertyName, propertyPath, token, schema));
                    }

                    if (schema.ExclusiveMaximum.HasValue && value >= schema.ExclusiveMaximum)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooBig, propertyName, propertyPath, token, schema));
                    }

                    if (schema.MultipleOf.HasValue && value % schema.MultipleOf != 0)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberNotMultipleOf, propertyName, propertyPath, token, schema));
                    }
                }
                catch (OverflowException)
                {
                    var value = token.Value<double>();

                    if (schema.Minimum.HasValue && (schema.IsExclusiveMinimum ? value <= (double)schema.Minimum : value < (double)schema.Minimum))
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooSmall, propertyName, propertyPath, token, schema));
                    }

                    if (schema.Maximum.HasValue && (schema.IsExclusiveMaximum ? value >= (double)schema.Maximum : value > (double)schema.Maximum))
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooBig, propertyName, propertyPath, token, schema));
                    }

                    if (schema.ExclusiveMinimum.HasValue && value <= (double)schema.ExclusiveMinimum)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooSmall, propertyName, propertyPath, token, schema));
                    }

                    if (schema.ExclusiveMaximum.HasValue && value >= (double)schema.ExclusiveMaximum)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooBig, propertyName, propertyPath, token, schema));
                    }

                    if (schema.MultipleOf.HasValue && value % (double)schema.MultipleOf != 0)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberNotMultipleOf, propertyName, propertyPath, token, schema));
                    }
                }
            }
        }

        private static void ValidateInteger(JToken token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (type.IsInteger() && token.Type != JTokenType.Integer)
            {
                errors.Add(new ValidationError(ValidationErrorKind.IntegerExpected, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateBoolean(JToken token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (type.IsBoolean() && token.Type != JTokenType.Boolean)
            {
                errors.Add(new ValidationError(ValidationErrorKind.BooleanExpected, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateObject(JToken token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (type.IsObject() && token is not JObject)
            {
                errors.Add(new ValidationError(ValidationErrorKind.ObjectExpected, propertyName, propertyPath, token, schema));
            }
        }

        private void ValidateProperties(JToken token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            var obj = token as JObject;
            if (obj == null && schema.Type.IsNull())
            {
                return;
            }

            var stringComparer = _settings.PropertyStringComparer;

            var schemaPropertyKeys = new HashSet<string>(schema.Properties.Keys, stringComparer);

            foreach (var propertyInfo in schema.Properties)
            {
                var newPropertyPath = GetPropertyPath(propertyPath, propertyInfo.Key);

                if (obj != null && 
                    TryGetPropertyWithStringComparer(obj, propertyInfo.Key, stringComparer, out var value) &&
                    value != null)
                {
                    if (value.Type == JTokenType.Null && propertyInfo.Value.IsNullable(schemaType))
                    {
                        continue;
                    }

                    var propertyErrors = Validate(value, propertyInfo.Value.ActualSchema, schemaType, propertyInfo.Key, newPropertyPath);
                    errors.AddRange(propertyErrors);
                }
                else if (propertyInfo.Value.IsRequired)
                {
                    errors.Add(new ValidationError(ValidationErrorKind.PropertyRequired, propertyInfo.Key, newPropertyPath, token, schema));
                }
            }

            // Properties may be required in a schema without being specified as a property.
            foreach (var requiredProperty in schema.RequiredProperties)
            {
                if (schemaPropertyKeys.Contains(requiredProperty))
                {
                    // The property has already been checked.
                    continue;
                }

                if (obj == null || !TryGetPropertyWithStringComparer(obj, requiredProperty, stringComparer, out _))
                {
                    var newPropertyPath = GetPropertyPath(propertyPath, requiredProperty);
                    errors.Add(new ValidationError(ValidationErrorKind.PropertyRequired, requiredProperty, newPropertyPath, token, schema));
                }
            }

            if (obj != null)
            {
                var properties = obj.Properties().ToList();

                JsonSchemaValidator.ValidateMaxProperties(token, properties, schema, propertyName, propertyPath, errors);
                JsonSchemaValidator.ValidateMinProperties(token, properties, schema, propertyName, propertyPath, errors);

                var additionalProperties = properties.Where(p => !schemaPropertyKeys.Contains(p.Name)).ToList();

                ValidatePatternProperties(additionalProperties, schema, schemaType, errors);
                ValidateAdditionalProperties(token, additionalProperties, schema, schemaType, propertyName, propertyPath, errors);
            }
        }

        private static string GetPropertyPath(string propertyPath, string propertyName)
        {
            return !string.IsNullOrEmpty(propertyPath) ? propertyPath + "." + propertyName : propertyName;
        }

        private static void ValidateMaxProperties(JToken token, List<JProperty> properties, JsonSchema schema, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema.MaxProperties > 0 && properties.Count > schema.MaxProperties)
            {
                errors.Add(new ValidationError(ValidationErrorKind.TooManyProperties, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateMinProperties(JToken token, List<JProperty> properties, JsonSchema schema, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema.MinProperties > 0 && properties.Count < schema.MinProperties)
            {
                errors.Add(new ValidationError(ValidationErrorKind.TooFewProperties, propertyName, propertyPath, token, schema));
            }
        }

        private void ValidatePatternProperties(List<JProperty> additionalProperties, JsonSchema schema, SchemaType schemaType, List<ValidationError> errors)
        {
            foreach (var property in additionalProperties.ToArray())
            {
                var patternPropertySchema = schema.PatternProperties.FirstOrDefault(p => Regex.IsMatch(property.Name, p.Key));
                if (patternPropertySchema.Value != null)
                {
                    var error = TryCreateChildSchemaError(property.Value,
                        patternPropertySchema.Value,
                        schemaType,
                        ValidationErrorKind.AdditionalPropertiesNotValid, property.Name, property.Path);

                    if (error != null)
                    {
                        errors.Add(error);
                    }

                    additionalProperties.Remove(property);
                }
            }
        }

        private void ValidateAdditionalProperties(JToken token, List<JProperty> additionalProperties, JsonSchema schema, SchemaType schemaType,
            string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema.AdditionalPropertiesSchema != null)
            {
                foreach (var property in additionalProperties)
                {
                    var error = TryCreateChildSchemaError(property.Value,
                        schema.AdditionalPropertiesSchema,
                        schemaType,
                        ValidationErrorKind.AdditionalPropertiesNotValid, property.Name, property.Path);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                }
            }
            else if (!schema.AllowAdditionalProperties && additionalProperties.Count > 0)
            {
                foreach (var property in additionalProperties)
                {
                    var newPropertyPath = !string.IsNullOrEmpty(propertyPath) ? propertyPath + "." + property.Name : property.Name;
                    errors.Add(new ValidationError(ValidationErrorKind.NoAdditionalPropertiesAllowed, property.Name, newPropertyPath, property, schema));
                }
            }
        }

        private void ValidateArray(JToken token, JsonSchema schema, SchemaType schemaType, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (token is JArray array)
            {
                if (schema.MinItems > 0 && array.Count < schema.MinItems)
                {
                    errors.Add(new ValidationError(ValidationErrorKind.TooFewItems, propertyName, propertyPath, token, schema));
                }

                if (schema.MaxItems > 0 && array.Count > schema.MaxItems)
                {
                    errors.Add(new ValidationError(ValidationErrorKind.TooManyItems, propertyName, propertyPath, token, schema));
                }

                if (schema.UniqueItems && array.Count != array.Select(a => a.ToString()).Distinct().Count())
                {
                    errors.Add(new ValidationError(ValidationErrorKind.ItemsNotUnique, propertyName, propertyPath, token, schema));
                }

                for (var index = 0; index < array.Count; index++)
                {
                    var item = array[index];

                    var propertyIndex = $"[{index}]";
                    var itemPath = !string.IsNullOrEmpty(propertyPath) ? propertyPath + propertyIndex : propertyIndex;

                    if (schema.Item != null)
                    {
                        var error = TryCreateChildSchemaError(item, schema.Item, schemaType, ValidationErrorKind.ArrayItemNotValid, propertyIndex, itemPath);
                        if (error != null)
                        {
                            errors.Add(error);
                        }
                    }

                    ValidateAdditionalItems(item, schema, schemaType, index, propertyPath, errors);
                }
            }
            else if (type.IsArray())
            {
                errors.Add(new ValidationError(ValidationErrorKind.ArrayExpected, propertyName, propertyPath, token, schema));
            }
        }

        private void ValidateAdditionalItems(JToken item, JsonSchema schema, SchemaType schemaType, int index, string? propertyPath, List<ValidationError> errors)
        {
            if (schema.Items.Count > 0)
            {
                var propertyIndex = $"[{index}]";
                if (schema.Items.Count > index)
                {
                    var error = TryCreateChildSchemaError(item,
                        schema.Items.ElementAt(index),
                        schemaType,
                        ValidationErrorKind.ArrayItemNotValid, propertyIndex, propertyPath + propertyIndex);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                }
                else if (schema.AdditionalItemsSchema != null)
                {
                    var error = TryCreateChildSchemaError(item,
                        schema.AdditionalItemsSchema,
                        schemaType,
                        ValidationErrorKind.AdditionalItemNotValid, propertyIndex, propertyPath + propertyIndex);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                }
                else if (!schema.AllowAdditionalItems)
                {
                    errors.Add(new ValidationError(ValidationErrorKind.TooManyItemsInTuple,
                        propertyIndex, propertyPath + propertyIndex, item, schema));
                }
            }
        }

        private ChildSchemaValidationError? TryCreateChildSchemaError(JToken token, JsonSchema schema, SchemaType schemaType, ValidationErrorKind errorKind, string property, string path)
        {
            var errors = Validate(token, schema.ActualSchema, schemaType, null, path);
            if (errors.Count == 0)
            {
                return null;
            }

            var errorDictionary = new Dictionary<JsonSchema, ICollection<ValidationError>>
            {
                { schema, errors }
            };

            return new ChildSchemaValidationError(errorKind, property, path, errorDictionary, token, schema);
        }

        private static bool TryGetPropertyWithStringComparer(JObject obj, string propertyName, StringComparer comparer, out JToken? value)
        {
            // This method mimics the behavior of the JObject.TryGetValue(string property, StringComparison comparison, out JToken)
            // extension method using a StringComparer class instead of StringComparison enum value.

            if (obj.TryGetValue(propertyName, out value))
            {
                return true;
            }

            foreach (var property in obj.Properties())
            {
                if (comparer.Equals(propertyName, property.Name))
                {
                    value = property.Value;
                    return true;
                }
            }

            return false;
        }
    }
}
