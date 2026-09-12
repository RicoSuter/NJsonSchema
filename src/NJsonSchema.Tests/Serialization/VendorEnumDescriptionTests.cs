#nullable enable
using System.Text.Json.Nodes;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Serialization;

public class VendorEnumDescriptionTests
{
    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.Swagger2)]
    [InlineData(SchemaType.OpenApi3)]
    public async Task Vendor_metadata_roundtrips_directly_and_through_references(SchemaType dialect)
    {
        // Arrange
        const string literal = """{"type":"not-a-schema","properties":{"a":1},"default":null,"enum":[1,"2"],"nested":{"value":7}}""";
        var payloads = new[]
        {
            """[{"value":"A","description":"Alpha","default":true}]""",
            "[" + literal + ""","text",7,null]""",
            literal
        };
        foreach (var alias in new[] { "x-enumDescriptions", "x-enum-descriptions" })
        foreach (var payload in payloads)
        foreach (var referenced in new[] { false, true })
        {
            var example = dialect == SchemaType.OpenApi3 ? "example" : "x-example";
            var target = """{"type":"string","enum":["A"],"default":LITERAL,"EXAMPLE":LITERAL,"ALIAS":PAYLOAD}"""
                .Replace("LITERAL", literal).Replace("EXAMPLE", example).Replace("ALIAS", alias).Replace("PAYLOAD", payload);
            var json = referenced
                ? """{"components":{"schemas":{"Use":{"type":"object","properties":{"value":{"$ref":"#/components/schemas/Value"}}},"Value":TARGET}}}""".Replace("TARGET", target)
                : target;

            // Act
            var root = await Load(json, dialect);
            var schema = referenced
                ? (JsonSchema)new JsonReferenceResolver(new JsonSchemaAppender(root, new DefaultTypeNameGenerator()))
                    .ResolveDocumentReference(root, "#/components/schemas/Value", typeof(JsonSchema))
                : root;
            var output = JsonNode.Parse(JsonSchemaSerialization.ToJson(schema, dialect,
                JsonSchema.CreateSchemaSerializationConverter(dialect), false))!;

            // Assert
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(payload), output[alias]));
            Assert.Null(output[alias == "x-enumDescriptions" ? "x-enum-descriptions" : "x-enumDescriptions"]);
            Assert.Empty(schema.EnumerationDescriptions);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(literal), output["default"]));
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(literal), output[dialect == SchemaType.JsonSchema ? "x-example" : "example"]));
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""["A"]"""), output["enum"]));
            if (referenced)
            {
                var use = (JsonSchema)new JsonReferenceResolver(new JsonSchemaAppender(root, new DefaultTypeNameGenerator()))
                    .ResolveDocumentReference(root, "#/components/schemas/Use", typeof(JsonSchema));
                Assert.Same(schema, use.Properties["value"].Reference);
            }
        }
    }

    [Theory]
    [InlineData("x-enumDescriptions")]
    [InlineData("x-enum-descriptions")]
    public async Task Supported_string_arrays_keep_the_collection_contract(string alias)
    {
        // Arrange
        var json = """{"type":"string","ALIAS":["Alpha",null,"Beta"]}""".Replace("ALIAS", alias);

        // Act
        var schema = await JsonSchema.FromJsonAsync(json);
        var output = JsonNode.Parse(schema.ToJson())!;

        // Assert
        Assert.Equal(new string?[] { "Alpha", null, "Beta" }, schema.EnumerationDescriptions);
        Assert.Null(output["x-enumDescriptions"]);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""["Alpha",null,"Beta"]"""), output["x-enum-descriptions"]));
    }

    [Fact]
    public async Task Both_unsupported_aliases_preserve_their_own_payloads()
    {
        // Arrange
        const string json = """{"type":"string","x-enumDescriptions":[{"value":"A"}],"x-enum-descriptions":[1,null]}""";

        // Act
        var schema = await JsonSchema.FromJsonAsync(json);
        var output = JsonNode.Parse(schema.ToJson())!;

        // Assert
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json)!["x-enumDescriptions"], output["x-enumDescriptions"]));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json)!["x-enum-descriptions"], output["x-enum-descriptions"]));
    }

    [Theory]
    [InlineData("""{"x-target":{"title":"Value","default":7}}""", "#/x-target")]
    [InlineData("""{"x-target":{"nested":{"title":"Value","default":7}}}""", "#/x-target/nested")]
    [InlineData("""{"x-target":[null,{"title":"Value","default":7}]}""", "#/x-target/1")]
    public async Task Dictionary_reference_targets_remain_in_the_serialized_graph(string json, string path)
    {
        // Arrange
        var input = JsonNode.Parse(json)!;
        input["properties"] = JsonNode.Parse("""{"value":{"$ref":"PATH"},"other":{"$ref":"PATH"}}""".Replace("PATH", path));
        var baseline = await JsonSchema.FromJsonAsync("""{"default":7}""");

        // Act
        var schema = await JsonSchema.FromJsonAsync(input.ToJsonString());
        var target = schema.Properties["value"].ActualSchema;
        var output = JsonNode.Parse(schema.ToJson())!;

        // Assert
        Assert.Equal(baseline.Default, target.Default);
        Assert.Same(target, schema.Properties["other"].Reference);
        Assert.Equal(path, JsonPathUtilities.GetJsonPath(schema, target));
        Assert.Equal(path, output["properties"]!["value"]!["$ref"]!.GetValue<string>());
        Assert.Equal(path, output["properties"]!["other"]!["$ref"]!.GetValue<string>());
    }

    [Fact]
    public async Task Materializing_dictionary_targets_preserves_already_resolved_nested_references()
    {
        // Arrange
        const string json = """
            {"definitions":{"Value":{"type":"string"}},
             "x-target":{"allOf":[{"type":"object","properties":{"nested":{"$ref":"#/definitions/Value"}}}]},
             "properties":{"use":{"$ref":"#/x-target"}}}
            """;

        // Act
        var schema = await JsonSchema.FromJsonAsync(json);
        var output = JsonNode.Parse(schema.ToJson())!;

        // Assert
        Assert.Equal("#/definitions/Value", output["x-target"]!["allOf"]![0]!["properties"]!["nested"]!["$ref"]!.GetValue<string>());
        Assert.Same(schema.Definitions["Value"], schema.Properties["use"].ActualSchema.AllOf.First().Properties["nested"].Reference);
    }

    [Fact]
    public async Task PayPal_roundtrip_preserves_every_source_reference()
    {
        // Arrange
        var json = File.ReadAllText(Path.Combine("Deserialization", "TestData", "paypal_billing_subscriptions_v1.json"));
        var input = JsonNode.Parse(json)!;

        // Act
        var schema = await JsonSchema.FromJsonAsync(json);
        var output = JsonNode.Parse(schema.ToJson())!;

        // Assert
        CompareReferences(input, output);
    }

    private static void CompareReferences(JsonNode? input, JsonNode? output)
    {
        if (input is JsonObject obj)
        {
            foreach (var pair in obj)
            {
                if (pair.Key == "$ref") Assert.True(JsonNode.DeepEquals(pair.Value, output?[pair.Key]));
                else CompareReferences(pair.Value, output?[pair.Key]);
            }
        }
        else if (input is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++) CompareReferences(array[index], output?[index]);
        }
    }

    [Fact]
    public async Task Materialized_children_keep_sharing_with_adversarial_extension_keys()
    {
        // Arrange
        var first = new JsonSchema { Type = JsonObjectType.String };
        var second = new JsonSchema { Type = JsonObjectType.Integer };
        var third = new JsonSchema { Type = JsonObjectType.Boolean };
        var source = new Dictionary<string, object?>
        {
            ["x-node/items[0]"] = first,
            ["x-node"] = new Dictionary<string, object?> { ["items"] = new object[] { second } },
            ["x-list[0]"] = third,
            ["x-list"] = new object[] { first }
        };

        // Act
        var schema = await JsonSchemaSerialization.FromJsonAsync<JsonSchema>(
            """{"properties":{"use":{"$ref":"#/x-target"}}}""", SchemaType.JsonSchema, null, root =>
            {
                root.ExtensionData = new Dictionary<string, object?> { ["x-target"] = source };
                return new JsonReferenceResolver(new JsonSchemaAppender(root, new DefaultTypeNameGenerator()));
            }, JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema));
        var target = schema.Properties["use"].ActualSchema;

        // Assert
        Assert.Same(first, target.ExtensionData!["x-node/items[0]"]);
        Assert.Same(third, target.ExtensionData["x-list[0]"]);
        Assert.Same(first, Assert.IsType<object[]>(target.ExtensionData["x-list"])[0]);
        var nested = Assert.IsType<Dictionary<string, object?>>(target.ExtensionData["x-node"]);
        Assert.Same(second, Assert.IsType<object[]>(nested["items"])[0]);
    }

    [Fact]
    public async Task Newly_materialized_target_descendants_are_resolved()
    {
        // Arrange
        const string json = """{"definitions":{"Value":{"type":"string"}},"x-target":{"allOf":[{"$ref":"#/definitions/Value"}]},"properties":{"use":{"$ref":"#/x-target"}}}""";

        // Act
        var schema = await JsonSchema.FromJsonAsync(json);

        // Assert
        Assert.Same(schema.Definitions["Value"], schema.Properties["use"].Reference!.AllOf.First().Reference);
    }

    private static Task<JsonSchema> Load(string json, SchemaType dialect) =>
        JsonSchemaSerialization.FromJsonAsync<JsonSchema>(json, dialect, null,
            root => new JsonReferenceResolver(new JsonSchemaAppender(root, new DefaultTypeNameGenerator())),
            JsonSchema.CreateSchemaSerializationConverter(dialect));
}
