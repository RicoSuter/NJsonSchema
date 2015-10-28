using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class FormatHostnameTests
    {
        [TestMethod]
        public void When_format_hostname_incorrect_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Hostname;

            var token = new JValue("foo:bar");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.HostnameExpected, errors.First().Kind);
        }

        [TestMethod]
        public void When_format_hostname_is_ip_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Hostname;

            var token = new JValue("rsuter.com");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }
    }
}