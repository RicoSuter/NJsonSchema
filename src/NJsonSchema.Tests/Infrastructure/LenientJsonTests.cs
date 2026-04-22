#nullable enable
using System.Reflection;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Infrastructure;

public class LenientJsonTests
{
    private static readonly MethodInfo FixLenientJsonMethod =
        typeof(JsonSchemaSerialization).GetMethod(
            "FixLenientJson",
            BindingFlags.Static | BindingFlags.NonPublic)!;

    private static string Fix(string json) => (string)FixLenientJsonMethod.Invoke(null, new object[] { json })!;

    [Fact]
    public void Replaces_NBSP_with_regular_space_between_tokens()
    {
        // Arrange
        // U+00A0 between property and colon — STJ rejects it; the fallback normalizes.
        var input = "{ \"foo\" : 1 }";

        // Act
        var result = Fix(input);

        // Assert
        Assert.DoesNotContain(' ', result);
    }

    [Fact]
    public void Converts_stringified_true_to_bool_true()
    {
        // Arrange
        var input = "{ \"readOnly\": \"true\" }";

        // Act
        var result = Fix(input);

        // Assert
        Assert.Contains(": true", result);
        Assert.DoesNotContain("\"true\"", result);
    }

    [Fact]
    public void Converts_stringified_false_to_bool_false()
    {
        // Arrange
        var input = "{ \"readOnly\": \"false\" }";

        // Act
        var result = Fix(input);

        // Assert
        Assert.Contains(": false", result);
        Assert.DoesNotContain("\"false\"", result);
    }

    [Fact]
    public void Does_not_convert_stringified_bool_inside_string_value()
    {
        // Arrange
        // The token between colon and the next structural character is a string literal
        // whose content is "example: true" — the inner `true` must stay a string.
        var input = "{ \"note\": \"example: true\" }";

        // Act
        var result = Fix(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Replaces_single_quoted_string_value()
    {
        // Arrange
        var input = "{ \"foo\": 'bar' }";

        // Act
        var result = Fix(input);

        // Assert
        Assert.Contains("\"bar\"", result);
        Assert.DoesNotContain("'bar'", result);
    }

    [Fact]
    public void Does_not_replace_apostrophes_inside_double_quoted_strings()
    {
        // Arrange
        var input = "{ \"message\": \"it's fine\" }";

        // Act
        var result = Fix(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Quotes_unquoted_property_names()
    {
        // Arrange
        var input = "{ foo: 1, bar: 2 }";

        // Act
        var result = Fix(input);

        // Assert
        Assert.Contains("\"foo\":", result);
        Assert.Contains("\"bar\":", result);
    }

    [Fact]
    public void Does_not_mangle_colons_inside_string_values()
    {
        // Arrange
        // A colon inside a string value must not be interpreted as an unquoted-key separator.
        var input = "{ \"url\": \"http://example.com\" }";

        // Act
        var result = Fix(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Is_idempotent()
    {
        // Arrange
        var input = "{ 'foo': 'bar', baz: \"qux\" }";

        // Act
        var once = Fix(input);
        var twice = Fix(once);

        // Assert
        Assert.Equal(once, twice);
    }

    [Fact]
    public void Handles_combined_dirty_json()
    {
        // Arrange
        // Single quotes + unquoted keys + stringified boolean + NBSP together.
        var input = "{ foo: 'bar', readOnly: \"true\", baz: \"qux\" }";

        // Act
        var result = Fix(input);
        // Confirm the result is valid JSON by parsing it.
        using var doc = System.Text.Json.JsonDocument.Parse(result);
        var root = doc.RootElement;

        // Assert
        Assert.Equal("bar", root.GetProperty("foo").GetString());
        Assert.True(root.GetProperty("readOnly").GetBoolean());
        Assert.Equal("qux", root.GetProperty("baz").GetString());
    }
}
#nullable restore
