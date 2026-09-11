#nullable enable
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Serialization;

public class LenientSchemaInputRegressionTests
{
    [Theory]
    [InlineData(SchemaType.JsonSchema, "x-nullable", "x-example")]
    [InlineData(SchemaType.Swagger2, "x-nullable", "x-example")]
    [InlineData(SchemaType.OpenApi3, "nullable", "example")]
    public void Quoted_booleans_preserve_literals(SchemaType dialect, string nullable, string example)
    {
        // Arrange
        var converter = JsonSchema.CreateSchemaSerializationConverter(dialect);
        var input = new JsonObject { [nullable] = "true", [example] = "false", ["default"] = "true",
            ["description"] = "a\u00a0b", ["x-vendor"] = new JsonObject { ["readOnly"] = "true", ["text"] = "a\u00a0b" } };

        // Act
        var schema = JsonSchemaSerialization.FromJson<JsonSchema>(input.ToJsonString(), converter)!;
        var output = JsonNode.Parse(JsonSchemaSerialization.ToJson(schema, dialect, converter, false))!;

        // Assert
        Assert.True(schema.IsNullableRaw);
        Assert.Equal("true", output["default"]!.GetValue<string>());
        Assert.Equal("false", output[dialect == SchemaType.JsonSchema ? "x-example" : "example"]!.GetValue<string>());
        Assert.Equal("a\u00a0b", schema.Description);
        Assert.True(JsonNode.DeepEquals(input["x-vendor"], output["x-vendor"]));

        // Arrange
        input["default"] = JsonNode.Parse("""{"type":"object","properties":{"readOnly":"true"},"nested":["true","false",{"readOnly":"false"}]}""");
        input["description"] = "literal {key: 'value'} and : \"true\"";

        // Act
        schema = JsonSchemaSerialization.FromJson<JsonSchema>(input.ToJsonString(), converter)!;
        output = JsonNode.Parse(JsonSchemaSerialization.ToJson(schema, dialect, converter, false))!;

        // Assert
        Assert.True(JsonNode.DeepEquals(input["default"], output["default"]));
        Assert.Equal(input["description"]!.GetValue<string>(), schema.Description);
    }

    [Theory]
    [InlineData("{'default':'ok'}", "ok")]
    [InlineData("{type:'string', default:'true'}", "true")]
    [InlineData("{\u00a0type: 'string', default: 'a\u00a0b'}", "a\u00a0b")]
    [InlineData("{default:'it\\'s good'}", "it's good")]
    [InlineData("{ /* 'ignored' { */ default: 'ok', // comment\n}", "ok")]
    [InlineData("{default:'a\\nb\\u0041'}", "a\nbA")]
    public void Supported_syntax_preserves_string_values(string json, string expected)
    {
        // Arrange
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);

        // Act
        var schema = JsonSchemaSerialization.FromJson<JsonSchema>(json, converter)!;

