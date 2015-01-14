//-----------------------------------------------------------------------
// <copyright file="JsonSchemaValidator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

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

            ValidateAnyOf(token, propertyName, propertyPath, errors);
            ValidateAllOf(token, propertyName, propertyPath, errors);
            ValidateOneOf(token, propertyName, propertyPath, errors);
            ValidateNot(token, propertyName, propertyPath, errors);

            ValidateEnum(token, propertyName, propertyPath, errors);
            ValidateString(token, propertyName, propertyPath, errors);
            ValidateNumber(token, propertyName, propertyPath, errors);
            ValidateInteger(token, propertyName, propertyPath, errors);
            ValidateBoolean(token, propertyName, propertyPath, errors);
            ValidateNull(token, propertyName, propertyPath, errors);
            ValidateObject(token, propertyName, propertyPath, errors);
            ValidateArray(token, propertyName, propertyPath, errors);
            ValidateProperties(token, propertyName, propertyPath, errors);

            return errors;
        }

        private bool ValidateAnyOf(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.AnyOf.Count > 0)
            {
                var propertyErrors = _schema.AnyOf.ToDictionary(s => s, s => s.Validate(token));
                if (propertyErrors.All(s => s.Value.Count != 0))
                    errors.Add(new ChildSchemaValidationError(ValidationErrorKind.NotAnyOf, propertyName, propertyPath, propertyErrors));

                return true;
            }
            return false;
        }

        private bool ValidateAllOf(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.AllOf.Count > 0)
            {
                var propertyErrors = _schema.AllOf.ToDictionary(s => s, s => s.Validate(token));
                if (propertyErrors.Any(s => s.Value.Count != 0))
                    errors.Add(new ChildSchemaValidationError(ValidationErrorKind.NotAllOf, propertyName, propertyPath, propertyErrors));

                return true;
            }
            return false;
        }

        private bool ValidateOneOf(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.OneOf.Count > 0)
            {
                var propertyErrors = _schema.OneOf.ToDictionary(s => s, s => s.Validate(token));
                if (propertyErrors.Count(s => s.Value.Count == 0) != 1)
                    errors.Add(new ChildSchemaValidationError(ValidationErrorKind.NotOneOf, propertyName, propertyPath, propertyErrors));

                return true;
            }
            return false;
        }

        private void ValidateNot(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Not != null)
            {
                if (_schema.Not.Validate(token).Count == 0)
                    errors.Add(new ValidationError(ValidationErrorKind.ExcludedSchemaValidates, propertyName, propertyPath));
            }
        }

        private void ValidateNull(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(JsonObjectType.Null))
            {
                if (token != null && token.Type != JTokenType.Null)
                    errors.Add(new ValidationError(ValidationErrorKind.NullExpected, propertyName, propertyPath));
            }
        }

        private void ValidateEnum(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            // TODO: Support other enum types, not only string?
            if (_schema.Enumeration.Count > 0 && !_schema.Enumeration.Contains(token.ToString()))
                errors.Add(new ValidationError(ValidationErrorKind.NotInEnumeration, propertyName, propertyPath));
        }

        private void ValidateString(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(JsonObjectType.String))
            {
                if (token.Type != JTokenType.String && token.Type != JTokenType.Date &&
                    token.Type != JTokenType.Guid && token.Type != JTokenType.TimeSpan &&
                    token.Type != JTokenType.Uri)
                {
                    errors.Add(new ValidationError(ValidationErrorKind.StringExpected, propertyName, propertyPath));
                }
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
                        if (_schema.Format == JsonFormatStrings.DateTime)
                        {
                            DateTime dateTimeResult;
                            if (token.Type != JTokenType.Date && DateTime.TryParse(value, out dateTimeResult) == false)
                                errors.Add(new ValidationError(ValidationErrorKind.DateTimeExpected, propertyName, propertyPath));
                        }

                        if (_schema.Format == JsonFormatStrings.Uri)
                        {
                            Uri uriResult;
                            if (token.Type != JTokenType.Uri && Uri.TryCreate(value, UriKind.Absolute, out uriResult) == false)
                                errors.Add(new ValidationError(ValidationErrorKind.UriExpected, propertyName, propertyPath));
                        }

                        if (_schema.Format == JsonFormatStrings.Email)
                        {
                            var isEmail = Regex.IsMatch(value, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*" +
                                @"@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase);
                            if (!isEmail)
                                errors.Add(new ValidationError(ValidationErrorKind.EmailExpected, propertyName, propertyPath));
                        }

                        // TODO: Implement other format types (hostname, ipv4, ipv6)
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
            }

            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
            {
                var value = token.Value<double>();

                if (_schema.Minimum.HasValue && (_schema.IsExclusiveMinimum ? value <= _schema.Minimum : value < _schema.Minimum))
                    errors.Add(new ValidationError(ValidationErrorKind.NumberTooSmall, propertyName, propertyPath));

                if (_schema.Maximum.HasValue && (_schema.IsExclusiveMaximum ? value >= _schema.Maximum : value > _schema.Maximum))
                    errors.Add(new ValidationError(ValidationErrorKind.NumberTooBig, propertyName, propertyPath));

                if (_schema.MultipleOf.HasValue && value % _schema.MultipleOf != 0)
                    errors.Add(new ValidationError(ValidationErrorKind.NumberNotMultipleOf, propertyName, propertyPath));
            }
        }

        private void ValidateInteger(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(JsonObjectType.Integer))
            {
                if (token.Type != JTokenType.Integer)
                    errors.Add(new ValidationError(ValidationErrorKind.IntegerExpected, propertyName, propertyPath));
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
                if (obj == null)
                    errors.Add(new ValidationError(ValidationErrorKind.ObjectExpected, propertyName, propertyPath));
            }
        }

        private void ValidateProperties(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            var obj = token as JObject;
            foreach (var propertyInfo in _schema.Properties)
            {
                var newPropertyPath = propertyName != null ? propertyName + "." + propertyInfo.Key : propertyInfo.Key;

                var property = obj != null ? obj.Property(propertyInfo.Key) : null;
                if (property != null)
                {
                    var propertyValidator = new JsonSchemaValidator(propertyInfo.Value);
                    var propertyErrors = propertyValidator.Validate(property.Value, propertyInfo.Key, newPropertyPath);
                    errors.AddRange(propertyErrors);
                }
                else if (propertyInfo.Value.IsRequired)
                    errors.Add(new ValidationError(ValidationErrorKind.PropertyRequired, propertyInfo.Key, newPropertyPath));
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

                    var itemValidator = _schema.Item != null ? new JsonSchemaValidator(_schema.Item) : null;
                    for (var i = 0; i < array.Count; i++)
                    {
                        var item = array[i];

                        var propertyIndex = string.Format("[{0}]", i);
                        var itemPath = propertyName != null ? propertyName + "." + propertyIndex : propertyIndex;

                        if (_schema.Item != null && itemValidator != null)
                        {
                            var itemErrors = itemValidator.Validate(item, propertyIndex, itemPath);
                            if (itemErrors.Any())
                            {
                                var errorDictionary = new Dictionary<JsonSchema4, ICollection<ValidationError>>();
                                errorDictionary.Add(_schema.Item, itemErrors);

                                errors.Add(new ChildSchemaValidationError(ValidationErrorKind.ArrayItemNotValid, 
                                    propertyIndex, itemPath, errorDictionary));
                            }
                        }

                        if (_schema.Items != null && _schema.Items.Count > 0)
                        {
                            if (_schema.Items.Count > i)
                            {
                                var tupleItemValidator = new JsonSchemaValidator(_schema.Items.ElementAt(i));
                                var itemErrors = tupleItemValidator.Validate(item, propertyIndex, itemPath);
                                errors.AddRange(itemErrors);
                            }
                            else
                                errors.Add(new ValidationError(ValidationErrorKind.TooManyItemsInTuple, propertyIndex, itemPath));
                        }
                    }
                }
                else
                    errors.Add(new ValidationError(ValidationErrorKind.ArrayExpected, propertyName, propertyPath));
            }
        }
    }
}