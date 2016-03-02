using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class FormatIpV6Tests
    {
        [TestMethod]
        public void When_format_ipv6_incorrect_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.IpV6;

            var token = new JValue("test");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.IpV6Expected, errors.First().Kind);
        }

        [TestMethod]
        public void When_format_ipv6_correct_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.IpV6;

            var token = new JValue("::1");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }
    }
}