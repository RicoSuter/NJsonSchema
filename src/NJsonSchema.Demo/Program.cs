using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NJsonSchema.Demo
{
    public class Program
    {
        private static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try { Console.BufferHeight = 2000; } catch { }
            }

            var passes = 0;
            var fails = 0;
            var exceptions = 0;
            var files = Directory.GetFiles("Tests");
            foreach (var file in files)
            {
                Console.WriteLine("File: " + file);
                var data = JsonNode.Parse(File.ReadAllText(file))!.AsArray();
                foreach (var suite in data)
                {
                    var suiteObj = suite!.AsObject();
                    var description = suiteObj["description"]!.GetValue<string>();
                    Console.WriteLine("  Suite: " + description);

                    foreach (var test in suiteObj["tests"]!.AsArray())
                    {
                        var testObj = test!.AsObject();
                        var testDescription = testObj["description"]!.GetValue<string>();
                        var valid = testObj["valid"]!.GetValue<bool>();

                        Console.WriteLine("    Test: " + testDescription);
                        Console.WriteLine("      Valid: " + valid);

                        RunTest(file, suiteObj, testObj["data"], valid, ref fails, ref passes, ref exceptions);
                    }
                }
            }

            Console.WriteLine("Passes: " + passes);
            Console.WriteLine("Fails: " + fails);
            Console.WriteLine("Exceptions: " + exceptions);

            var expectedFails = 11;
            var expectedExceptions = 14;
            if (fails != expectedFails || exceptions != expectedExceptions)
            {
                Console.WriteLine("========================\n" +
                                  "Unexpected result => Some commits changed the outcome! Please check. \n" +
                                  "Expected fails: " + expectedFails + "\n" +
                                  "Expected exceptions: " + expectedExceptions);
            }

            Console.ReadLine();
        }

        private static void RunTest(string file, JsonObject suite, JsonNode value, bool expectedResult, ref int fails, ref int passes, ref int exceptions)
        {
            try
            {
                var schema = JsonSchema.FromJsonAsync(suite["schema"]!.ToJsonString(), Path.GetDirectoryName(file)).GetAwaiter().GetResult();
                var errors = schema.Validate(value != null ? value.ToJsonString() : "null");
                var success = expectedResult ? errors.Count == 0 : errors.Count > 0;

                if (!success)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.WriteLine("      Result: " + success);

                if (!success)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                if (!success)
                {
                    fails++;
                }
                else
                {
                    passes++;
                }
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
