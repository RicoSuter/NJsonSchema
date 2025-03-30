﻿using NJsonSchema.NewtonsoftJson.Generation;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace NJsonSchema.Demo.Performance
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Run().GetAwaiter().GetResult();
        }

        private static Task Run()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < 500; i++)
            {
                var schema = NewtonsoftJsonSchemaGenerator.FromType<Container>();
                var json = schema.ToJson();
            }
            stopwatch.Stop();
            
            Console.WriteLine("Time: " + stopwatch.ElapsedMilliseconds);
            Console.ReadKey();
            return Task.CompletedTask;
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
            return [typeof(Pen), typeof(Pencil)];
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
