using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.DraftV4;
using NJsonSchema.Validation;

namespace JsonSchema4.Tests.Validation
{
    [TestClass]
    public class ValueTypeValidationTests
    {
        [TestMethod]
        public void When_string_required_and_string_provided_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = SimpleType.String;

            var token = new JValue("test");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_string_required_but_integer_provided_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = SimpleType.String;

            var token = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.StringExpected, errors.First().Kind);
        }

        [TestMethod]
        public void When_number_required_and_integer_provided_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = SimpleType.Number;

            var token = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_number_required_but_string_provided_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = SimpleType.Number;

            var token = new JValue("foo");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.NumberExpected, errors.First().Kind);
        }

        [TestMethod]
        public void When_integer_required_and_integer_provided_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = SimpleType.Integer;

            var token = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_integer_required_but_string_provided_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = SimpleType.Integer;

            var token = new JValue("foo");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.IntegerExpected, errors.First().Kind);
        }

        [TestMethod]
        public void When_boolean_required_and_boolean_provided_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = SimpleType.Boolean;

            var token = new JValue(true);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_boolean_required_but_string_provided_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = SimpleType.Boolean;

            var token = new JValue("foo");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.BooleanExpected, errors.First().Kind);
        }
    }
}