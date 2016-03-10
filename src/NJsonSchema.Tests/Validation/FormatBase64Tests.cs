using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class FormatBase64Tests
    {
        [TestMethod]
        public void Validation_should_fail_if_string_is_not_base64_formatted()
        {
            //// Arrange
            var schema = new JsonSchema4
                         {
                             Type = JsonObjectType.String,
                             Format = "base64"
                         };

            var token = new JValue("invalid");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(ValidationErrorKind.Base64Expected, errors.Single().Kind);
        }

        [TestMethod]
        public void Validation_should_fail_if_string_is_not_byte_formatted()
        {
            //// Arrange
            var schema = new JsonSchema4
                         {
                             Type = JsonObjectType.String,
                             Format = JsonFormatStrings.Byte
                         };

            var token = new JValue("invalid");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(ValidationErrorKind.Base64Expected, errors.Single().Kind);
        }

        [TestMethod]
        public void Validation_should_succeed_if_string_is_base64_formatted_with_trailing_equals()
        {
            //// Arrange
            var schema = new JsonSchema4
                         {
                             Type = JsonObjectType.String,
                             Format = "base64"
                         };

            var value = Convert.ToBase64String(new byte[] { 101, 22, 87, 25 });
            var token = new JValue(value);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void Validation_should_succeed_if_string_is_byte_formatted_with_trailing_equals()
        {
            //// Arrange
            var schema = new JsonSchema4
                         {
                             Type = JsonObjectType.String,
                             Format = JsonFormatStrings.Byte
                         };

            var value = Convert.ToBase64String(new byte[] { 101, 22, 87, 25 });
            var token = new JValue(value);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void Validation_should_succeed_if_string_is_base64_formatted_without_trailing_equals()
        {
            //// Arrange
            var schema = new JsonSchema4
                         {
                             Type = JsonObjectType.String,
                             Format = "base64"
                         };

            var value = Convert.ToBase64String(new byte[] { 1, 2, 3 });
            var token = new JValue(value);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void Validation_should_succeed_if_string_is_byte_formatted_without_trailing_equals()
        {
            //// Arrange
            var schema = new JsonSchema4
                         {
                             Type = JsonObjectType.String,
                             Format = JsonFormatStrings.Byte
                         };

            var value = Convert.ToBase64String(new byte[] { 1, 2, 3 });
            var token = new JValue(value);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void Numeric_type_should_not_trigger_validation_if_has_byte_format()
        {
            //// Arrange
            var numericSchema = new JsonSchema4
                                {
                                    Type = JsonObjectType.Integer,
                                    Format = JsonFormatStrings.Byte
                                };

            var token = new JValue(1);

            //// Act
            var numericErrors = numericSchema.Validate(token);

            //// Assert
            Assert.AreEqual(0, numericErrors.Count);
        }
    }
}
