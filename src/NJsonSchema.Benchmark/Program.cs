using BenchmarkDotNet.Running;

namespace NJsonSchema.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<SerializationPerformance>();
        }
    }
}
