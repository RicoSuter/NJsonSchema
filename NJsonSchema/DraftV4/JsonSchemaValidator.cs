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
            if (_schema.Type.HasFlag(SimpleType.String))
            {
                if (token.Type != JTokenType.String)
                    errors.Add(new ValidationError(ValidationErrorKind.StringExpected, propertyName, propertyPath));
            }
            if (_schema.Type.HasFlag(SimpleType.Number))
            {
                if (token.Type != JTokenType.Float && token.Type != JTokenType.Integer)
                    errors.Add(new ValidationError(ValidationErrorKind.NumberExpected, propertyName, propertyPath));
            }
            if (_schema.Type.HasFlag(SimpleType.Integer))
            {
                if (token.Type != JTokenType.Integer)
                    errors.Add(new ValidationError(ValidationErrorKind.IntegerExpected, propertyName, propertyPath));
            }
            if (_schema.Type.HasFlag(SimpleType.Boolean))
            {
                if (token.Type != JTokenType.Boolean)
                    errors.Add(new ValidationError(ValidationErrorKind.BooleanExpected, propertyName, propertyPath));
            }
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
                            errors.Add(new ValidationError(ValidationErrorKind.PropertyRequired, propertyInfo.Key, newPropertyPath));
                    }
                }
                else
                    errors.Add(new ValidationError(ValidationErrorKind.ObjectExpected, null, null));
            }
            if (_schema.Type.HasFlag(SimpleType.Array))
            {
                var array = token as JArray;
                if (array != null)
                {
                    
                }

                throw new NotImplementedException();
            }
            return errors;
        }
    }
}