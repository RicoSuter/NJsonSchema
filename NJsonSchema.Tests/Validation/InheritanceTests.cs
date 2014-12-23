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
        public void When_any_of_is_correct_then_it_should_succeed()
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
        public void When_any_of_is_incorrect_then_it_should_succeed()
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
            Assert.AreEqual(ValidationErrorKind.NotAnyOf, errors.First().Kind);
        }
    }
}
