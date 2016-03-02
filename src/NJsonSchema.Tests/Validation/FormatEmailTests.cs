using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class FormatEmailTests
    {
        [TestMethod]
        public void When_format_email_incorrect_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Email;

            var token = new JValue("test");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.EmailExpected, errors.First().Kind);
        }

        [TestMethod]
        public void When_format_email_correct_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Email;

            var token = new JValue("mail@rsuter.com");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }
    }
}