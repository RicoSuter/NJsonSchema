using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class LineInformationTest
    {
        private JsonSchema4 Schema { get; set; }

        private string Json { get; set; }

        public async Task InitAsync()
        {
            Schema = await JsonSchema4.FromJsonAsync(@"{
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
                    ""prop3"": {},
                    ""prop4"": {}
                },
                ""oneOf"": [
                    {
                        ""properties"": {
                            ""prop4"": { ""uniqueItems"": true }
                        }
                    },
                    {
                        ""properties"": {
                            ""prop2"": { ""minLength"": 123 }
                        }
                    },
                    {
                        ""properties"": {
                            ""prop1"": { ""maximum"": 10 }
                        }
                    }
                ],
                ""allOf"": [
                    {
                        ""properties"": {
                            ""prop1"": { ""minimum"": 22 }
                        }
                    },
                    {
                        ""properties"": {
                            ""prop2"": { ""pattern"": ""anything"" }
                        }
                    }
                ]
            }");

            Json = @"{
                ""prop1"": 12,
                ""prop2"": ""something"",
                ""prop4"": [1,2,3,1],
                ""prop5"": null
            }";
        }

        [TestMethod]
        public async Task When_validating_from_string_parse_line_information()
        {
            //// Arrange
            await InitAsync();

            //// Act
            var errors = Schema.Validate(Json);

            //// Assert
            Assert.AreEqual(7, errors.Count, "Seven validation errors expected.");
            Assert.AreEqual(3, errors.OfType<ChildSchemaValidationError>().Single(error => error.Kind == ValidationErrorKind.NotOneOf).Errors.Count, "Three NotOneOf clause violations expected");
            Assert.AreEqual(2, errors.OfType<ChildSchemaValidationError>().Single(error => error.Kind == ValidationErrorKind.NotAllOf).Errors.Count, "Two NotAllOf clause violations expected");
            ValidateErrors(errors, true);
        }

        [TestMethod]
        public async Task When_validating_from_jtoken_parse_line_information_if_exists()
        {
            //// Arrange
            await InitAsync();

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
                Assert.AreEqual(hasLineInfo, error.HasLineInfo, "HasLineInfo incorrect.");

                if (hasLineInfo)
                {
                    switch (error.Kind)
                    {
                    case ValidationErrorKind.StringExpected:
                        AssertLineNumber(2, 27, error);
                        break;
                    case ValidationErrorKind.NumberExpected:
                        AssertLineNumber(3, 36, error);
                        break;
                    case ValidationErrorKind.NotInEnumeration:
                        AssertLineNumber(3, 36, error);
                        break;
                    case ValidationErrorKind.StringTooShort:
                        AssertLineNumber(3, 36, error);
                        break;
                    case ValidationErrorKind.PatternMismatch:
                        AssertLineNumber(3, 36, error);
                        break;
                    case ValidationErrorKind.PropertyRequired:
                        AssertLineNumber(1, 1, error);
                        break;
                    case ValidationErrorKind.NumberTooBig:
                        AssertLineNumber(2, 27, error);
                        break;
                    case ValidationErrorKind.NumberTooSmall:
                        AssertLineNumber(2, 27, error);
                        break;
                    case ValidationErrorKind.ItemsNotUnique:
                        AssertLineNumber(4, 26, error);
                        break;
                    case ValidationErrorKind.NoAdditionalPropertiesAllowed:
                        AssertLineNumber(5, 24, error);
                        break;
                    case ValidationErrorKind.NotOneOf:
                        AssertLineNumber(1, 1, error);
                        break;
                    case ValidationErrorKind.NotAllOf:
                        AssertLineNumber(1, 1, error);
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

                var childSchemaError = error as ChildSchemaValidationError;
                if (childSchemaError != null)
                {
                    ValidateErrors(childSchemaError.Errors.Values.SelectMany(x => x), hasLineInfo);
                }
            }
        }

        private static void AssertLineNumber(int lineNumber, int linePosition, ValidationError error)
        {
            Assert.AreEqual(lineNumber, error.LineNumber, string.Format("Line number unexpected for {0} error.", error.Kind));
            Assert.AreEqual(linePosition, error.LinePosition, string.Format("Line position unexpected for {0} error.", error.Kind));
        }
    }
}
