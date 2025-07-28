using System.Reflection;

namespace NJsonSchema.Benchmark
{
    public class SerializationBenchmarks
    {
        private JsonSchema _schema;
        private string _json;

        public void Setup()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();

            using (var reader = new StreamReader(
                executingAssembly.GetManifestResourceStream(
                    executingAssembly.GetName().Name + ".Schema.json")))
            {
                _json = reader.ReadToEnd();
            }

            _schema = JsonSchema.FromJsonAsync(_json).Result;
        }

        /// <summary>
        /// Ensure that we can serialise at least 200 times per second (5ms).
        /// </summary>
        public void ToJson()
        {
            _schema.ToJson();
        }

        /// <summary>
        /// Ensure that we can deserialise at least 200 times per second (5ms).
        /// </summary>
        public void FromJson()
        {
            JsonSchema.FromJsonAsync(_json).GetAwaiter().GetResult();
        }
    }
}
