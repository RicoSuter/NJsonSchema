using BenchmarkDotNet.Attributes;

namespace NJsonSchema.Benchmark
{
    [MemoryDiagnoser]
    public class JsonSchemaBenchmark
    {
        private string _json;
        
        [GlobalSetup]
        public async Task Setup()
        {
            _json = await ReadJson();
        }

        internal static Task<string> ReadJson()
        {
            var assembly = typeof(JsonSchemaBenchmark).Assembly;
            var file = assembly.GetManifestResourceNames().First(x => x.Contains("LargeSchema.json"));
            return new StreamReader(assembly.GetManifestResourceStream(file)).ReadToEndAsync();
        }

        [Benchmark]
        public Task<JsonSchema> FromJsonAsync()
        {
            return JsonSchema.FromJsonAsync(_json);
        }
    }
}