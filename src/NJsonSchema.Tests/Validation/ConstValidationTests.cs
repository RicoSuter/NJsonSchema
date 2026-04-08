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

        [Fact]
        public void When_schema_has_no_const_then_const_is_not_serialized()
        {
            // Arrange
            var schema = new JsonSchema { Type = JsonObjectType.String };

            // Act
            var json = schema.ToJson();

            // Assert
            Assert.False(schema.HasConstValue);
            Assert.DoesNotContain("\"const\"", json);
        }

        [Fact]
        public async Task When_schema_has_const_null_then_round_trip_preserves_it()
        {
            // Arrange
            var json = @"{ ""const"": null }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Assert deserialization
            Assert.True(schema.HasConstValue);

            // Act - serialize back
            var serialized = schema.ToJson();

            // Assert round-trip
            Assert.Contains("\"const\"", serialized);

            // Act - deserialize again
            var schema2 = await JsonSchema.FromJsonAsync(serialized);

            // Assert second round-trip
            Assert.True(schema2.HasConstValue);
        }

        [Fact]
        public void When_const_is_set_programmatically_then_serialization_includes_it()
        {
            // Arrange
            var schema = new JsonSchema { Const = "hello" };

            // Act
            var json = schema.ToJson();

            // Assert
            Assert.True(schema.HasConstValue);
            Assert.Contains("\"const\": \"hello\"", json);
        }

        [Fact]
        public void When_const_is_cleared_then_serialization_excludes_it()
        {
            // Arrange
            var schema = new JsonSchema { Const = "hello" };
            schema.HasConstValue = false;

            // Act
            var json = schema.ToJson();

            // Assert
            Assert.DoesNotContain("\"const\"", json);
        }
    }
}
