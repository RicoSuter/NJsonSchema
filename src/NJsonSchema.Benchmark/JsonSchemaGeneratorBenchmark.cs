using BenchmarkDotNet.Attributes;
using NJsonSchema.Generation;

namespace NJsonSchema.Benchmark
{
    [MemoryDiagnoser]
    public class JsonSchemaGeneratorBenchmark
    {
        [Benchmark]
        public void GenerateFile()
        {
            JsonSchemaGenerator.FromType<SchemaGenerationBenchmarks.Container>();
        }
    }
}