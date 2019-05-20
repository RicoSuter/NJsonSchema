using System.IO;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace NJsonSchema.Benchmark
{
    public class SerializationPerformance
    {
        private readonly string _json;
        private readonly JsonSchema _schema;

        public SerializationPerformance()
        {
            // We need to embed the resource as BenchmarkDotNet
            // will rebuild this project.
            var executingAssembly = Assembly.GetExecutingAssembly();

            using (var reader = new StreamReader(
                executingAssembly.GetManifestResourceStream(
                    executingAssembly.GetName().Name + ".Schema.json")))
            {
                _json = reader.ReadToEnd();
            }

            _schema = JsonSchema.FromJsonAsync(_json).Result;
        }

        [Benchmark]
        public string ToJson()
        {
            return _schema.ToJson();
        }

        [Benchmark]
        public JsonSchema FromJson()
        {
            return JsonSchema.FromJsonAsync(_json).Result;
        }
    }
}
