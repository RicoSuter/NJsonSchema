using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class PatternPropertyValidationTests
    {
        [TestMethod]
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
            Assert.AreEqual(2, errors.Count());
            foreach (var validationError in errors)
            {
                Assert.AreEqual(ValidationErrorKind.NoAdditionalPropertiesAllowed, validationError.Kind);
            }
        }

        [TestMethod]
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
            Assert.AreEqual(2, errors.Count());
            Assert.IsTrue(errors.All(error => error.Kind == ValidationErrorKind.AdditionalPropertiesNotValid));
        }

        [TestMethod]
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
            Assert.AreEqual(0, errors.Count());
        }
        
    }
}
