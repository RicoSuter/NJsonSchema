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
        public void When_tuple_correct_then_it_should_pass()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Array;
            schema.Items.Add(new JsonSchema4 { Type = JsonObjectType.String });
            schema.Items.Add(new JsonSchema4 { Type = JsonObjectType.Integer });

            var token = new JArray();
            token.Add(new JValue("Foo"));
            token.Add(new JValue(5));

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_tuple_too_large_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Array;
            schema.Items.Add(new JsonSchema4 { Type = JsonObjectType.String });
            schema.Items.Add(new JsonSchema4 { Type = JsonObjectType.Integer });
            schema.AllowAdditionalItems = false;

            var token = new JArray();
            token.Add(new JValue("Foo"));
            token.Add(new JValue(5));
            token.Add(new JValue(5));

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(1, errors.Count());
            Assert.AreEqual(ValidationErrorKind.TooManyItemsInTuple, errors.First().Kind);
        }

        [TestMethod]
        public void When_array_item_are_valid_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Array;
            schema.Item = new JsonSchema4();
            schema.Item.Type = JsonObjectType.String;

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
            schema.Item = new JsonSchema4();
            schema.Item.Type = JsonObjectType.String;

            var token = new JArray();
            token.Add(new JValue("Foo"));
            token.Add(new JValue(10));

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(1, errors.Count());
            Assert.AreEqual(ValidationErrorKind.ArrayItemNotValid, errors.First().Kind);

            var firstItemError = ((ChildSchemaValidationError) errors.First()).Errors.First().Value.First();
            Assert.AreEqual(ValidationErrorKind.StringExpected, firstItemError.Kind);
            Assert.AreEqual("[1]", errors.First().Property);
            Assert.AreEqual("[1]", errors.First().Path);
        }

        [TestMethod]
        public void When_max_item_does_not_match_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Array;
            schema.MaxItems = 1;
            schema.Item = new JsonSchema4();
            schema.Item.Type = JsonObjectType.String;

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
            schema.Item = new JsonSchema4();
            schema.Item.Type = JsonObjectType.String;

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
            schema.Item = new JsonSchema4();
            schema.Item.Type = JsonObjectType.String;

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
