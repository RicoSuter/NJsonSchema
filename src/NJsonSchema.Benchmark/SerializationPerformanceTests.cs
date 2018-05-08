using System.Diagnostics;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace NJsonSchema.Benchmark
{
    public class SerializationPerformanceTests
    {
        private readonly SerializationPerformance _serializationPerformance = new SerializationPerformance();
        private Counter _counter;

        public SerializationPerformanceTests(ITestOutputHelper output)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));
        }

        [PerfSetup]
        #pragma warning disable xUnit1013 // Public method should be marked as test
        public void Setup(BenchmarkContext context)
        #pragma warning restore xUnit1013 // Public method should be marked as test
        {
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
            _serializationPerformance.ToJson();
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
            _serializationPerformance.FromJson();
            _counter.Increment();
        }
    }
}
