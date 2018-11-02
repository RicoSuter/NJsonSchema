using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;
using NJsonSchema.Validation.FormatValidators;
using System;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class JsonSchemaValidatorTests
    {
        [Fact]
        public void When_a_null_of_settings_passed_to_ctor_then_argument_null_exception_is_thrown()
        {
            //// Arrange
            JsonSchemaValidatorSettings settings = null;

            //// Act & Assert
            Assert.Throws<ArgumentNullException>(() => new JsonSchemaValidator(settings));
        }

        [Fact]
        public void When_settings_contain_custom_format_validator_then_it_validates()
        {
            //// Arrange
            var settings = new JsonSchemaValidatorSettings();
            var formatValidator = new CustomFormatValidator();
            settings.FormatValidators.Add(formatValidator);
            var validator = new JsonSchemaValidator(settings);
            var schema = new JsonSchema4
            {
                Type = JsonObjectType.String,
                Format = formatValidator.Format
            };

            //// Act
            validator.Validate(@"""test""", schema);

            //// Assert
            Assert.True(formatValidator.WasCalled);
        }

        class CustomFormatValidator : IFormatValidator
        {
            public ValidationErrorKind ValidationErrorKind => ValidationErrorKind.Unknown;

            public bool WasCalled { get; set; }  

            public string Format => "custom";

            public bool IsValid(string value, JTokenType tokenType)
            {
                return WasCalled = true;
            }
        }
    }
}
