using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    public class PatternPropertyValidationTests
    {
        [Fact]
        public void When_there_are_no_properties_matching_pattern_then_validation_fails()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.AllowAdditionalProperties = false;
            schema.PatternProperties.Add("^[a-z]+$", new JsonSchemaProperty() { Type = JsonObjectType.Object});

            var token = new JObject();
            token.Add("123", new JObject());
            token.Add("qwe123", new JObject());

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Equal(2, errors.Count());
            foreach (var validationError in errors)
            {
                Assert.Equal(ValidationErrorKind.NoAdditionalPropertiesAllowed, validationError.Kind);
            }
        }

        [Fact]
        public void When_there_are_properties_matching_pattern_but_types_doesnt_match_then_validation_fails()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.AllowAdditionalProperties = false;
            schema.PatternProperties.Add("^[a-z]+$", new JsonSchemaProperty() { Type = JsonObjectType.Object});

            var token = new JObject();
            token.Add("qwerty", new JArray());
            token.Add("wsad", new JValue("test"));

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Equal(2, errors.Count());
            Assert.True(errors.All(error => error.Kind == ValidationErrorKind.AdditionalPropertiesNotValid));
        }

        [Fact]
        public void When_there_are_properties_matching_pattern_and_types_matches_then_validation_succeds()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.AllowAdditionalProperties = false;
            schema.PatternProperties.Add("^[a-z]+$", new JsonSchemaProperty() { Type = JsonObjectType.Object});

            var token = new JObject();
            token.Add("qwerty", new JObject());
            token.Add("wsad", new JObject());

            // Act
            var errors = schema.Validate(token);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void When_defined_property_value_doesnt_match_pattern_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.Properties.Add("capitallettersonly", new JsonSchemaProperty() { Type = JsonObjectType.String });
            schema.AllowAdditionalProperties = false;
            schema.PatternProperties.Add("^[a-z]+$", new JsonSchemaProperty() { Pattern = "^[A-Z]+$" });

            var token = new JObject();
            token.Add("capitallettersonly", new JValue("lowercase"));

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Single(errors);
            Assert.Equal(ValidationErrorKind.AdditionalPropertiesNotValid, errors.First().Kind);
        }

        [Fact]
        public void When_defined_property_value_matches_pattern_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.Properties.Add("capitallettersonly", new JsonSchemaProperty() { Type = JsonObjectType.String });
            schema.AllowAdditionalProperties = false;
            schema.PatternProperties.Add("^[a-z]+$", new JsonSchemaProperty() { Pattern = "^[A-Z]+$" });

            var token = new JObject();
            token.Add("capitallettersonly", new JValue("UPPERCASE"));

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void When_mixed_properties_match_pattern_then_validation_works()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.Properties.Add("name", new JsonSchemaProperty() { Type = JsonObjectType.String });
            schema.AllowAdditionalProperties = false;
            schema.PatternProperties.Add("^[a-z]+$", new JsonSchemaProperty() { Type = JsonObjectType.String });

            var token = new JObject();
            token.Add("name", new JValue("test"));
            token.Add("extra", new JValue("value"));

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void When_defined_property_doesnt_match_pattern_name_then_pattern_is_not_applied()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.Properties.Add("Property1", new JsonSchemaProperty() { Type = JsonObjectType.String });
            schema.AllowAdditionalProperties = false;
            schema.PatternProperties.Add("^[a-z]+$", new JsonSchemaProperty() { Pattern = "^[A-Z]+$" });

            var token = new JObject();
            token.Add("Property1", new JValue("lowercase"));

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }
    }
}
