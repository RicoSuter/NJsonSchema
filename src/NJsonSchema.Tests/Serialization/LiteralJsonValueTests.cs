using System.Text.Json;
using System.Text.Json.Nodes;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Serialization;

public class LiteralJsonValueTests
{
    public static IEnumerable<object[]> LiteralCases()
    {
        foreach (var schemaType in new[] { SchemaType.JsonSchema, SchemaType.Swagger2, SchemaType.OpenApi3 })
        {
            var keywords = schemaType == SchemaType.JsonSchema ? new[] { "default", "enum" } : new[] { "default", "enum", "example" };
            foreach (var keyword in keywords)
            {
                foreach (var value in new[]
                {
                    "{\"type\":\"foo\",\"name\":\"test\"}",
                    "{\"type\":\"string\"}",
                    "{\"properties\":{\"value\":{\"type\":\"foo\"}}}",
                    "[{\"nested\":{\"type\":\"foo\"}},{\"type\":\"string\"}]",
                    "[9007199254740992,9007199254740993,0.1234567890123456789012345678]"
                })
                {
                    yield return new object[] { schemaType, keyword, value };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(LiteralCases))]
    public async Task RoundTrip_PreservesLiteralValues(SchemaType schemaType, string keyword, string value)
    {
        // Arrange
        var json = "{\"" + keyword + "\":" + (keyword == "enum" ? "[" + value + "]" : value) + "}";
        var converter = JsonSchema.CreateSchemaSerializationConverter(schemaType);

        // Act
        var schema = await JsonSchemaSerialization.FromJsonAsync<JsonSchema>(json, schemaType, null,
            JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator()), converter);
        using var output = JsonDocument.Parse(JsonSchemaSerialization.ToJson(schema, schemaType, converter, true));
        var actual = output.RootElement.GetProperty(keyword);
        if (keyword == "enum")
        {
            actual = actual[0];
        }

        // Assert
        if (value.StartsWith("[900"))
        {
            Assert.Equal(9007199254740992L, actual[0].GetInt64());
            Assert.Equal(9007199254740993L, actual[1].GetInt64());
            Assert.Equal(0.1234567890123456789012345678m, actual[2].GetDecimal());
        }
        else
        {
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(value), JsonNode.Parse(actual.GetRawText())));
        }
    }

    [Fact]
    public async Task PropertyReference_ResolvesExtensionSchema()
    {
        // Arrange
        const string json = """{"type":"object","properties":{"name":{"$ref":"#/x-schema"}},"x-schema":{"type":"string"}}""";

        // Act
        var schema = await JsonSchema.FromJsonAsync(json);

        // Assert
        var extension = Assert.IsType<JsonSchema>(schema.ExtensionData["x-schema"]);
        Assert.Same(extension, schema.Properties["name"].ActualSchema);
    }
}
