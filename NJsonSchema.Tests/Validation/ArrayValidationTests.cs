using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.DraftV4;
using NJsonSchema.Validation;

namespace JsonSchema4.Tests.Validation
{
    [TestClass]
    public class ArrayValidationTests
    {
        [TestMethod]
        public void When_token_is_not_array_then_validation_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = SimpleType.Array;
            
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
            var schema = new JsonSchema();
            schema.Type = SimpleType.Array;
            schema.Items = new JsonSchemaBase();
            schema.Items.Type = SimpleType.String;

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
            var schema = new JsonSchema();
            schema.Type = SimpleType.Array;
            schema.Items = new JsonSchemaBase();
            schema.Items.Type = SimpleType.String;

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
    }
}
