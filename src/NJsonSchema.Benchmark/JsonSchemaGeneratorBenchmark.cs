using BenchmarkDotNet.Attributes;

namespace NJsonSchema.Benchmark
{
    [MemoryDiagnoser]
    public class JsonSchemaGeneratorBenchmark
    {
        [Benchmark]
        public void GenerateFile()
        {
            JsonSchema.FromType<SchemaGenerationBenchmarks.Container>();
        }
    }
}