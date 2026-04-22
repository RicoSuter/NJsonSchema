#nullable enable
using System.Reflection;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation;

public class LineInformationMultiByteTest
{
    [Fact]
    public async Task BuildLineInfoMap_handles_multibyte_utf8_before_error()
    {
        // Arrange
        // The first property name contains "é" (two UTF-8 bytes). If line positions were
        // byte-counted, the reported column for `bar` would be off by one; they're expected
        // to be character-counted (Newtonsoft's IJsonLineInfo convention).
        var schema = await JsonSchema.FromJsonAsync(@"{
            ""type"": ""object"",
            ""properties"": {
                ""é"": { ""type"": ""integer"" },
                ""bar"": { ""type"": ""integer"" }
            }
        }");

        var json = "{ \"é\": 1, \"bar\": \"should-be-int\" }";

        // Act
        var errors = schema.Validate(json);

        // Assert
        var error = Assert.Single(errors);
        Assert.True(error.HasLineInfo);
        Assert.Equal(1, error.LineNumber);

        // Byte count to 'should-be-int' end is 32 (due to 2-byte 'é'); character count is 31.
        // Whatever the exact value, it must be the character count, not the byte count.
        var charEnd = json.LastIndexOf('"') + 1;
        var byteEnd = System.Text.Encoding.UTF8.GetByteCount(json.Substring(0, charEnd));
        Assert.NotEqual(byteEnd, charEnd);
        Assert.Equal(charEnd, error.LinePosition);
    }

    [Fact]
    public async Task Validate_root_array_produces_line_info()
    {
        // Arrange
        var schema = await JsonSchema.FromJsonAsync(@"{
            ""type"": ""array"",
            ""items"": { ""type"": ""integer"" }
        }");

        var json = "[1, \"x\"]";

        // Act
        var errors = schema.Validate(json);

        // Assert
        var error = Assert.Single(errors);
        Assert.True(error.HasLineInfo);
        Assert.Equal(1, error.LineNumber);
        Assert.Equal(7, error.LinePosition); // past the closing '"' of "x"
    }

    [Theory]
    [InlineData("$", "#")]
    [InlineData("$.prop", "#/prop")]
    [InlineData("$.prop1.prop2", "#/prop1.prop2")]
    [InlineData("$.prop[0]", "#/prop[0]")]
    [InlineData("$[0]", "#/[0]")]
    [InlineData("$[0][1]", "#/[0][1]")]
    [InlineData("$['foo.bar']", "#/foo.bar")]
    [InlineData("$.outer['foo.bar']", "#/outer.foo.bar")]
    public void ConvertJsonNodePathToValidationPath_maps_known_shapes(string input, string expected)
    {
        // Arrange
        var method = typeof(JsonSchemaValidator).GetMethod(
            "ConvertJsonNodePathToValidationPath",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ConvertJsonNodePathToValidationPath not found");

        // Act
        var result = (string)method.Invoke(null, new object[] { input })!;

        // Assert
        Assert.Equal(expected, result);
    }
}
#nullable restore
