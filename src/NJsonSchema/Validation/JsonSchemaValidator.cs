//-----------------------------------------------------------------------
// <copyright file="JsonSchemaValidator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using NJsonSchema.Infrastructure;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NJsonSchema.Validation.FormatValidators;

namespace NJsonSchema.Validation
{
    /// <summary>Class to validate a JSON schema against a given <see cref="JsonNode"/>. </summary>
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
        /// <exception cref="JsonException">Could not deserialize the JSON data.</exception>
        /// <returns>The list of validation errors.</returns>
        public ICollection<ValidationError> Validate(string jsonData, JsonSchema schema, SchemaType schemaType = SchemaType.JsonSchema)
        {
            var documentOptions = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

            string jsonToParse = jsonData;
            JsonNode? jsonObject;
            try
            {
                jsonObject = JsonNode.Parse(jsonData, documentOptions: documentOptions);
            }
            catch (JsonException)
            {
                jsonToParse = Infrastructure.JsonSchemaSerialization.FixLenientJson(jsonData);
                jsonObject = JsonNode.Parse(jsonToParse, documentOptions: documentOptions);
            }

            var errors = Validate(jsonObject, schema, schemaType);

            if (errors.Count > 0)
            {
                JsonSourceLocation.Apply(jsonToParse, jsonObject, errors);
            }

            return errors;
        }

        /// <summary>Validates the given JSON token.</summary>
        /// <param name="token">The token.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="schemaType">The type of the schema.</param>
        /// <returns>The list of validation errors.</returns>
        public ICollection<ValidationError> Validate(JsonNode? token, JsonSchema schema, SchemaType schemaType = SchemaType.JsonSchema)
        {
            return Validate(token, schema.ActualSchema, schemaType, null, string.Empty);
        }

