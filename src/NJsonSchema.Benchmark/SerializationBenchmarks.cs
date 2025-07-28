using System.Reflection;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Counter = NBench.Counter;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace NJsonSchema.Benchmark
{
    public class SerializationBenchmarks
    {
        private JsonSchema _schema;
        private Counter _counter;
        private string _json;

        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();

            using (var reader = new StreamReader(
                executingAssembly.GetManifestResourceStream(
                    executingAssembly.GetName().Name + ".Schema.json")))
            {
                _json = reader.ReadToEnd();
            }

            _schema = JsonSchema.FromJsonAsync(_json).Result;
            _counter = context.GetCounter("Iterations");
        }

        /// <summary>
        /// Ensure that we can serialise at least 200 times per second (5ms).
        /// </summary>
        [NBenchFact]
        [PerfBenchmark(
            Description = "Ensure serialization doesn't take too long",
            NumberOfIterations = 3,
            RunTimeMilliseconds = 1000,
            RunMode = RunMode.Throughput,
            TestMode = TestMode.Test)]
        [CounterThroughputAssertion("Iterations", MustBe.GreaterThan, 200)]
        public void ToJson()
        {
            _schema.ToJson();
            _counter.Increment();
        }

        /// <summary>
        /// Ensure that we can deserialise at least 200 times per second (5ms).
        /// </summary>
        [NBenchFact]
        [PerfBenchmark(
            Description = "Ensure deserialization doesn't take too long",
            NumberOfIterations = 3,
            RunTimeMilliseconds = 1000,
            RunMode = RunMode.Throughput,
            TestMode = TestMode.Test)]
        [CounterThroughputAssertion("Iterations", MustBe.GreaterThan, 200)]
        public void FromJson()
        {
            JsonSchema.FromJsonAsync(_json).GetAwaiter().GetResult();
            _counter.Increment();
        }
    }
}
