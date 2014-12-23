using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class ArrayValidationTests
    {
        [TestMethod]
        public void When_token_is_not_array_then_validation_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Array;
            
            var token = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.ArrayExpected, errors.First().Kind);
        }

        [TestMethod]
        public void When_array_items_are_valid_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Array;
            schema.Items = new JsonSchema4();
            schema.Items.Type = JsonObjectType.String;

            var token = new JArray();
            token.Add(new JValue("Foo"));
            token.Add(new JValue("Bar"));

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_second_item_validation_fails_then_path_should_be_correct()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Array;
            schema.Items = new JsonSchema4();
            schema.Items.Type = JsonObjectType.String;

            var token = new JArray();
            token.Add(new JValue("Foo"));
            token.Add(new JValue(10));

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(1, errors.Count());
            Assert.AreEqual(ValidationErrorKind.StringExpected, errors.First().Kind);
            Assert.AreEqual("[1]", errors.First().Property);
            Assert.AreEqual("[1]", errors.First().Path);
        }

        [TestMethod]
        public void When_max_items_does_not_match_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Array;
            schema.MaxItems = 1;
            schema.Items = new JsonSchema4();
            schema.Items.Type = JsonObjectType.String;

            var token = new JArray();
            token.Add(new JValue("Foo"));
            token.Add(new JValue("Bar"));

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(1, errors.Count());
            Assert.AreEqual(ValidationErrorKind.TooManyItems, errors.First().Kind);
        }

        [TestMethod]
        public void When_min_items_does_not_match_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Array;
            schema.MinItems = 2;
            schema.Items = new JsonSchema4();
            schema.Items.Type = JsonObjectType.String;

            var token = new JArray();
            token.Add(new JValue("Foo"));

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(1, errors.Count());
            Assert.AreEqual(ValidationErrorKind.TooFewItems, errors.First().Kind);
        }

        [TestMethod]
        public void When_unique_items_does_not_match_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Array;
            schema.UniqueItems = true;
            schema.Items = new JsonSchema4();
            schema.Items.Type = JsonObjectType.String;

            var token = new JArray();
            token.Add(new JValue("Foo"));
            token.Add(new JValue("Foo"));

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(1, errors.Count());
            Assert.AreEqual(ValidationErrorKind.ItemsNotUnique, errors.First().Kind);
        }
    }
}
