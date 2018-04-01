using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NJsonSchema.Demo.Performance
{
    public class Program
    {
        static void Main(string[] args)
        {
            Run().GetAwaiter().GetResult();
        }

        private static async Task Run()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < 200; i++)
            {
                var schema = await JsonSchema4.FromTypeAsync<Container>();
                var json = schema.ToJson();
            }
            stopwatch.Stop();
            
            Console.WriteLine("Time: " + stopwatch.ElapsedMilliseconds);
            Console.ReadKey();
        }
    }

    public class SpecialTeacher : Teacher
    {
        public string Foo { get; set; }
    }

    [KnownType(typeof(SpecialTeacher))]
    public class Teacher
    {
        public string Bar { get; set; }
    }

    [KnownType(typeof(Teacher))]
    public class Person
    {
        public string Baz { get; set; }
    }

    public class Pen : WritingInstrument
    {
        public string Foo { get; set; }
    }

    public class Pencil : WritingInstrument
    {
        public string Bar { get; set; }
    }

    [KnownType("GetKnownTypes")]
    public class WritingInstrument
    {
        public static Type[] GetKnownTypes()
        {
            return new[] { typeof(Pen), typeof(Pencil) };
        }

        public string Baz { get; set; }
    }

    public class Container
    {
        public Person Person { get; set; }

        public Teacher Teacher { get; set; }

        public WritingInstrument WritingInstrument { get; set; }
    }
}
