using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Demo
{
    public class Program
    {
        static void Main(string[] args)
        {
            var schema = JsonSchema4.FromType<Person>();
            var schemaData = schema.ToJson();

            var jsonToken = JToken.Parse("{}");
            var errors = schema.Validate(jsonToken);

            foreach (var error in errors)
                Console.WriteLine(error.Path + ": " + error.Kind);

            schema = JsonSchema4.FromJson(schemaData);

            Console.ReadLine();
        }
    }

    public class Person
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public Sex Sex { get; set; }

        public DateTime Birthday { get; set; }

        public Collection<Job> Jobs { get; set; }

        [Range(2, 5)]
        public int Test { get; set; }
    }

    public class Job
    {
        public string Company { get; set; }
    }

    public enum Sex
    {
        Male, 
        Female
    }
}
