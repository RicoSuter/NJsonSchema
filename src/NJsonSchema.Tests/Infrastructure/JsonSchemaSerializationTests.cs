#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Infrastructure;

public class JsonSchemaSerializationTests
{
    [Fact]
    public void ToJson_restores_thread_static_state_on_exception()
    {
        // Arrange
        // A converter registered via AddConverter is applied to every serializer options
        // build; make it throw on write and observe the thread-static state afterwards.
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.OpenApi3);
        converter.AddConverter(new ThrowingStringConverter());

        var schema = new JsonSchema { Title = "boom" };

        var previousIsWriting = JsonSchemaSerialization.IsWriting;
        var previousSchemaType = JsonSchemaSerialization.CurrentSchemaType;
        var previousOptions = JsonSchemaSerialization.CurrentSerializerOptions;

        // Act
        Assert.Throws<InvalidOperationException>(
            () => JsonSchemaSerialization.ToJson(schema, SchemaType.OpenApi3, converter, writeIndented: false));

        // Assert
        Assert.Equal(previousIsWriting, JsonSchemaSerialization.IsWriting);
        Assert.Equal(previousSchemaType, JsonSchemaSerialization.CurrentSchemaType);
        Assert.Same(previousOptions, JsonSchemaSerialization.CurrentSerializerOptions);
    }

    [Fact]
    public void ResolveDocumentReference_throws_when_CurrentSerializerOptions_is_null()
    {
        // Arrange
        // Construct a resolver and hit the IDictionary path with an empty segment list
        // (what jsonPath "#" expands to). CurrentSerializerOptions is [ThreadStatic]-null
        // unless a higher-level FromJsonAsync call has set it.
        Assert.Null(JsonSchemaSerialization.CurrentSerializerOptions);

        var resolver = new NJsonSchema.JsonReferenceResolver(
            new JsonSchemaAppender(new JsonSchema(), new DefaultTypeNameGenerator()));
        var rootDictionary = new Dictionary<string, object?>();

        // Act / Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => resolver.ResolveDocumentReference(rootDictionary, "#", typeof(JsonSchema)));
        Assert.Contains("CurrentSerializerOptions", ex.Message);
    }

    [Fact]
    public void FromJson_Stream_handles_lenient_json()
    {
        // Arrange
        // Same lenient-JSON forms the string overload already recovers from: single-quoted
        // string values and unquoted property names. The stream overload must buffer and
        // delegate so callers don't silently regress compared to v11.
        var lenient = "{ 'title': 'example', description: 'foo' }";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(lenient));

        // Act
        var schema = JsonSchemaSerialization.FromJson<JsonSchema>(stream, converter: null);

        // Assert
        Assert.NotNull(schema);
        Assert.Equal("example", schema!.Title);
        Assert.Equal("foo", schema.Description);
    }

    private sealed class ThrowingStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetString();

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => throw new InvalidOperationException("boom");
    }
}
#nullable restore
