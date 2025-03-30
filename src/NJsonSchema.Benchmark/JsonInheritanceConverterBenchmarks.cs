using NBench;
using NJsonSchema.Tests.Generation;
using Pro.NBench.xUnit.XunitExtensions;

namespace NJsonSchema.Benchmark
{
    public class JsonInheritanceConverterBenchmarks
    {
        private Counter _counter;
        private JsonInheritanceConverterTests _tests;

        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            _counter = context.GetCounter("Iterations");
            _tests = new JsonInheritanceConverterTests();
        }

        [NBenchFact]
        [PerfBenchmark(
            NumberOfIterations = 3,
            RunTimeMilliseconds = 1000,
            RunMode = RunMode.Throughput,
            TestMode = TestMode.Test)]
        [CounterThroughputAssertion("Iterations", MustBe.GreaterThan, 100)]
        public void Serialize()
        {
            _tests.When_serializing_discriminator_property_is_set();
            _tests.When_serializing_discriminator_property_is_overwritten_if_already_present();
            _counter.Increment();
        }

        [NBenchFact]
        [PerfBenchmark(
            NumberOfIterations = 3,
            RunTimeMilliseconds = 1000,
            RunMode = RunMode.Throughput,
            TestMode = TestMode.Test)]
        [CounterThroughputAssertion("Iterations", MustBe.GreaterThan, 100)]
        public void Deserialize()
        {
            _tests.When_deserializing_type_is_resolved_using_discriminator_value();
            _tests.When_deserializing_existing_property_is_populated_with_discriminator_value();
            _counter.Increment();
        }
    }
}
