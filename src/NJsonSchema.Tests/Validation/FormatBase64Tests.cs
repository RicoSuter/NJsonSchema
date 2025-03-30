﻿using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class FormatBase64Tests
    {
        [Fact]
        public void Validation_should_fail_if_string_is_not_base64_formatted()
        {
            // Arrange
            var schema = new JsonSchema
                         {
                             Type = JsonObjectType.String,
                             Format = "base64"
                         };

            var token = new JValue("invalid");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Single(errors);
            Assert.Equal(ValidationErrorKind.Base64Expected, errors.Single().Kind);
        }

        [Fact]
        public void Validation_should_fail_if_string_is_not_byte_formatted()
        {
            // Arrange
            var schema = new JsonSchema
                         {
                             Type = JsonObjectType.String,
                             Format = JsonFormatStrings.Byte
                         };

            var token = new JValue("invalid");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Single(errors);
            Assert.Equal(ValidationErrorKind.Base64Expected, errors.Single().Kind);
        }

        [Fact]
        public void Validation_should_succeed_if_string_is_base64_formatted_with_trailing_equals()
        {
            // Arrange
            var schema = new JsonSchema
                         {
                             Type = JsonObjectType.String,
                             Format = "base64"
                         };

            var value = Convert.ToBase64String([101, 22, 87, 25]);
            var token = new JValue(value);

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void Validation_should_succeed_if_string_is_byte_formatted_with_trailing_equals()
        {
            // Arrange
            var schema = new JsonSchema
                         {
                             Type = JsonObjectType.String,
                             Format = JsonFormatStrings.Byte
                         };

            var value = Convert.ToBase64String([101, 22, 87, 25]);
            var token = new JValue(value);

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void Validation_should_succeed_if_string_is_base64_formatted_without_trailing_equals()
        {
            // Arrange
            var schema = new JsonSchema
                         {
                             Type = JsonObjectType.String,
                             Format = "base64"
                         };

            var value = Convert.ToBase64String([1, 2, 3]);
            var token = new JValue(value);

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void Validation_should_succeed_if_string_is_byte_formatted_without_trailing_equals()
        {
            // Arrange
            var schema = new JsonSchema
                         {
                             Type = JsonObjectType.String,
                             Format = JsonFormatStrings.Byte
                         };

            var value = Convert.ToBase64String([1, 2, 3]);
            var token = new JValue(value);

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void Numeric_type_should_not_trigger_validation_if_has_byte_format()
        {
            // Arrange
            var numericSchema = new JsonSchema
                                {
                                    Type = JsonObjectType.Integer,
                                    Format = JsonFormatStrings.Byte
                                };

            var token = new JValue(1);

            // Act
            var numericErrors = numericSchema.Validate(token);

            // Assert
            Assert.Empty(numericErrors);
        }
    }
}
