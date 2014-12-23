using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema
{
    /// <summary>Class to validate a JSON schema against a given <see cref="JToken"/>. </summary>
    internal class JsonSchemaValidator
    {
        private readonly JsonSchema4 _schema;

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaValidator"/> class. </summary>
        /// <param name="schema">The schema. </param>
        public JsonSchemaValidator(JsonSchema4 schema)
        {
            _schema = schema;
        }

        /// <summary>Validates the given JSON token. </summary>
        /// <param name="token">The token. </param>
        /// <param name="propertyName">The current property name. </param>
        /// <param name="propertyPath">The current property path. </param>
        /// <returns>The list of validation errors. </returns>
        public virtual List<ValidationError> Validate(JToken token, string propertyName, string propertyPath)
        {
            var errors = new List<ValidationError>();

            // TODO: If multiple flags check whether it is either one of them...

            ValidateString(token, propertyName, propertyPath, errors);
            ValidateNumber(token, propertyName, propertyPath, errors);
            ValidateInteger(token, propertyName, propertyPath, errors);
            ValidateBoolean(token, propertyName, propertyPath, errors);
            ValidateNull(token, propertyName, propertyPath, errors);
            ValidateObject(token, propertyName, propertyPath, errors);
            ValidateArray(token, propertyName, propertyPath, errors);

            return errors;
        }

        private void ValidateNull(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(JsonObjectType.Null))
            {
                if (token.Type != JTokenType.Null)
                    errors.Add(new ValidationError(ValidationErrorKind.NullExpected, propertyName, propertyPath));
            }
        }

        private void ValidateString(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(JsonObjectType.String))
            {
                if (token.Type != JTokenType.String)
                    errors.Add(new ValidationError(ValidationErrorKind.StringExpected, propertyName, propertyPath));
                else
                {
                    var value = token.Value<string>(); 

                    if (!string.IsNullOrEmpty(_schema.Pattern))
                    {
                        if (!Regex.IsMatch(value, _schema.Pattern))
                            errors.Add(new ValidationError(ValidationErrorKind.PatternMismatch, propertyName, propertyPath));
                    }

                    if (_schema.MinLength.HasValue && value.Length < _schema.MinLength)
                        errors.Add(new ValidationError(ValidationErrorKind.StringTooShort, propertyName, propertyPath));

                    if (_schema.MaxLength.HasValue && value.Length > _schema.MaxLength)
                        errors.Add(new ValidationError(ValidationErrorKind.StringTooLong, propertyName, propertyPath));

                    if (!string.IsNullOrEmpty(_schema.Format))
                    {
                        DateTime dateTimeResult;
                        if (_schema.Format == JsonFormatStrings.DateTime && !DateTime.TryParse(value, out dateTimeResult))
                            errors.Add(new ValidationError(ValidationErrorKind.DateTimeExpected, propertyName, propertyPath));

                        // TODO: Implement other format types
                        //if (_schema.Format == JsonFormatStrings.Email && !DateTime.TryParse(value, out dateTimeResult))
                        //    errors.Add(new ValidationError(ValidationErrorKind.DateTimeExpected, propertyName, propertyPath));
                    }
                }
            }
        }

        private void ValidateNumber(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(JsonObjectType.Number))
            {
                if (token.Type != JTokenType.Float && token.Type != JTokenType.Integer)
                    errors.Add(new ValidationError(ValidationErrorKind.NumberExpected, propertyName, propertyPath));
                else
                {
                    var value = token.Value<double>();

                    if (_schema.Minimum.HasValue && (_schema.IsExclusiveMinimum ? value <= _schema.Minimum : value < _schema.Minimum))
                        errors.Add(new ValidationError(ValidationErrorKind.IntegerTooSmall, propertyName, propertyPath));

                    if (_schema.Maximum.HasValue && (_schema.IsExclusiveMaximum ? value >= _schema.Maximum : value > _schema.Maximum))
                        errors.Add(new ValidationError(ValidationErrorKind.IntegerTooBig, propertyName, propertyPath));
                }
            }
        }

        private void ValidateInteger(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(JsonObjectType.Integer))
            {
                if (token.Type != JTokenType.Integer)
                    errors.Add(new ValidationError(ValidationErrorKind.IntegerExpected, propertyName, propertyPath));
                else
                {
                    var value = token.Value<int>();

                    if (_schema.Minimum.HasValue && (_schema.IsExclusiveMinimum ? value <= _schema.Minimum : value < _schema.Minimum))
                        errors.Add(new ValidationError(ValidationErrorKind.IntegerTooSmall, propertyName, propertyPath));

                    if (_schema.Maximum.HasValue && (_schema.IsExclusiveMaximum ? value >= _schema.Maximum : value > _schema.Maximum))
                        errors.Add(new ValidationError(ValidationErrorKind.IntegerTooBig, propertyName, propertyPath));
                }
            }
        }

        private void ValidateBoolean(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(JsonObjectType.Boolean))
            {
                if (token.Type != JTokenType.Boolean)
                    errors.Add(new ValidationError(ValidationErrorKind.BooleanExpected, propertyName, propertyPath));
            }
        }

        private void ValidateObject(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(JsonObjectType.Object))
            {
                var obj = token as JObject;
                if (obj != null)
                {
                    foreach (var propertyInfo in _schema.Properties)
                    {
                        var newPropertyPath = propertyName != null ? propertyName + "." + propertyInfo.Key : propertyInfo.Key;

                        var property = obj.Property(propertyInfo.Key);
                        if (property != null)
                        {
                            var propertyValidator = new JsonSchemaValidator(propertyInfo.Value);
                            var propertyErrors = propertyValidator.Validate(property.Value, propertyInfo.Key, newPropertyPath);
                            errors.AddRange(propertyErrors);
                        }
                        else if (propertyInfo.Value.IsRequired)
                            errors.Add(new ValidationError(ValidationErrorKind.PropertyRequired, propertyInfo.Key,
                                newPropertyPath));
                    }
                }
                else
                    errors.Add(new ValidationError(ValidationErrorKind.ObjectExpected, propertyName, propertyPath));
            }
        }

        private void ValidateArray(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(JsonObjectType.Array))
            {
                var array = token as JArray;
                if (array != null)
                {
                    if (_schema.MinItems > 0 && array.Count < _schema.MinItems)
                        errors.Add(new ValidationError(ValidationErrorKind.TooFewItems, propertyName, propertyPath));

                    if (_schema.MaxItems > 0 && array.Count > _schema.MaxItems)
                        errors.Add(new ValidationError(ValidationErrorKind.TooManyItems, propertyName, propertyPath));
                    
                    if (_schema.UniqueItems && array.Count != array.Distinct().Count())
                        errors.Add(new ValidationError(ValidationErrorKind.ItemsNotUnique, propertyName, propertyPath)); // TODO: Is this implementation correct?

                    for (var i = 0; i < array.Count; i++)
                    {
                        var item = array[i];

                        var propertyIndex = string.Format("[{0}]", i);
                        var itemPath = propertyName != null ? propertyName + "." + propertyIndex : propertyIndex;

                        var itemValidator = new JsonSchemaValidator(_schema.Items);
                        var itemErrors = itemValidator.Validate(item, propertyIndex, itemPath);
                        errors.AddRange(itemErrors);
                    }
                }
                else
                    errors.Add(new ValidationError(ValidationErrorKind.ArrayExpected, propertyName, propertyPath));
            }
        }
    }
}