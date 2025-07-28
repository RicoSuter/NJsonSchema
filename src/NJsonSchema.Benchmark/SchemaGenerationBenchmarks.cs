using System.Runtime.Serialization;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.Benchmark
{
    public class SchemaGenerationBenchmarks
    {
        public void GenerateSchema()
        {
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Container>();
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
}
