#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.References;

public class ReferencePathContractTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Reference_pointer_uses_inherited_and_overridden_serialized_names(bool derived, bool overridden)
    {
        // Arrange
        var target = new JsonSchema { Type = JsonObjectType.String };
        RenamedRoot root = derived ? new DerivedRoot() : new RenamedRoot();
        root.Use = new JsonSchema { Reference = target };
        root.Definitions["X"] = target;
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);
        converter.RenameProperty(typeof(RenamedRoot), "defs", "definitions");
        if (overridden) converter.RenameProperty(typeof(DerivedRoot), "defs", "models");
        var name = overridden ? "models" : "definitions";

        // Act
        using var parsed = JsonDocument.Parse(JsonSchemaSerialization.ToJson(root, SchemaType.JsonSchema, converter, false));

        // Assert
        var pointer = parsed.RootElement.GetProperty("use").GetProperty("$ref").GetString();
        Assert.Equal("#/" + name + "/X", pointer);
        var referenced = parsed.RootElement;
        foreach (var segment in pointer!.Substring(2).Split('/')) referenced = referenced.GetProperty(segment);
        Assert.Equal("string", referenced.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.Swagger2)]
    [InlineData(SchemaType.OpenApi3)]
    public void Ignored_dangling_reference_is_omitted_before_collection(SchemaType dialect)
    {
        // Arrange
        var root = new IgnoredRoot();
        var converter = JsonSchema.CreateSchemaSerializationConverter(dialect);
        converter.IgnoreProperty(typeof(IgnoredRoot), "callbacks");
        converter.RenameProperty(typeof(IgnoredRoot), "callbacks", "renamed");

        // Act
        using var parsed = JsonDocument.Parse(JsonSchemaSerialization.ToJson(root, dialect, converter, false));

        // Assert
        Assert.False(parsed.RootElement.TryGetProperty("callbacks", out _));
        Assert.False(parsed.RootElement.TryGetProperty("renamed", out _));
        Assert.Throws<InvalidOperationException>(() => JsonSchemaSerialization.ToJson(root, dialect,
            JsonSchema.CreateSchemaSerializationConverter(dialect), false));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Ignored_getter_is_never_read(bool dictionary)
    {
        // Arrange
        object root = dictionary ? new ExtraDictionary() : new ThrowingRoot();
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);
        converter.IgnoreProperty(root.GetType(), "callbacks");

        // Act
        using var parsed = JsonDocument.Parse(JsonSchemaSerialization.ToJson(root, SchemaType.JsonSchema, converter, false));

        // Assert
        Assert.False(parsed.RootElement.TryGetProperty("callbacks", out _));
    }

    [Fact]
    public void Ignored_derived_schema_member_is_not_collected()
    {
        // Arrange
        var root = new DerivedSchema();
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);
        converter.IgnoreProperty(typeof(DerivedSchema), "callbacks");

        // Act
        using var parsed = JsonDocument.Parse(JsonSchemaSerialization.ToJson(root, SchemaType.JsonSchema, converter, false));

        // Assert
        Assert.False(parsed.RootElement.TryGetProperty("callbacks", out _));
    }

    [Theory]
    [InlineData("properties")]
    [InlineData("definitions")]
    [InlineData("allOf")]
    [InlineData("items")]
    public void Ignored_schema_fast_path_does_not_collect_dangling_references(string keyword)
    {
        // Arrange
        var root = new JsonSchema();
        if (keyword == "properties") root.Properties["use"] = new JsonSchemaProperty { Reference = new JsonSchema() };
        if (keyword == "definitions") root.Definitions["use"] = new JsonSchema { Reference = new JsonSchema() };
        if (keyword == "allOf") root.AllOf.Add(new JsonSchema { Reference = new JsonSchema() });
        if (keyword == "items") root.Items.Add(new JsonSchema { Reference = new JsonSchema() });
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);
        converter.IgnoreProperty(typeof(JsonSchema), keyword);

        // Act
        using var parsed = JsonDocument.Parse(JsonSchemaSerialization.ToJson(root, SchemaType.JsonSchema, converter, false));

        // Assert
        Assert.False(parsed.RootElement.TryGetProperty(keyword, out _));
    }

    [Fact]
    public void Ignored_generic_callback_container_does_not_collect_children()
    {
        // Arrange
        var callbacks = new ReferenceGraphTraversalTests.GenericOnlyDictionary<JsonSchema>();
        ((IDictionary<string, JsonSchema>)callbacks)["onEvent"] = new JsonSchema { Reference = new JsonSchema() };
        var root = new CallbackRoot { Callbacks = callbacks };
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);
        converter.IgnoreProperty(typeof(CallbackRoot), "callbacks");

        // Act
        using var parsed = JsonDocument.Parse(JsonSchemaSerialization.ToJson(root, SchemaType.JsonSchema, converter, false));

        // Assert
        Assert.False(parsed.RootElement.TryGetProperty("callbacks", out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Async_processing_skips_ignored_getters(bool dictionary)
    {
        // Arrange
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);
        converter.IgnoreProperty(typeof(ThrowingRoot), "callbacks");
        converter.IgnoreProperty(typeof(ExtraDictionary), "callbacks");

        // Act
        var loaded = await JsonSchemaSerialization.FromJsonAsync<AsyncRoot>("{}", SchemaType.JsonSchema, null, root =>
        {
            root.Child = dictionary ? new ExtraDictionary() : new ThrowingRoot();
            return new JsonReferenceResolver(new JsonSchemaAppender(root, new DefaultTypeNameGenerator()));
        }, converter);

        // Assert
        Assert.NotNull(loaded.Child);
    }

    [Fact]
    public void Custom_dictionary_additional_property_path_matches_its_json_name()
    {
        // Arrange
        var root = new TargetDictionary();
        var use = new JsonSchema { Reference = root.Target };
        ((IDictionary<string, JsonSchema>)root)["use"] = use;
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);

        // Act
        using var parsed = JsonDocument.Parse(JsonSchemaSerialization.ToJson(root, SchemaType.JsonSchema, converter, false));

        // Assert
        var pointer = parsed.RootElement.GetProperty("use").GetProperty("$ref").GetString();
        Assert.Equal("#/target", pointer);
        Assert.Equal("string", parsed.RootElement.GetProperty(pointer!.Substring(2)).GetProperty("type").GetString());
    }

    public sealed class AsyncRoot
    {
        public object? Child { get; set; }
    }

    [JsonConverter(typeof(TargetDictionaryConverter))]
    public sealed class TargetDictionary : ReferenceGraphTraversalTests.GenericOnlyDictionary<JsonSchema>
    {
        [JsonPropertyName("target")] public JsonSchema Target { get; set; } = new() { Type = JsonObjectType.String };
    }

    public sealed class TargetDictionaryConverter : JsonConverter<TargetDictionary>
    {
        public override TargetDictionary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();
        public override void Write(Utf8JsonWriter writer, TargetDictionary value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("target");
            JsonSerializer.Serialize(writer, value.Target, options);
            foreach (var pair in (IDictionary<string, JsonSchema>)value)
            {
                writer.WritePropertyName(pair.Key);
                JsonSerializer.Serialize(writer, pair.Value, options);
            }
            writer.WriteEndObject();
        }
    }

    public class RenamedRoot
    {
        [JsonPropertyName("defs")] public Dictionary<string, JsonSchema> Definitions { get; set; } = new();
        [JsonPropertyName("use")] public JsonSchema Use { get; set; } = null!;
    }
    public sealed class DerivedRoot : RenamedRoot { }
    public sealed class IgnoredRoot
    {
        [JsonPropertyName("callbacks")] public JsonSchema Callbacks { get; set; } = new() { Reference = new JsonSchema() };
    }
    public sealed class ThrowingRoot
    {
        [JsonPropertyName("callbacks")] public JsonSchema Callbacks => throw new InvalidOperationException("Ignored getter evaluated.");
    }
    public sealed class DerivedSchema : JsonSchema
    {
        [JsonPropertyName("callbacks")] public JsonSchema Callbacks { get; set; } = new() { Reference = new JsonSchema() };
    }
    public sealed class CallbackRoot
    {
        [JsonPropertyName("callbacks")] public ReferenceGraphTraversalTests.GenericOnlyDictionary<JsonSchema> Callbacks { get; set; } = new();
    }
    [JsonConverter(typeof(ExtraDictionaryConverter))]
    public sealed class ExtraDictionary : ReferenceGraphTraversalTests.GenericOnlyDictionary<JsonSchema>
    {
        [JsonPropertyName("callbacks")] public JsonSchema Callbacks => throw new InvalidOperationException("Ignored dictionary getter evaluated.");
    }
    public sealed class ExtraDictionaryConverter : JsonConverter<ExtraDictionary>
    {
        public override ExtraDictionary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();
        public override void Write(Utf8JsonWriter writer, ExtraDictionary value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
