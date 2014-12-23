using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.DraftV4
{
    public class JsonSchemaValidator
    {
        private readonly JsonSchemaBase _schema;

        public JsonSchemaValidator(JsonSchemaBase schema)
        {
            _schema = schema;
        }

        public virtual List<ValidationError> Validate(JToken token, string propertyName, string propertyPath)
        {
            var errors = new List<ValidationError>();

            // TODO: What if multiple flags are available? Then check whether it's either one of them...

            ValidateString(token, propertyName, propertyPath, errors);
            ValidateNumber(token, propertyName, propertyPath, errors);
            ValidateInteger(token, propertyName, propertyPath, errors);
            ValidateBoolean(token, propertyName, propertyPath, errors);
            ValidateNull(token, propertyName, propertyPath, errors);
            ValidateObject(token, propertyName, errors);
            ValidateArray(token, propertyName, errors);

            return errors;
        }

        private void ValidateNull(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(SimpleType.Null))
            {
                if (token.Type != JTokenType.Null)
                    errors.Add(new ValidationError(ValidationErrorKind.NullExpected, propertyName, propertyPath));
            }
        }

        private void ValidateString(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(SimpleType.String))
            {
                if (token.Type != JTokenType.String)
                    errors.Add(new ValidationError(ValidationErrorKind.StringExpected, propertyName, propertyPath));
            }
        }

        private void ValidateNumber(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(SimpleType.Number))
            {
                if (token.Type != JTokenType.Float && token.Type != JTokenType.Integer)
                    errors.Add(new ValidationError(ValidationErrorKind.NumberExpected, propertyName, propertyPath));
            }
        }

        private void ValidateInteger(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(SimpleType.Integer))
            {
                if (token.Type != JTokenType.Integer)
                    errors.Add(new ValidationError(ValidationErrorKind.IntegerExpected, propertyName, propertyPath));
            }
        }

        private void ValidateBoolean(JToken token, string propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(SimpleType.Boolean))
            {
                if (token.Type != JTokenType.Boolean)
                    errors.Add(new ValidationError(ValidationErrorKind.BooleanExpected, propertyName, propertyPath));
            }
        }

        private void ValidateObject(JToken token, string propertyName, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(SimpleType.Object))
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
                    errors.Add(new ValidationError(ValidationErrorKind.ObjectExpected, null, null));
            }
        }

        private void ValidateArray(JToken token, string propertyName, List<ValidationError> errors)
        {
            if (_schema.Type.HasFlag(SimpleType.Array))
            {
                var array = token as JArray;
                if (array != null)
                {
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
                    errors.Add(new ValidationError(ValidationErrorKind.ArrayExpected, null, null));
            }
        }
    }
}