using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Tests.Samples
{
    [TestClass]
    public class SampleTests
    {
        public class Person
        {
            [Required]
            public string FirstName { get; set; }

            [Required]
            public string LastName { get; set; }

            public DateTime Birthday { get; set; }

            public Collection<Job> Jobs { get; set; }
        }

        public class Job
        {
            public string Company { get; set; }
        }

        [TestMethod]
        public void Demo()
        {
            var schema = JsonSchema4.FromType<Person>();
            var schemaJsonData = schema.ToJson();
            var errors = schema.Validate("...");
        }

 //       [TestMethod]
 //       public void When_DateTime_is_available_then_validator_works()
 //       {
 //           //// Arrange
 //           var schemaJson = @"{
 //""$schema"": ""http://json-schema.org/draft-04/schema#"",
 //           ""type"": ""object"",
 //""properties"": {
 //               ""SimpleDate"": {
 //                   ""type"": ""string"",
 //""format"": ""date-time""
 //               },
 //""PatternDate"": {
 //                   ""type"": ""string"",
 //""pattern"" : ""(^[0-9]{4}-[0-9]{1,2}-[0-9]{1,2}T[0-9]{1,2}:[0-9]{1,2}:[0-9]{1,2}Z$|^$)""
 //}
 //           }
 //       }";
 //           var schema = JsonSchema4.FromJson(schemaJson);

 //           var dataJson = @"{
 //""SimpleDate"":""2012-05-18T00: 00:00Z"",
 //""PatternDate"":""2012-11-07T00:00:00Z""
 //}";
 //           var data = JObject.Parse(dataJson);

 //           //// Act
 //           var errors = schema.Validate(data);

 //           //// Assert
 //           Assert.AreEqual(0, errors.Count);
 //       }
    }
}
