namespace NJsonSchema.Benchmark
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // RunCsharpBenchmark();
            BenchmarkDotNet.Running.BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAllJoined();
        }

        private static void RunCsharpBenchmark()
        {
            var benchmark = new CsharpGeneratorBenchmark();
            benchmark.Setup().GetAwaiter().GetResult();
            benchmark.GenerateFile();
        }
    }
}