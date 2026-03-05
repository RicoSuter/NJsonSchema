using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    public class ConstValidationTests
    {
        [Fact]
        public async Task When_const_is_defined_and_value_matches_then_validation_succeeds()
        {
            // Arrange
            var json = @"{ ""const"": ""person"" }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var errors = schema.Validate(@"""person""");

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task When_const_is_defined_and_value_does_not_match_then_validation_fails()
        {
            // Arrange
            var json = @"{ ""const"": ""person"" }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var errors = schema.Validate(@"""other""");

            // Assert
            Assert.Single(errors);
            Assert.Equal(ValidationErrorKind.ConstantValueMismatch, errors.First().Kind);
        }

        [Fact]
        public async Task When_const_is_integer_and_value_matches_then_validation_succeeds()
        {
            // Arrange
            var json = @"{ ""const"": 42 }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var errors = schema.Validate("42");

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task When_const_is_integer_and_value_does_not_match_then_validation_fails()
        {
            // Arrange
            var json = @"{ ""const"": 42 }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var errors = schema.Validate("99");

            // Assert
            Assert.Single(errors);
            Assert.Equal(ValidationErrorKind.ConstantValueMismatch, errors.First().Kind);
        }

        [Fact]
        public void When_schema_is_serialized_with_const_then_const_is_included()
        {
            // Arrange
            var schema = new JsonSchema
            {
                Const = "person"
            };

            // Act
            var json = schema.ToJson();

            // Assert
            Assert.Contains("\"const\"", json);
            Assert.Contains("\"person\"", json);
        }

        [Fact]
        public async Task When_schema_with_const_is_deserialized_then_const_is_populated()
        {
            // Arrange
            var json = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""cmdType"": { ""const"": ""person"" }
                }
            }";

            // Act
            var schema = await JsonSchema.FromJsonAsync(json);

            // Assert
            Assert.True(schema.Properties["cmdType"].HasConstValue);
            Assert.Equal("person", schema.Properties["cmdType"].Const?.ToString());
        }
    }
}
