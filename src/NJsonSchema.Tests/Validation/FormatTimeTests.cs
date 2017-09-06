using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    /// <summary>
    /// Time format tests according to rfc 3339 full-time
    /// https://tools.ietf.org/html/rfc3339#section-5.6
    /// </summary>
    [TestClass]
    public class FormatTimeTests
    {
        [TestMethod]
        public void When_format_time_incorrect_then_validation_fails()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Time;

            var token = new JValue("10 am");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(ValidationErrorKind.TimeExpected, errors.First().Kind);
        }

        [TestMethod]
        public void When_format_time_positive_offset_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Time;

            var token = new JValue("14:30:00+02:00");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_format_time_has_negative_offset_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Time;

            var token = new JValue("14:30:00-02:00");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_format_time_is_utc_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Time;

            var token = new JValue("14:30:00Z");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_format_time_secfrac_correct_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Time;

            var token = new JValue("14:30:00.1234567+02:00");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        /// <summary>
        /// This allows time without a specified timezone. It represents just the 
        /// partial-time component in the RFC
        /// </summary>
        [TestMethod]
        public void When_format_time_is_not_utc_or_offset_then_validation_succeeds()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.String;
            schema.Format = JsonFormatStrings.Time;

            var token = new JValue("14:30:00");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }
    }
}