using BenchmarkDotNet.Attributes;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.Benchmark
{
    [MemoryDiagnoser]
    public class JsonSchemaGeneratorBenchmark
    {
        [Benchmark]
        public void GenerateFile()
        {
            NewtonsoftJsonSchemaGenerator.FromType<SchemaGenerationBenchmarks.Container>();
        }
    }
}