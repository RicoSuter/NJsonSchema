using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Demo
{
    public class Test
    {
        public byte[] Bar { get; set; }

        public byte Foo { get; set; }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            var schema = JsonSchema4.FromJson(File.ReadAllText(@"c:\data\person.json"), @"c:\data");
            var x = JToken.Parse(File.ReadAllText(@"c:\data\data.json"));
            var errors = schema.Validate(x);
            var u = schema.ToJson(); 

            var t = new Test
            {
                Bar = new byte[] { 1, 2, 3 }, 
                Foo = 123
            };
            var json = JsonConvert.SerializeObject(t);
            var foo = Convert.FromBase64String("AQID");

            Console.BufferHeight = 2000;

            var passes = 0;
            var fails = 0;
            var exceptions = 0;
            var files = Directory.GetFiles("Tests");
            foreach (var file in files)
            {
                Console.WriteLine("File: " + file);
                var data = JArray.Parse(File.ReadAllText(file));
                foreach (var suite in data.OfType<JObject>())
                {
                    var description = suite["description"].Value<string>();
                    Console.WriteLine("  Suite: " + description);

                    foreach (var test in suite["tests"].OfType<JObject>())
                    {
                        var testDescription = test["description"].Value<string>();
                        var valid = test["valid"].Value<bool>();

                        Console.WriteLine("    Test: " + testDescription);
                        Console.WriteLine("      Valid: " + valid);

                        //if (testDescription == "both anyOf invalid")
                        RunTest(suite, test["data"], valid, ref fails, ref passes, ref exceptions);
                    }
                }
            }

            Console.WriteLine("Passes: " + passes);
            Console.WriteLine("Fails: " + fails);
            Console.WriteLine("Exceptions: " + exceptions);

            Console.ReadLine();

            //var schema = JsonSchema4.FromType<Person>();
            //var schemaData = schema.ToJson();

            //var jsonToken = JToken.Parse("{}");
            //var errors = schema.Validate(jsonToken);

            //foreach (var error in errors)
            //    Console.WriteLine(error.Path + ": " + error.Kind);

            //schema = JsonSchema4.FromJson(schemaData);

            //Console.ReadLine();
        }

        private static void RunTest(JObject suite, JToken value, bool expectedResult, ref int fails, ref int passes, ref int exceptions)
        {
            try
            {
                var schema = JsonSchema4.FromJson(suite["schema"].ToString());
                var errors = schema.Validate(value);
                var success = expectedResult ? errors.Count == 0 : errors.Count > 0;

                if (!success)
                    Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("      Result: " + success);

                if (!success)
                    Console.ForegroundColor = ConsoleColor.Gray;

                if (!success)
                    fails++;
                else
                    passes++;
            }
            catch (Exception ex)
            {
                exceptions++;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("      Exception: " + ex.GetType().FullName + " => " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }

    public class Person
    {
        [Required]
        public string FirstName { get; set; }

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
}