        // Assert
        Assert.Equal(expected, ((JsonElement)schema.Default!).GetString());
    }

    [Fact]
    public void Syntax_recovery_preserves_double_quoted_tokens_and_nested_arrays()
    {
        // Arrange
        const string json = """{default:[['true'],{text:"literal {key: 'value'} and : \"true\"", path:"a\\b"},], description:'ok'}""";
        var expected = JsonNode.Parse("""[["true"],{"text":"literal {key: 'value'} and : \"true\"","path":"a\\b"}]""");

        // Act
        var schema = JsonSchemaSerialization.FromJson<JsonSchema>(json, JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema))!;

        // Assert
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(((JsonElement)schema.Default!).GetRawText())));
    }

    [Theory]
    [InlineData("{default:'unfinished}")]
    [InlineData("{default:\"unfinished}")]
    [InlineData("{default:'bad\\q'}")]
    [InlineData("{default:'ok' /* unfinished}")]
    [InlineData("{default:['ok'}")]
    public void Invalid_syntax_throws(string json)
    {
        // Arrange
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);

        // Act / Assert
        Assert.Throws<JsonException>(() => JsonSchemaSerialization.FromJson<JsonSchema>(json, converter));
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.OpenApi3)]
    public void Quoted_numeric_bounds_match_unquoted_bounds(SchemaType dialect)
    {
        // Arrange
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-CH");
        try
        {
            foreach (var name in new[] { "exclusiveMinimum", "exclusiveMaximum" })
            foreach (var number in new[] { "1.5", "1e1" })
            {
                var converter = JsonSchema.CreateSchemaSerializationConverter(dialect);
                var prefix = "\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"type\":\"number\",";
                var expected = decimal.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture);

                // Act
                var schema = JsonSchemaSerialization.FromJson<JsonSchema>("{" + prefix + "\"" + name + "\":\"" + number + "\"}", converter)!;
                var control = JsonSchemaSerialization.FromJson<JsonSchema>("{" + prefix + "\"" + name + "\":" + number + "}", converter)!;
                var output = JsonNode.Parse(JsonSchemaSerialization.ToJson(schema, dialect, converter, false))!;

                // Assert
                Assert.Equal(expected, name == "exclusiveMinimum" ? schema.ExclusiveMinimum : schema.ExclusiveMaximum);
                Assert.Equal(expected, output[name]!.GetValue<decimal>());
                foreach (var value in new[] { expected - 1, expected, expected + 1 })
                    Assert.Equal(control.Validate(value.ToString(CultureInfo.InvariantCulture)).Count, schema.Validate(value.ToString(CultureInfo.InvariantCulture)).Count);
                Assert.NotEmpty(schema.Validate(expected.ToString(CultureInfo.InvariantCulture)));
            }
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.Swagger2)]
    [InlineData(SchemaType.OpenApi3)]
    public void Quoted_boolean_bounds_match_unquoted_bounds(SchemaType dialect)
    {
        // Arrange
        var converter = JsonSchema.CreateSchemaSerializationConverter(dialect);
        foreach (var name in new[] { "exclusiveMinimum", "exclusiveMaximum" })
        foreach (var boolean in new[] { "true", "false" })
        {
            var prefix = "\"type\":\"number\",\"minimum\":1,\"maximum\":2,";

            // Act
            var schema = JsonSchemaSerialization.FromJson<JsonSchema>("{" + prefix + "\"" + name + "\":\"" + boolean + "\"}", converter)!;
            var control = JsonSchemaSerialization.FromJson<JsonSchema>("{" + prefix + "\"" + name + "\":" + boolean + "}", converter)!;
            var output = JsonNode.Parse(JsonSchemaSerialization.ToJson(schema, dialect, converter, false))!;
            var controlOutput = JsonNode.Parse(JsonSchemaSerialization.ToJson(control, dialect, converter, false))!;

            // Assert
            Assert.Equal(boolean == "true", name == "exclusiveMinimum" ? schema.IsExclusiveMinimum : schema.IsExclusiveMaximum);
            Assert.True(JsonNode.DeepEquals(controlOutput[name], output[name]));
            foreach (var value in new[] { "0", "1", "1.5", "2", "3" })
                Assert.Equal(control.Validate(value).Count, schema.Validate(value).Count);
            Assert.Equal(boolean == "true", schema.Validate(name == "exclusiveMinimum" ? "1" : "2").Count > 0);
        }
    }

    [Theory]
    [InlineData("exclusiveMinimum")]
    [InlineData("exclusiveMaximum")]
    [InlineData("x-nullable")]
    public void Invalid_recognized_strings_throw(string keyword)
    {
        // Arrange
        var json = "{\"" + keyword + "\":\"not-a-bound\"}";

        // Act / Assert
        Assert.Throws<JsonException>(() => JsonSchemaSerialization.FromJson<JsonSchema>(json, JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema)));
    }

    [Fact]
    public void Property_boolean_converter_owns_quoted_values()
    {
        // Arrange
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);

        // Act
        var schema = JsonSchemaSerialization.FromJson<ConvertedBooleanSchema>("""{"custom":"true","x-nullable":"true"}""", converter)!;

        // Assert
        Assert.False(schema.Custom);
        Assert.True(schema.IsNullableRaw);

        // Arrange
        converter.AddConverter(new InvertedBooleanConverter());

        // Act
        schema = JsonSchemaSerialization.FromJson<ConvertedBooleanSchema>("""{"custom":"true","x-nullable":"true"}""", converter)!;

        // Assert
        Assert.False(schema.Custom);
        Assert.False(schema.IsNullableRaw);
    }

    public class ConvertedBooleanSchema : JsonSchema
    {
        [JsonPropertyName("custom")]
        [JsonConverter(typeof(InvertedBooleanConverter))]
        public bool Custom { get; set; }
    }

    public class InvertedBooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => !bool.Parse(reader.GetString()!);
        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteStringValue((!value).ToString());
    }
}
