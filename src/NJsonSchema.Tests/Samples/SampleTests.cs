using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NJsonSchema.CodeGeneration.TypeScript;

namespace NJsonSchema.Tests.Samples
{
    [TestClass]
    public class SampleTests
    {
        public class Person
        {
            [Required]
            public string FirstName { get; set; }

            public string MiddleName { get; set; }

            [Required]
            public string LastName { get; set; }

            public Gender Gender { get; set; }

            [Range(2, 5)]
            public int NumberWithRange { get; set; }

            public DateTime Birthday { get; set; }

            public Company Company { get; set; }

            public Collection<Car> Cars { get; set; }
        }

        public enum Gender
        {
            Male,
            Female
        }

        public class Car
        {
            public string Name { get; set; }

            public Company Manufacturer { get; set; }
        }

        public class Company
        {
            public string Name { get; set; }
        }

        [TestMethod]
        public void Demo()
        {
            var schema = JsonSchema4.FromType<Person>();
            var schemaJsonData = schema.ToJson();
            var errors = schema.Validate("{}");
            var generator = new TypeScriptGenerator(schema);
            var code = generator.GenerateFile();
        }

        [TestMethod]
        public void When_JSON_contains_DateTime_is_available_then_string_validator_validates_correctly()
        {
            //// Arrange
            var schemaJson = @"{
            ""$schema"": ""http://json-schema.org/draft-04/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""SimpleDate"": {
                    ""type"": ""string"",
                    ""format"": ""date-time""
                },
                ""PatternDate"": {
                    ""type"": ""string"",
                    ""pattern"" : ""(^[0-9]{4}-[0-9]{1,2}-[0-9]{1,2}T[0-9]{1,2}:[0-9]{1,2}:[0-9]{1,2}Z$|^$)""
                    }
                }
            }";
            var schema = JsonSchema4.FromJson(schemaJson);

            var dataJson = @"{
                ""SimpleDate"":""2012-05-18T00:00:00Z"",
                ""PatternDate"":""2012-11-07T00:00:00Z""
            }"; 

            //// Act
            var errors = schema.Validate(dataJson);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void When_JSON_contains_DateTime_is_available_then_JObject_validator_validates_correctly()
        {
            //// Arrange
            var schemaJson = @"{
            ""$schema"": ""http://json-schema.org/draft-04/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""SimpleDate"": {
                    ""type"": ""string"",
                    ""format"": ""date-time""
                },
                ""PatternDate"": {
                    ""type"": ""string"",
                    ""pattern"" : ""(^[0-9]{4}-[0-9]{1,2}-[0-9]{1,2}T[0-9]{1,2}:[0-9]{1,2}:[0-9]{1,2}Z$|^$)""
                    }
                }
            }";
            var schema = JsonSchema4.FromJson(schemaJson);

            var data = JObject.Parse(@"{
                ""SimpleDate"":""2012-05-18T00:00:00Z"",
                ""PatternDate"":""2012-11-07T00:00:00Z""
            }");

            var value = data["SimpleDate"].Value<string>(); // not original format

            //// Act
            var errors = schema.Validate(data);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }
    }
}
