using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
