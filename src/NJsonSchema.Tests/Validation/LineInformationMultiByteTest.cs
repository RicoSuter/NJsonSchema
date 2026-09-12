#nullable enable
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
    [InlineData("\n", "null")]
    [InlineData("\r\n", "null")]
    [InlineData("\r", "null")]
    [InlineData("\n", "\"é😀\"")]
    public async Task Colliding_property_paths_keep_actual_source_locations(string newline, string value)
    {
        // Arrange
        var schema = await JsonSchema.FromJsonAsync("""
            {"properties":{"a.b":{"type":["integer","boolean"]},"a":{"properties":{"b":{"anyOf":[{"type":"integer"},{"type":"boolean"}]}}}}}
            """);
        var lines = new[] { "{", "  \"a.b\": " + value + ",", "  \"a\": {", "    \"b\": " + value, "  }", "}" };

        // Act
        var errors = schema.Validate(string.Join(newline, lines)).ToArray();

        // Assert
        Assert.Equal(2, errors.Length);
        Assert.All(errors, error => Assert.Equal("#/a.b", error.Path));
        AssertLocationTree(errors[0], 2, lines[1].Length - 1);
        AssertLocationTree(errors[1], 4, lines[3].Length);
    }

    [Fact]
    public void Root_null_has_token_end_location()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Integer };

        // Act
        var error = Assert.Single(schema.Validate("null"));

        // Assert
        Assert.Equal("#/", error.Path);
        AssertLocationTree(error, 1, 4);
    }

    [Theory]
    [InlineData("a.b")]
    [InlineData("[0]")]
    [InlineData("")]
    [InlineData("a\"b")]
    [InlineData("a\\b")]
    [InlineData("/~")]
    [InlineData("é😀")]
    public void Forbidden_properties_keep_name_coordinates(string name)
    {
        // Arrange
        var schema = new JsonSchema { AllowAdditionalProperties = false };
        var property = System.Text.Json.JsonSerializer.Serialize(name);
        var json = "{\n  " + property + ": null\n}";

        // Act
        var error = Assert.Single(schema.Validate(json));

        // Assert
        Assert.Equal("#/" + name, error.Path);
        AssertLocationTree(error, 2, 2 + property.Length + 1);
    }

    [Fact]
    public async Task Array_null_and_bracket_property_keep_distinct_locations()
    {
        // Arrange
        var schema = await JsonSchema.FromJsonAsync("""
            {"properties":{"a[0]":{"type":"integer"},"a":{"type":"array","items":{"type":"integer"}}}}
            """);

        // Act
        var errors = schema.Validate("{\n\"a[0]\": null,\n\"a\": [null]\n}").ToArray();

        // Assert
        Assert.Equal(2, errors.Length);
        Assert.All(errors, error => Assert.Equal("#/a[0]", error.Path));
        AssertLocationTree(errors[0], 2, 12);
        AssertLocationTree(errors[1], 3, 10);
    }

    [Theory]
    [InlineData("a.b")]
    [InlineData("[0]")]
    [InlineData("")]
    [InlineData("a\"b")]
    [InlineData("a\\b")]
    [InlineData("/~")]
    [InlineData("é😀")]
    public void Special_property_names_keep_value_coordinates(string name)
    {
        // Arrange
        var schema = new JsonSchema();
        schema.Properties[name] = new JsonSchemaProperty { Type = JsonObjectType.Integer };
        var property = System.Text.Json.JsonSerializer.Serialize(name);
        var json = "{\n  " + property + ": \"é😀\"\n}";

        // Act
        var error = Assert.Single(schema.Validate(json));

        // Assert
        Assert.Equal("#/" + name, error.Path);
        AssertLocationTree(error, 2, 2 + property.Length + 2 + 5);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"wrong\"")]
    public async Task Case_insensitive_matching_uses_actual_input_identity(string value)
    {
        // Arrange
        var schema = await JsonSchema.FromJsonAsync("""
            {"properties":{"a.b":{"type":"integer"},"a":{"properties":{"b":{"type":"integer"}}}}}
            """);
        var validator = new JsonSchemaValidator(new JsonSchemaValidatorSettings { PropertyStringComparer = StringComparer.OrdinalIgnoreCase });
        var lines = new[] { "{", "\"A.B\": " + value + ",", "\"A\": {\"B\": " + value + "}", "}" };

        // Act
        var errors = validator.Validate(string.Join("\n", lines), schema).ToArray();

        // Assert
        Assert.Equal(2, errors.Length);
        Assert.All(errors, error => Assert.Equal("#/a.b", error.Path));
        AssertLocationTree(errors[0], 2, lines[1].Length - 1);
        AssertLocationTree(errors[1], 3, lines[2].Length - 1);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{\"x\": 1}")]
    public async Task Forbidden_colliding_properties_keep_original_owner(string value)
    {
        // Arrange
        var schema = await JsonSchema.FromJsonAsync("""
            {"additionalProperties":false,"properties":{"a":{"additionalProperties":false}}}
            """);
        var json = "{\n\"a.b\": " + value + ",\n\"a\": {\n\"b\": " + value + "\n}\n}";

        // Act
        var errors = schema.Validate(json).OrderBy(error => error.LineNumber).ToArray();

        // Assert
        Assert.Equal(2, errors.Length);
        Assert.All(errors, error => Assert.Equal("#/a.b", error.Path));
        AssertLocationTree(errors[0], 2, 6);
        AssertLocationTree(errors[1], 4, 4);
    }

    [Theory]
    [InlineData("{\"patternProperties\":{\".*\":{\"type\":\"integer\"}}}")]
    [InlineData("{\"additionalProperties\":{\"type\":\"integer\"}}")]
    public async Task Null_additional_and_pattern_properties_keep_child_locations(string schemaJson)
    {
        // Arrange
        var schema = await JsonSchema.FromJsonAsync(schemaJson);

        // Act
        var errors = schema.Validate("{\n\"a.b\": null,\n\"\": null\n}").ToArray();

        // Assert
        Assert.Equal(2, errors.Length);
        AssertLocationTree(errors[0], 2, 11);
        AssertLocationTree(errors[1], 3, 8);
    }

    [Fact]
    public async Task Null_tuple_and_additional_items_keep_child_locations()
    {
        // Arrange
        var schema = await JsonSchema.FromJsonAsync("""
            {"items":[{"type":"integer"}],"additionalItems":{"type":"boolean"}}
            """);

        // Act
        var errors = schema.Validate("[\nnull,\nnull\n]").ToArray();

        // Assert
        Assert.Equal(2, errors.Length);
        AssertLocationTree(errors[0], 2, 4);
        AssertLocationTree(errors[1], 3, 4);
    }

    private static void AssertLocationTree(ValidationError error, int line, int position)
    {
        Assert.True(error.HasLineInfo);
        Assert.Equal(line, error.LineNumber);
        Assert.Equal(position, error.LinePosition);
        if (error is ChildSchemaValidationError child)
        {
            foreach (var nested in child.Errors.Values.SelectMany(errors => errors))
                AssertLocationTree(nested, line, position);
        }
        if (error is MultiTypeValidationError multi)
        {
            foreach (var nested in multi.Errors.Values.SelectMany(errors => errors))
                AssertLocationTree(nested, line, position);
        }
    }
}
#nullable restore
