using System.Diagnostics;
using System.Threading.Tasks;
using NBench;
using NJsonSchema.Infrastructure;
using Pro.NBench.xUnit.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace NJsonSchema.Benchmark
{
    public class GeneratorPerformanceTests
    {
        private readonly GeneratorPerformance _generatorPerformance = new GeneratorPerformance();
        private Counter _counter;

        public GeneratorPerformanceTests(ITestOutputHelper output)
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
            Description = "Xml Docs (with cache)",
            NumberOfIterations = 3,
            RunTimeMilliseconds = 1000,
            RunMode = RunMode.Throughput,
            TestMode = TestMode.Test)]
        [CounterThroughputAssertion("Iterations", MustBe.GreaterThan, 200)]
        public void XmlDocTestsWithCache()
        {
            _generatorPerformance.XmlDocTests().GetAwaiter().GetResult();
            _counter.Increment();
        }

        /// <summary>
        /// Ensure that we can serialise at least 200 times per second (5ms).
        /// </summary>
        [NBenchFact]
        [PerfBenchmark(
            Description = "Xml Docs (without cache)",
            NumberOfIterations = 3,
            RunTimeMilliseconds = 1000,
            RunMode = RunMode.Throughput,
            TestMode = TestMode.Test)]
        [CounterThroughputAssertion("Iterations", MustBe.GreaterThan, 200)]
        public void XmlDocTestsWithoutCache()
        {
            XmlDocumentationExtensions.ClearCacheAsync().GetAwaiter().GetResult();
            _generatorPerformance.XmlDocTests().GetAwaiter().GetResult();
            _counter.Increment();
        }
    }
}
