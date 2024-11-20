using BenchmarkDotNet.Attributes;
using NJsonSchema.CodeGeneration.CSharp;

namespace NJsonSchema.Benchmark;

[MemoryDiagnoser]
public class CSharpTypeResolverBenchmark
{
    private Dictionary<string, JsonSchema> _definitions;
    private CSharpGeneratorSettings _settings;

    [GlobalSetup]
    public async Task Setup()
    {
        var json = await JsonSchemaBenchmark.ReadJson();
        var schema = await JsonSchema.FromJsonAsync(json);
        _definitions = schema.Definitions.ToDictionary(p => p.Key, p => p.Value);
        _settings = new CSharpGeneratorSettings();
    }

    [Benchmark]
    public void RegisterSchemaDefinitions()
    {
        var resolver = new CSharpTypeResolver(_settings, exceptionSchema: null);
        resolver.RegisterSchemaDefinitions(_definitions);
    }
}