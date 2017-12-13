using System.Linq;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class PatternPropertyValidationTests
    {
        [Fact]
        public void When_there_are_no_properties_matching_pattern_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.AllowAdditionalProperties = false;
            schema.PatternProperties.Add("^[a-z]+$", new JsonProperty() { Type = JsonObjectType.Object});

            var token = new JObject();
            token.Add("123", new JObject());
            token.Add("qwe123", new JObject());

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(2, errors.Count());
            foreach (var validationError in errors)
            {
                Assert.Equal(ValidationErrorKind.NoAdditionalPropertiesAllowed, validationError.Kind);
            }
        }

        [Fact]
        public void When_there_are_properties_matching_pattern_but_types_doesnt_match_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.AllowAdditionalProperties = false;
            schema.PatternProperties.Add("^[a-z]+$", new JsonProperty() { Type = JsonObjectType.Object});

            var token = new JObject();
            token.Add("qwerty", new JArray());
            token.Add("wsad", new JValue("test"));

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(2, errors.Count());
            Assert.True(errors.All(error => error.Kind == ValidationErrorKind.AdditionalPropertiesNotValid));
        }

        [Fact]
        public void When_there_are_properties_matching_pattern_and_types_matches_then_validation_succeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.AllowAdditionalProperties = false;
            schema.PatternProperties.Add("^[a-z]+$", new JsonProperty() { Type = JsonObjectType.Object});

            var token = new JObject();
            token.Add("qwerty", new JObject());
            token.Add("wsad", new JObject());

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }
        
    }
}
