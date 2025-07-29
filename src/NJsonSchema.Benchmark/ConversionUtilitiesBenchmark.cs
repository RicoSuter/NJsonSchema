using BenchmarkDotNet.Attributes;

namespace NJsonSchema.Benchmark;

[MemoryDiagnoser]
public class ConversionUtilitiesBenchmark
{
    [Params("example_string", "1another_example", "ConversionUtilities", "ConvertToUpperCamelCase", "/foo/bar/baz", "")]
    public string Input { get; set; }

    [Benchmark]
    public string ConvertToUpperCamelCase()
    {
        return ConversionUtilities.ConvertToUpperCamelCase(Input, firstCharacterMustBeAlpha: true);
    }

    [Benchmark]
    public string ConvertToLowerCamelCase()
    {
        return ConversionUtilities.ConvertToLowerCamelCase(Input, firstCharacterMustBeAlpha: true);
    }
}