using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class ObjectValidationTests
    {
        [TestMethod]
        public void When_token_is_not_object_then_validation_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonProperty();

            var token = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.ObjectExpected, errors.First().Kind);
        }

        [TestMethod]
        public void When_required_property_is_missing_then_it_should_be_in_error_list()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonProperty
            {
                IsRequired = true,
            };

            var token = new JObject();

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(1, errors.Count());
            Assert.AreEqual("Foo", errors.First().Property);
            Assert.AreEqual("Foo", errors.First().Path);
            Assert.AreEqual(ValidationErrorKind.PropertyRequired, errors.First().Kind);
        }

        [TestMethod]
        public void When_optional_property_is_missing_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonProperty
            {
                IsRequired = false,
            };

            var token = new JObject();

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_string_property_is_available_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonProperty
            {
                IsRequired = true,
                Type = JsonObjectType.String
            };

            var token = new JObject();
            token["Foo"] = new JValue("Bar");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_string_property_required_but_integer_provided_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonProperty
            {
                IsRequired = true,
                Type = JsonObjectType.String
            };

            var token = new JObject();
            token["Foo"] = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.StringExpected, errors.First().Kind);
            Assert.AreEqual("Foo", errors.First().Property);
            Assert.AreEqual("Foo", errors.First().Path);
        }
    }
}