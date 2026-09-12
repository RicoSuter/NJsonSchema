#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Serialization;

public class SchemaSerializationConverterTests
{
    [Fact]
    public void Read_does_not_rename_deprecated_on_non_schema_type()
    {
        // Arrange
        // A non-JsonSchema class that has a "deprecated" JSON property — simulates NSwag's
        // OpenApiOperation.IsDeprecated. The OpenApi3 converter renames JsonSchema's
        // "deprecated" → "x-deprecated", but this must NOT affect unrelated types.
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.OpenApi3);

        var options = new JsonSerializerOptions();
        options.Converters.Add(converter);

        var json = "{ \"deprecated\": true, \"name\": \"test\" }";

        // Act
        var result = JsonSerializer.Deserialize<NonSchemaTypeWithDeprecated>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.IsDeprecated);
        Assert.Equal("test", result.Name);
    }

    [Fact]
    public async Task Read_applies_rename_to_nested_JsonSchema()
    {
        // Arrange
        // OpenApi3 renames "nullable" → "x-nullable" when deserializing. Verify the rename
        // is applied at every nesting level, not only at the root.
        var rootSchema = new JsonSchema { Type = JsonObjectType.Object };
        var nestedAllOf = new JsonSchema
        {
            Type = JsonObjectType.Object,
            ExtensionData = new Dictionary<string, object?> { ["x-nullable"] = true }
        };
        rootSchema.AllOf.Add(nestedAllOf);

        var nestedProperty = new JsonSchemaProperty
        {
            Type = JsonObjectType.String,
            ExtensionData = new Dictionary<string, object?> { ["x-nullable"] = true }
        };
        rootSchema.Properties["nestedProp"] = nestedProperty;

        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.OpenApi3);
        var json = JsonSchemaSerialization.ToJson(rootSchema, SchemaType.OpenApi3, converter, writeIndented: false);

        // Act
        var deserialized = await JsonSchemaSerialization.FromJsonAsync<JsonSchema>(
            json, SchemaType.OpenApi3, documentPath: null,
            referenceResolverFactory: schema => new NJsonSchema.JsonReferenceResolver(new JsonSchemaAppender(schema, new DefaultTypeNameGenerator())),
            converter);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.AllOf.First().IsNullableRaw);
        Assert.True(deserialized.Properties["nestedProp"].IsNullableRaw);
    }

    [Fact]
    public async Task Read_preserves_extension_data_keys_named_nullable_or_deprecated()
    {
        // Arrange
        // Vendor extensions (x-*) carry arbitrary dictionaries whose keys happen to match
        // schema-level rename targets ("nullable", "deprecated"). Those nested keys belong
        // to a non-schema object and must survive deserialization unchanged.
        var json = """
        {
          "type": "object",
          "x-vendor-foo": {
            "nullable": true,
            "deprecated": false,
            "notes": "these keys belong to an x- extension, not the schema"
          }
        }
        """;

        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.OpenApi3);

        // Act
        var schema = await JsonSchemaSerialization.FromJsonAsync<JsonSchema>(
            json, SchemaType.OpenApi3, documentPath: null,
            referenceResolverFactory: s => new NJsonSchema.JsonReferenceResolver(new JsonSchemaAppender(s, new DefaultTypeNameGenerator())),
            converter);

        // Assert
        Assert.NotNull(schema);
        Assert.NotNull(schema.ExtensionData);
        Assert.True(schema.ExtensionData!.TryGetValue("x-vendor-foo", out var vendorValue));
        var vendorDict = Assert.IsAssignableFrom<IDictionary<string, object?>>(vendorValue);
        Assert.True(vendorDict.ContainsKey("nullable"));
        Assert.True(vendorDict.ContainsKey("deprecated"));
    }

    private sealed class NonSchemaTypeWithDeprecated
    {
        [JsonPropertyName("deprecated")]
        public bool IsDeprecated { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
#nullable restore
