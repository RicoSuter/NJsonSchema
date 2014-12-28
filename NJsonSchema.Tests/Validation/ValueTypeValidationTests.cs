using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class ValueTypeValidationTests
    {
        [TestMethod]
        public void When_string_required_and_string_provided_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;

            var token = new JValue("test");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_format_date_time_incorrect_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.DateTime;

            var token = new JValue("test");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.DateTimeExpected, errors.First().Kind);
        }

        [TestMethod]
        public void When_format_date_time_correct_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.DateTime;

            var token = new JValue("2014-12-01 11:00:01");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_string_required_but_integer_provided_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;

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
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Number;

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
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Number;

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
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Integer;

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
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Integer;

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
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Boolean;

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
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Boolean;

            var token = new JValue("foo");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.BooleanExpected, errors.First().Kind);
        }

        [TestMethod]
        public void When_string_pattern_does_not_match_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Pattern = "aa(.*)aa";

            var token = new JValue("aaccbb");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.PatternMismatch, errors.First().Kind);
        }

        [TestMethod]
        public void When_string_min_length_does_not_match_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.MinLength = 2;

            var token = new JValue("a");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.StringTooShort, errors.First().Kind);
        }

        [TestMethod]
        public void When_string_max_length_does_not_match_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.MaxLength = 2;

            var token = new JValue("aaa");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.StringTooLong, errors.First().Kind);
        }

        [TestMethod]
        public void When_integer_minimum_does_not_match_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Integer;
            schema.Minimum = 2;

            var token = new JValue(1);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.IntegerTooSmall, errors.First().Kind);
        }

        [TestMethod]
        public void When_integer_maximum_does_not_match_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Integer;
            schema.Maximum = 2;

            var token = new JValue(3);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.IntegerTooBig, errors.First().Kind);
        }

        [TestMethod]
        public void When_number_minimum_does_not_match_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Number;
            schema.Minimum = 1.5;

            var token = new JValue(1.4);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.NumberTooSmall, errors.First().Kind);
        }

        [TestMethod]
        public void When_number_maximum_does_not_match_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Number;
            schema.Maximum = 1.5;

            var token = new JValue(1.6);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.NumberTooBig, errors.First().Kind);
        }

        [TestMethod]
        public void When_value_not_in_enumeration_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Enumerations.Add("Red");
            schema.Enumerations.Add("Green");
            schema.Enumerations.Add("Blue");

            var token = new JValue("Yellow");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.ValueNotInEnumeration, errors.First().Kind);
        }

        [TestMethod]
        public void When_value_in_enumeration_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Enumerations.Add("Red");
            schema.Enumerations.Add("Green");
            schema.Enumerations.Add("Blue");

            var token = new JValue("Red");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }
    }
}