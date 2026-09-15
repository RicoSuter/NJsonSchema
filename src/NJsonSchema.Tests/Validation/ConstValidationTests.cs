using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    public class ConstValidationTests
    {
        [Fact]
        public async Task When_object_property_has_const_and_value_does_not_match_then_validation_fails()
        {
            // Arrange
            var json = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""cmdType"": { ""const"": ""person"" }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var matching = schema.Validate(@"{ ""cmdType"": ""person"" }");
            var mismatching = schema.Validate(@"{ ""cmdType"": ""other"" }");

            // Assert
            Assert.Empty(matching);
            Assert.Single(mismatching);
            Assert.Equal(ValidationErrorKind.ConstantValueMismatch, mismatching.First().Kind);
            Assert.Equal("cmdType", mismatching.First().Property);
        }

        [Fact]
        public async Task When_const_is_integer_and_value_is_mathematically_equal_float_then_validation_succeeds()
        {
            // JSON Schema draft 6+ considers 1 and 1.0 equal for const comparison.
            // Arrange
            var schemaInt = await JsonSchema.FromJsonAsync(@"{ ""const"": 42 }");
            var schemaFloat = await JsonSchema.FromJsonAsync(@"{ ""const"": 42.0 }");

            // Act + Assert
            Assert.Empty(schemaInt.Validate("42.0"));
            Assert.Empty(schemaFloat.Validate("42"));
        }

        [Fact]
        public async Task When_const_contains_nested_numeric_then_mathematical_equality_is_used()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(@"{ ""const"": { ""items"": [1, 2, 3] } }");

            // Act
            var matching = schema.Validate(@"{ ""items"": [1.0, 2.0, 3.0] }");

            // Assert
            Assert.Empty(matching);
        }

        [Fact]
        public async Task When_const_is_array_then_deep_equality_is_used()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(@"{ ""const"": [1, 2, 3] }");

            // Act
            var matching = schema.Validate("[1, 2, 3]");
            var mismatching = schema.Validate("[1, 2, 4]");
            var wrongOrder = schema.Validate("[3, 2, 1]");

            // Assert
            Assert.Empty(matching);
            Assert.Single(mismatching);
            Assert.Single(wrongOrder);
        }

        [Fact]
        public async Task When_const_is_object_then_deep_equality_is_used()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(@"{ ""const"": { ""a"": 1, ""b"": ""x"" } }");

            // Act
            var matching = schema.Validate(@"{ ""a"": 1, ""b"": ""x"" }");
            var mismatching = schema.Validate(@"{ ""a"": 1, ""b"": ""y"" }");

            // Assert
            Assert.Empty(matching);
            Assert.Single(mismatching);
        }

        [Fact]
        public void When_const_is_set_to_complex_clr_object_then_validation_does_not_throw()
        {
            // Arrange
            var schema = new JsonSchema { Const = new { name = "foo", value = 1 } };

            // Act + Assert (regression: previously threw because new JValue(object) rejects complex types)
            var errors = schema.Validate(@"{ ""name"": ""foo"", ""value"": 1 }");
            Assert.Empty(errors);
        }

        [Fact]
        public async Task When_const_is_null_then_only_null_value_validates()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(@"{ ""const"": null }");

            // Act
            var matching = schema.Validate("null");
            var mismatching = schema.Validate(@"""something""");

            // Assert
            Assert.Empty(matching);
            Assert.Single(mismatching);
            Assert.Equal(ValidationErrorKind.ConstantValueMismatch, mismatching.First().Kind);
        }

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