        /// <summary>Validates the given JSON token.</summary>
        /// <param name="token">The token.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="schemaType">The type of the schema.</param>
        /// <param name="propertyName">The current property name.</param>
        /// <param name="propertyPath">The current property path.</param>
        /// <returns>The list of validation errors.</returns>
        protected virtual ICollection<ValidationError> Validate(JsonNode? token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath)
        {
            token = JsonValueNormalization.Normalize(token);
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

        private void ValidateType(JsonNode? token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (token == null && schema.IsNullable(schemaType))
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

        private void ValidateAnyOf(JsonNode? token, JsonSchema schema, string? propertyName, string propertyPath, List<ValidationError> errors)
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

        private void ValidateAllOf(JsonNode? token, JsonSchema schema, string? propertyName, string propertyPath, List<ValidationError> errors)
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

        private void ValidateOneOf(JsonNode? token, JsonSchema schema, string? propertyName, string? propertyPath, List<ValidationError> errors)
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

        private void ValidateNot(JsonNode? token, JsonSchema schema, string? propertyName, string? propertyPath, List<ValidationError> errors)
        {
            if (schema.Not != null && Validate(token, schema.Not).Count == 0)
            {
                errors.Add(new ValidationError(ValidationErrorKind.ExcludedSchemaValidates, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateNull(JsonNode? token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (type.IsNull() && token != null)
            {
                errors.Add(new ValidationError(ValidationErrorKind.NullExpected, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateEnum(JsonNode? token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema.IsNullable(schemaType) && token == null)
            {
                return;
            }

            if (schema.Enumeration.Count > 0 && schema.Enumeration.All(value => !JsonValueComparer.Instance.Equals(JsonValueComparer.ToNode(value), token)))
            {
                errors.Add(new ValidationError(ValidationErrorKind.NotInEnumeration, propertyName, propertyPath, token, schema));
            }
        }

        private void ValidateString(JsonNode? token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            var isString = token is JsonValue v && v.TryGetValue<string>(out _);

            if (isString)
            {
                var value = token!.GetValue<string>();

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
                        && !formatValidators.Any(x => x.IsValid(value, JsonValueKind.String)))
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

        private static void ValidateNumber(JsonNode? token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            var isNumber = IsNumericValue(token);
            var isInteger = IsIntegerValue(token);

            if (type.IsNumber() && !isNumber && !isInteger)
            {
                errors.Add(new ValidationError(ValidationErrorKind.NumberExpected, propertyName, propertyPath, token, schema));
            }

            // Type recognition is exact; conversion is only needed to evaluate arithmetic constraints.
            if (!schema.Minimum.HasValue && !schema.Maximum.HasValue &&
                !schema.ExclusiveMinimum.HasValue && !schema.ExclusiveMaximum.HasValue &&
                !schema.MultipleOf.HasValue)
            {
                return;
            }

            if (isNumber || isInteger)
            {
                try
                {
                    var value = GetDecimalValue(token!);

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
                    var value = GetDoubleValue(token!);

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

        private static void ValidateInteger(JsonNode? token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (type.IsInteger() && !IsIntegerValue(token))
            {
                errors.Add(new ValidationError(ValidationErrorKind.IntegerExpected, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateBoolean(JsonNode? token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (type.IsBoolean() && token?.GetValueKind() is not (JsonValueKind.True or JsonValueKind.False))
            {
                errors.Add(new ValidationError(ValidationErrorKind.BooleanExpected, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateObject(JsonNode? token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (type.IsObject() && token is not JsonObject)
            {
                errors.Add(new ValidationError(ValidationErrorKind.ObjectExpected, propertyName, propertyPath, token, schema));
            }
        }

        private void ValidateProperties(JsonNode? token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            var obj = token as JsonObject;
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
                    TryGetPropertyWithStringComparer(obj, propertyInfo.Key, stringComparer, out var value, out var actualPropertyName))
                {
                    if (value == null && propertyInfo.Value.IsNullable(schemaType))
                    {
                        continue;
                    }

                    var propertyErrors = Validate(value, propertyInfo.Value.ActualSchema, schemaType, propertyInfo.Key, newPropertyPath);
                    JsonSourceLocation.SetNullIdentity(propertyErrors, ((JsonNode)obj, actualPropertyName, -1));
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
                var propertyNames = obj.Select(p => p.Key).ToList();

                JsonSchemaValidator.ValidateMaxProperties(token, propertyNames, schema, propertyName, propertyPath, errors);
                JsonSchemaValidator.ValidateMinProperties(token, propertyNames, schema, propertyName, propertyPath, errors);

                var additionalPropertyNames = propertyNames.Where(p => !schemaPropertyKeys.Contains(p)).ToList();

                ValidatePatternProperties(obj, additionalPropertyNames, schema, schemaType, propertyPath, errors);
                ValidateAdditionalProperties(token, obj, additionalPropertyNames, schema, schemaType, propertyName, propertyPath, errors);
            }
        }

        private static string GetPropertyPath(string propertyPath, string propertyName)
        {
            return !string.IsNullOrEmpty(propertyPath) ? propertyPath + "." + propertyName : propertyName;
        }

        private static void ValidateMaxProperties(JsonNode? token, List<string> propertyNames, JsonSchema schema, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema.MaxProperties > 0 && propertyNames.Count > schema.MaxProperties)
            {
                errors.Add(new ValidationError(ValidationErrorKind.TooManyProperties, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateMinProperties(JsonNode? token, List<string> propertyNames, JsonSchema schema, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema.MinProperties > 0 && propertyNames.Count < schema.MinProperties)
            {
                errors.Add(new ValidationError(ValidationErrorKind.TooFewProperties, propertyName, propertyPath, token, schema));
            }
        }

        private void ValidatePatternProperties(JsonObject obj, List<string> additionalPropertyNames, JsonSchema schema, SchemaType schemaType, string propertyPath, List<ValidationError> errors)
        {
            foreach (var kvp in obj)
            {
                var patternPropertySchema = schema.PatternProperties.FirstOrDefault(p => Regex.IsMatch(kvp.Key, p.Key));
                if (patternPropertySchema.Value != null)
                {
                    var propPath = GetPropertyPath(propertyPath, kvp.Key);
                    var error = TryCreateChildSchemaError(kvp.Value,
                        patternPropertySchema.Value,
                        schemaType,
                        ValidationErrorKind.AdditionalPropertiesNotValid, kvp.Key, propPath, ((JsonNode)obj, kvp.Key, -1));

                    if (error != null)
                    {
                        errors.Add(error);
                    }

                    additionalPropertyNames.Remove(kvp.Key);
                }
            }
        }

        private void ValidateAdditionalProperties(JsonNode? token, JsonObject obj, List<string> additionalPropertyNames, JsonSchema schema, SchemaType schemaType,
            string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema.AdditionalPropertiesSchema != null)
            {
                foreach (var propName in additionalPropertyNames)
                {
                    var propPath = GetPropertyPath(propertyPath, propName);
                    var error = TryCreateChildSchemaError(obj[propName],
                        schema.AdditionalPropertiesSchema,
                        schemaType,
                        ValidationErrorKind.AdditionalPropertiesNotValid, propName, propPath, ((JsonNode)obj, propName, -1));
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                }
            }
            else if (!schema.AllowAdditionalProperties && additionalPropertyNames.Count > 0)
            {
                foreach (var propName in additionalPropertyNames)
                {
                    var newPropertyPath = GetPropertyPath(propertyPath, propName);
                    var propertyToken = new JsonPropertyToken(propName, obj[propName]?.DeepClone(), obj);
                    errors.Add(new ValidationError(ValidationErrorKind.NoAdditionalPropertiesAllowed, propName, newPropertyPath, propertyToken, schema));
                }
            }
        }

        private void ValidateArray(JsonNode? token, JsonSchema schema, SchemaType schemaType, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (token is JsonArray array)
            {
                if (schema.MinItems > 0 && array.Count < schema.MinItems)
                {
                    errors.Add(new ValidationError(ValidationErrorKind.TooFewItems, propertyName, propertyPath, token, schema));
                }

                if (schema.MaxItems > 0 && array.Count > schema.MaxItems)
                {
                    errors.Add(new ValidationError(ValidationErrorKind.TooManyItems, propertyName, propertyPath, token, schema));
                }

                if (schema.UniqueItems && array.Count != array.Distinct(JsonValueComparer.Instance).Count())
                {
                    errors.Add(new ValidationError(ValidationErrorKind.ItemsNotUnique, propertyName, propertyPath, token, schema));
                }

                for (var index = 0; index < array.Count; index++)
                {
                    var item = array[index];
                    var firstError = errors.Count;

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
                    for (var errorIndex = firstError; errorIndex < errors.Count; errorIndex++)
                    {
                        JsonSourceLocation.SetNullIdentity(errors[errorIndex], ((JsonNode)array, (string?)null, index));
                    }
                }
            }
            else if (type.IsArray())
            {
                errors.Add(new ValidationError(ValidationErrorKind.ArrayExpected, propertyName, propertyPath, token, schema));
            }
        }

        private void ValidateAdditionalItems(JsonNode? item, JsonSchema schema, SchemaType schemaType, int index, string? propertyPath, List<ValidationError> errors)
        {
            var items = schema._items;
            if (items.Count > 0)
            {
                var propertyIndex = $"[{index}]";
                if (items.Count > index)
                {
                    var error = TryCreateChildSchemaError(
                        item,
                        items[index],
                        schemaType,
                        ValidationErrorKind.ArrayItemNotValid,
                        propertyIndex,
                        propertyPath + propertyIndex);

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

        private ChildSchemaValidationError? TryCreateChildSchemaError(JsonNode? token, JsonSchema schema, SchemaType schemaType, ValidationErrorKind errorKind, string property, string path, object? sourceIdentity = null)
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

            var error = new ChildSchemaValidationError(errorKind, property, path, errorDictionary, token, schema);
            if (sourceIdentity != null)
            {
                JsonSourceLocation.SetNullIdentity(error, sourceIdentity);
            }
            return error;
        }

        private static bool TryGetPropertyWithStringComparer(JsonObject obj, string propertyName, StringComparer comparer, out JsonNode? value)
        {
            return TryGetPropertyWithStringComparer(obj, propertyName, comparer, out value, out _);
        }

        private static bool TryGetPropertyWithStringComparer(JsonObject obj, string propertyName, StringComparer comparer, out JsonNode? value, out string actualPropertyName)
        {
            actualPropertyName = propertyName;
            if (obj.TryGetPropertyValue(propertyName, out value))
            {
                return true;
            }

            foreach (var kvp in obj)
            {
                if (comparer.Equals(propertyName, kvp.Key))
                {
                    actualPropertyName = kvp.Key;
                    value = kvp.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static bool IsNumericValue(JsonNode? token)
        {
            return token is JsonValue value && value.GetValueKind() == JsonValueKind.Number;
        }

        private static bool IsIntegerValue(JsonNode? token)
        {
            if (token is not JsonValue value)
            {
                return false;
            }

            return JsonNumber.TryCreate(value, out var number) && number!.IsInteger;
        }

        private static decimal GetDecimalValue(JsonNode token)
        {
            if (token is JsonValue jsonValue && jsonValue.TryGetValue<JsonElement>(out var element))
            {
                if (element.TryGetDecimal(out var value))
                {
                    return value;
                }

                // Preserve the existing overflow path for parsed numbers outside decimal range.
                throw new OverflowException();
            }

            // Validate the serialized numeric meaning for every CLR backing, including custom
            // primitive converters. This also avoids the rounding of CLR floating-point casts.
            return decimal.Parse(token.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static double GetDoubleValue(JsonNode token)
        {
            if (token is JsonValue jsonValue && jsonValue.TryGetValue<JsonElement>(out var element))
            {
                return element.GetDouble();
            }

            return double.Parse(token.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}
