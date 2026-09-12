using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp.Tests;

namespace NJsonSchema.CodeGeneration.Tests.CSharp;

public class LiteralDefaultTests
{
    [Theory]
    [InlineData("1e0", "int", 1)]
    [InlineData("1.0", "int", 1)]
    [InlineData("9007199254740993e0", "long", 9007199254740993L)]
    [InlineData("-9223372036854775808.0", "long", long.MinValue)]
    [InlineData("18446744073709551615.0", "ulong", ulong.MaxValue)]
    public async Task FormatlessIntegerDefault_GeneratesCompilableInitializer(string value, string integerType, object expected)
    {
        // Arrange
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"count":{"type":"integer","default":VALUE}}}
            """.Replace("VALUE", value));
        var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco, IntegerType = integerType });

        // Act
        var code = generator.GenerateFile("Counter");

        // Assert
        var assembly = CSharpCompiler.AssertCompile(code, returnAssembly: true);
        var generatedType = assembly.GetType("MyNamespace.Counter");
        var instance = Activator.CreateInstance(generatedType);
        Assert.Equal(expected, generatedType.GetProperty("Count").GetValue(instance));
    }

    [Theory]
    [InlineData("integer", null, "1e999999999999999999999999999999", "1e999999999999999999999999999999")]
    [InlineData("integer", null, "1.00000000000000000000000000001", "1.00000000000000000000000000001")]
    [InlineData("integer", "uint64", "18446744073709551615", "18446744073709551615UL")]
    [InlineData("integer", "int64", "1e0", "1L")]
    [InlineData("number", "decimal", "0.1234567890123456789012345678", "0.1234567890123456789012345678M")]
    public async Task NumericDefault_PreservesValueInGeneratedLiteral(string type, string format, string value, string expected)
    {
        // Arrange
        var schema = await JsonSchema.FromJsonAsync("{\"type\":\"" + type + "\",\"format\":\"" + format + "\",\"default\":" + value + "}");
        var generator = new CSharpValueGenerator(new CSharpGeneratorSettings());

        // Act
        var literal = generator.GetNumericValue(schema.Type, schema.Default, schema.Format);

        // Assert
        Assert.Equal(expected, literal);
    }
}
