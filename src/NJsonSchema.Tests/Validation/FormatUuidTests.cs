using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class FormatUuidTests
    {
        [Fact]
        public void When_format_uuid_incorrect_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Uuid;

            var token = new JValue("test");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.UuidExpected, errors.First().Kind);
        }

        [Fact]
        public void When_format_uuid_correct_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Uuid;

            var uuid = Guid.NewGuid().ToString(); 
            var token = new JValue(uuid);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }
    }
}