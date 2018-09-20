using BenchmarkDotNet.Attributes;
using NJsonSchema.Tests.Generation;
using System.Threading.Tasks;
using NJsonSchema.Infrastructure;

using static NJsonSchema.Tests.Generation.XmlDocTests;

namespace NJsonSchema.Benchmark
{
    public class GeneratorPerformance
    {
        private readonly XmlDocTests _tests;

        public GeneratorPerformance()
        {
            _tests = new XmlDocTests();
        }

        [Benchmark]
        public async Task XmlDocTests()
        {
            await typeof(ClassWithInheritdoc).GetMethod("Bar").GetXmlSummaryAsync();
        }
    }
}