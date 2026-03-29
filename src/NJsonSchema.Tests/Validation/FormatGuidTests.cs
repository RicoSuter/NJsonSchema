using System.Text.Json.Nodes;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    public class FormatGuidTests
    {
        [Fact]
        public void When_format_guid_incorrect_then_validation_succeeds()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Guid;

            var token = JsonValue.Create("test");

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Equal(ValidationErrorKind.GuidExpected, errors.First().Kind);
        }

        [Fact]
        public void When_format_guid_correct_then_validation_succeeds()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Guid;

            var guid = Guid.NewGuid().ToString(); 
            var token = JsonValue.Create(guid);

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Empty(errors);
        }
    }
}