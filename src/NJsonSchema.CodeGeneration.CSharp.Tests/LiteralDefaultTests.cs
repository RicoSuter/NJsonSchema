using NJsonSchema.CodeGeneration.CSharp;

namespace NJsonSchema.CodeGeneration.Tests.CSharp;

public class LiteralDefaultTests
{
    [Theory]
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
