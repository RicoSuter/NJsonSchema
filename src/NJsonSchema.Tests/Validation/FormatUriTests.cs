using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class FormatUriTests
    {
        [TestMethod]
        public void When_format_uri_incorrect_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Uri;

            var token = new JValue("test");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.UriExpected, errors.First().Kind);
        }

        [TestMethod]
        public void When_format_uri_correct_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Uri;

            var token = new JValue("http://rsuter.com");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }
    }
}