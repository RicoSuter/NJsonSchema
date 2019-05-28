using System;

namespace NJsonSchema.NrtDemo
{
    class Program
    {
        public class Person
        {
            public string? FirstName { get; set; }

            public string MiddleName { get; set; }

            public string? LastName { get; set; }
        }

        static void Main(string[] args)
        {
            var schema = JsonSchema.FromType<Person>();
            var json = schema.ToJson();

            Console.WriteLine(json);
            Console.ReadKey();
        }
    }
}
