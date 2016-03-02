using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class FormatIpV4Tests
    {
        [TestMethod]
        public void When_format_ipv4_incorrect_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.IpV4;

            var token = new JValue("test");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.IpV4Expected, errors.First().Kind);
        }

        [TestMethod]
        public void When_format_ipv4_correct_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.IpV4;

            var token = new JValue("192.168.0.1");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }
    }
}