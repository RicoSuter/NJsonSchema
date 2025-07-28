using System.Runtime.Serialization;
using NBench;
using NJsonSchema.NewtonsoftJson.Generation;
using Pro.NBench.xUnit.XunitExtensions;
using Counter = NBench.Counter;

namespace NJsonSchema.Benchmark
{
    public class SchemaGenerationBenchmarks
    {
        private Counter _counter;

        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            _counter = context.GetCounter("Iterations");
        }

        [NBenchFact(Skip = "Can vary a lot when running on GitHub Actions")]
        [PerfBenchmark(
            NumberOfIterations = 3,
            RunTimeMilliseconds = 1000,
            RunMode = RunMode.Throughput,
            TestMode = TestMode.Test)]
        [CounterThroughputAssertion("Iterations", MustBe.GreaterThan, 100)]
        public void GenerateSchema()
        {
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Container>();
            _counter.Increment();
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
