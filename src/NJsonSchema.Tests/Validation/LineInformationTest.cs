using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class LineInformationTest
    {
        private static JsonSchema4 Schema { get; set; }
        private static string Json { get; set; }

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            Schema = JsonSchema4.FromJson(@"{
                ""type"": ""object"",
                ""required"": [""prop1"", ""prop3""],
                ""additionalProperties"": false,
                ""properties"": {
                    ""prop1"": {
                        ""type"": ""string""
                    },
                    ""prop2"": {
                        ""type"": ""number"",
                        ""enum"": [""this"", ""that""]
                    },
                    ""prop3"": {}
                }
            }");

            Json = @"{
                ""prop1"": 12,
                ""prop2"": ""something"",
                ""prop4"": null
            }";
        }

        [TestMethod]
        public void When_validating_from_string_parse_line_information()
        {
            //// Act
            var errors = Schema.Validate(Json);

            //// Assert
            Assert.AreEqual(5, errors.Count, "Five validation errors expected.");
            ValidateErrors(errors, true);
        }

        [TestMethod]
        public void When_validating_from_jtoken_parse_line_information_if_exists()
        {
            //// Act
            var tokenWithInfo = JToken.Parse(Json, new JsonLoadSettings() {LineInfoHandling = LineInfoHandling.Ignore});
            var errorsWithInfo = Schema.Validate(tokenWithInfo);
            var tokenNoInfoParse = JToken.Parse(Json, new JsonLoadSettings() {LineInfoHandling = LineInfoHandling.Load});
            var errorsNoInfoParse = Schema.Validate(tokenNoInfoParse);
            var tokenNoInfoDeserialize = JsonConvert.DeserializeObject<JToken>(Json);
            var errorsNoInfoDeserialize = Schema.Validate(tokenNoInfoDeserialize);

            //// Assert
            ValidateErrors(errorsWithInfo, true);
            ValidateErrors(errorsNoInfoParse, false);
            ValidateErrors(errorsNoInfoDeserialize, false);
        }

        private static void ValidateErrors(IEnumerable<ValidationError> errors, bool hasLineInfo)
        {
            foreach (var error in errors)
            {
                Assert.AreEqual(hasLineInfo, error.HasLineInfo(), "HasLineInfo incorrect.");

                if (hasLineInfo)
                {
                    switch (error.Kind)
                    {
                    case ValidationErrorKind.StringExpected:
                        Assert.AreEqual(2, error.LineNumber, string.Format("Line number unexpected for {0} error.", error.Kind));
                        Assert.AreEqual(27, error.LinePosition, string.Format("Line position unexpected for {0} error.", error.Kind));
                        break;
                    case ValidationErrorKind.NumberExpected:
                        Assert.AreEqual(3, error.LineNumber, string.Format("Line number unexpected for {0} error.", error.Kind));
                        Assert.AreEqual(36, error.LinePosition, string.Format("Line position unexpected for {0} error.", error.Kind));
                        break;
                    case ValidationErrorKind.NotInEnumeration:
                        Assert.AreEqual(3, error.LineNumber, string.Format("Line number unexpected for {0} error.", error.Kind));
                        Assert.AreEqual(36, error.LinePosition, string.Format("Line position unexpected for {0} error.", error.Kind));
                        break;
                    case ValidationErrorKind.PropertyRequired:
                        Assert.AreEqual(1, error.LineNumber, string.Format("Line number unexpected for {0} error.", error.Kind));
                        Assert.AreEqual(1, error.LinePosition, string.Format("Line position unexpected for {0} error.", error.Kind));
                        break;
                    case ValidationErrorKind.NoAdditionalPropertiesAllowed:
                        Assert.AreEqual(1, error.LineNumber, string.Format("Line number unexpected for {0} error.", error.Kind));
                        Assert.AreEqual(1, error.LinePosition, string.Format("Line position unexpected for {0} error.", error.Kind));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(error.Kind));
                    }
                }
                else
                {
                    Assert.AreEqual(0, error.LineNumber, "Line number not zero with no error info.");
                    Assert.AreEqual(0, error.LinePosition, "Line position not zero with no error info.");
                }
            }
        }
    }
}
