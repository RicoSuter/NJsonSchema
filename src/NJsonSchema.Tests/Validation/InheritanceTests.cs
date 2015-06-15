using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class InheritanceTests
    {
        [TestMethod]
        public void When_is_any_of_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.AnyOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.String
            });
            schema.AnyOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.Integer
            });

            var token = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void When_is_not_any_of_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.AnyOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.String
            });
            schema.AnyOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.Integer
            });

            var token = new JValue(1.5);
            
            //// Act
            var errors = schema.Validate(token);

            //// Assert
            var error = (ChildSchemaValidationError)errors.First();
            Assert.AreEqual(ValidationErrorKind.NotAnyOf, error.Kind);
            Assert.AreEqual(ValidationErrorKind.StringExpected, error.Errors.First().Value.First().Kind);
        }
        
        [TestMethod]
        public void When_is_all_of_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.AnyOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.String
            });
            schema.AnyOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.String
            });

            var token = new JValue("Foo");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void When_is_not_all_of_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.AllOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.String
            });
            schema.AllOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.Integer
            });

            var token = new JValue(5);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.NotAllOf, errors.First().Kind);
        }

        [TestMethod]
        public void When_is_one_of_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.OneOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.String
            });
            schema.OneOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.Integer
            });

            var token = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void When_is_not_one_of_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.OneOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.String
            });
            schema.OneOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.Boolean
            });

            var token = new JValue(5);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.NotOneOf, errors.First().Kind);
        }

        [TestMethod]
        public void When_one_of_matches_multiple_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.OneOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.String
            });
            schema.OneOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.Integer
            });
            schema.OneOf.Add(new JsonSchema4
            {
                Type = JsonObjectType.Integer
            });

            var token = new JValue(5);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.NotOneOf, errors.First().Kind);
        }

        [TestMethod]
        public void When_matches_excluded_schema_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Not = new JsonSchema4
            {
                Type = JsonObjectType.String
            };

            var token = new JValue("Foo");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.ExcludedSchemaValidates, errors.First().Kind);
        }

        [TestMethod]
        public void When_not_matches_excluded_schema_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Not = new JsonSchema4
            {
                Type = JsonObjectType.String
            };

            var token = new JValue(5);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }
    }
}
