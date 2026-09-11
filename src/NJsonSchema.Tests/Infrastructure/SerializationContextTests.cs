#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using NJsonSchema.Infrastructure;
using NJsonSchema.References;

namespace NJsonSchema.Tests.Infrastructure;

public class SerializationContextTests
{
    private const string ReferencesJson = """
        {"allOf":[{"$ref":"https://example.test/external.json"},{"$ref":"#/x-schema"}],"x-schema":{"title":"local"}}
        """;

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.Swagger2)]
    [InlineData(SchemaType.OpenApi3)]
    public async Task Suspended_resolution_preserves_context(SchemaType dialect)
    {
        // Arrange
        var caller = new State();
        var entered = Signal();
        var release = Signal();
        var converter = Converter(dialect);
        var load = JsonSchemaSerialization.FromJsonAsync<JsonSchema>(ReferencesJson, dialect, null,
            root => new SuspendingResolver(root, entered, release.Task), converter);
        try
        {
            // Act
            await WaitFor(entered.Task);
            Assert.False(load.IsCompleted);
            release.TrySetResult(true);
            var schema = await load;

            // Assert
            Assert.Equal("local", schema.AllOf.Last().ActualSchema.Title);
            caller.AssertCurrent();
        }
        finally
        {
            release.TrySetResult(true);
        }
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema, "x-nullable")]
    [InlineData(SchemaType.Swagger2, "x-nullable")]
    [InlineData(SchemaType.OpenApi3, "nullable")]
    public async Task Embedded_schema_uses_exact_loader_converters(SchemaType dialect, string keyword)
    {
        // Arrange
        var caller = new State();
        var converter = Converter(dialect, "converted:");

        // Act
        var schema = await Load("{\"x-model\":{\"type\":\"string\",\"title\":\"nested\",\"" + keyword + "\":true}}", dialect, converter);

        // Assert
        AssertModel(schema, "converted:");
        caller.AssertCurrent();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Loader_factory_and_embedded_schema_share_options(bool useStream)
    {
        // Arrange
        var caller = new State();
        var observer = new ObservingConverter();
        var converter = Converter();
        converter.AddConverter(observer);
        const string json = """{"title":"root","x-model":{"type":"string","title":"nested","nullable":true}}""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        JsonReferenceResolver Factory(JsonSchema root)
        {
            Assert.Same(observer.Options, JsonSchemaSerialization.CurrentSerializerOptions);
            Assert.Equal(SchemaType.OpenApi3, JsonSchemaSerialization.CurrentSchemaType);
            Assert.False(JsonSchemaSerialization.IsWriting);
            return new JsonReferenceResolver(new JsonSchemaAppender(root, new DefaultTypeNameGenerator()));
        }

        // Act
        var schema = useStream
            ? await JsonSchemaSerialization.FromJsonAsync<JsonSchema>(stream, SchemaType.OpenApi3, "example.json", Factory, converter)
            : await JsonSchemaSerialization.FromJsonAsync<JsonSchema>(json, SchemaType.OpenApi3, "example.json", Factory, converter);

        // Assert
        Assert.Equal("example.json", schema.DocumentPath);
        Assert.Equal(2, observer.ReadCount);
        Assert.True(Assert.IsType<JsonSchema>(schema.ExtensionData!["x-model"]).IsNullableRaw);
        caller.AssertCurrent();
    }

    [Fact]
    public void Nested_read_restores_writing_context()
    {
        // Arrange
        var caller = new State();
        var converter = Converter(SchemaType.Swagger2);
        converter.AddConverter(new NestedReadConverter());

        // Act
        var json = JsonSchemaSerialization.ToJson(new JsonSchema { Title = "outer" }, SchemaType.Swagger2, converter, false);

        // Assert
        Assert.Contains("outer", json);
        caller.AssertCurrent();
    }

    [Fact]
    public async Task Concurrent_loads_resumed_in_reverse_order_are_isolated()
    {
        // Arrange
        var caller = new State();
        var firstEntered = Signal();
        var secondEntered = Signal();
        var firstRelease = Signal();
        var secondRelease = Signal();
        var first = GatedLoad(SchemaType.Swagger2, "x-nullable", "first:", firstEntered, firstRelease);
        var second = GatedLoad(SchemaType.OpenApi3, "nullable", "second:", secondEntered, secondRelease);
        try
        {
            // Act
            await WaitFor(firstEntered.Task);
            await WaitFor(secondEntered.Task);
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);
            secondRelease.TrySetResult(true);
            var secondSchema = await second;
            firstRelease.TrySetResult(true);
            var firstSchema = await first;

            // Assert
            AssertModel(firstSchema, "first:");
            AssertModel(secondSchema, "second:");
            caller.AssertCurrent();
        }
        finally
        {
            firstRelease.TrySetResult(true);
            secondRelease.TrySetResult(true);
        }
    }

    [Fact]
    public async Task Nested_operations_restore_outer_context_including_failures()
    {
        // Arrange
        var caller = new State();

        // Act
        await JsonSchemaSerialization.FromJsonAsync<JsonSchema>(ReferencesJson, SchemaType.Swagger2, null,
            root => new CallbackResolver(root, async () =>
            {
                var outer = new State();
                Assert.Equal(SchemaType.Swagger2, JsonSchemaSerialization.CurrentSchemaType);
                await Load("{}", SchemaType.OpenApi3, Converter());
                outer.AssertCurrent();
                JsonSchemaSerialization.FromJson<JsonSchema>("{}", Converter());
                outer.AssertCurrent();
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}")))
                {
                    JsonSchemaSerialization.FromJson<JsonSchema>(stream, Converter());
                }
                outer.AssertCurrent();
                JsonSchemaSerialization.ToJson(new JsonSchema(), SchemaType.OpenApi3, Converter(), false);
                outer.AssertCurrent();
                Assert.Throws<JsonException>(() => JsonSchemaSerialization.FromJson<JsonSchema>("{", Converter()));
                outer.AssertCurrent();
                await Assert.ThrowsAsync<JsonException>(() => Load("{", SchemaType.OpenApi3, Converter()));
                outer.AssertCurrent();
                var throwing = Converter();
                throwing.AddConverter(new ThrowingConverter());
                Assert.Throws<InvalidOperationException>(() => JsonSchemaSerialization.FromJson<JsonSchema>("{\"title\":\"fail\"}", throwing));
                outer.AssertCurrent();
                await Assert.ThrowsAsync<InvalidOperationException>(() => Load("{\"title\":\"fail\"}", SchemaType.OpenApi3, throwing));
                outer.AssertCurrent();
                Assert.Throws<InvalidOperationException>(() => JsonSchemaSerialization.ToJson(new JsonSchema { Title = "fail" }, SchemaType.OpenApi3, throwing, false));
                outer.AssertCurrent();
                await Assert.ThrowsAnyAsync<Exception>(() => JsonSchemaSerialization.FromJsonAsync<JsonSchema>(ReferencesJson, SchemaType.OpenApi3, null,
                    nested => new CallbackResolver(nested, () => throw new InvalidOperationException("resolver")), Converter()));
                outer.AssertCurrent();
                using var cancellation = new CancellationTokenSource();
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Load("{}", SchemaType.OpenApi3, Converter(), cancellation.Token));
                outer.AssertCurrent();
            }), Converter(SchemaType.Swagger2));

        // Assert
        caller.AssertCurrent();
    }

    [Fact]
    public async Task Cancellation_after_suspension_restores_caller_context()
    {
        // Arrange
        var caller = new State();
        var entered = Signal();
        var release = Signal();
        using var cancellation = new CancellationTokenSource();
        var load = JsonSchemaSerialization.FromJsonAsync<JsonSchema>(ReferencesJson, SchemaType.OpenApi3, null,
            root => new SuspendingResolver(root, entered, release.Task), Converter(), cancellation.Token);
        try
        {
            // Act
            await WaitFor(entered.Task);
            Assert.False(load.IsCompleted);
            cancellation.Cancel();
            release.TrySetResult(true);
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => load);

            // Assert
            Assert.IsAssignableFrom<OperationCanceledException>(exception.InnerException);
            caller.AssertCurrent();
        }
        finally
        {
            release.TrySetResult(true);
        }
    }

    private static Task<JsonSchema> GatedLoad(SchemaType dialect, string keyword, string prefix,
        TaskCompletionSource<bool> entered, TaskCompletionSource<bool> release)
        => JsonSchemaSerialization.FromJsonAsync<JsonSchema>(
            "{\"x-model\":{\"type\":\"string\",\"title\":\"nested\",\"" + keyword + "\":true}}",
            dialect, null, root =>
            {
                var reference = new JsonSchema();
                ((IJsonReference)reference).ReferencePath = "https://example.test/external.json";
                root.AllOf.Add(reference);
                return new SuspendingResolver(root, entered, release.Task);
            }, Converter(dialect, prefix));

    private static void AssertModel(JsonSchema schema, string prefix)
    {
        var model = Assert.IsType<JsonSchema>(schema.ExtensionData!["x-model"]);
        Assert.True(model.IsNullableRaw);
        Assert.Equal(JsonObjectType.String, model.Type);
        Assert.Equal(prefix + "nested", model.Title);
        Assert.False(model.ExtensionData?.ContainsKey("nullable") == true);
        Assert.False(model.ExtensionData?.ContainsKey("x-nullable") == true);
    }

    private static SchemaSerializationConverter Converter(SchemaType dialect = SchemaType.OpenApi3, string? prefix = null)
    {
        var converter = JsonSchema.CreateSchemaSerializationConverter(dialect);
        if (prefix != null)
        {
            converter.AddConverter(new PrefixConverter(prefix));
        }
        return converter;
    }

    private static Task<JsonSchema> Load(string json, SchemaType dialect, SchemaSerializationConverter converter, CancellationToken cancellationToken = default)
        => JsonSchemaSerialization.FromJsonAsync<JsonSchema>(json, dialect, null,
            JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator()), converter, cancellationToken);

    private static TaskCompletionSource<bool> Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task WaitFor(Task signal)
    {
        Assert.Same(signal, await Task.WhenAny(signal, Task.Delay(TimeSpan.FromSeconds(10))));
        await signal;
    }

    private sealed class State
    {
        private readonly SchemaType _dialect = JsonSchemaSerialization.CurrentSchemaType;
        private readonly JsonSerializerOptions? _options = JsonSchemaSerialization.CurrentSerializerOptions;
        private readonly bool _writing = JsonSchemaSerialization.IsWriting;

        public void AssertCurrent()
        {
            Assert.Equal(_dialect, JsonSchemaSerialization.CurrentSchemaType);
            Assert.Same(_options, JsonSchemaSerialization.CurrentSerializerOptions);
            Assert.Equal(_writing, JsonSchemaSerialization.IsWriting);
        }
    }

    private sealed class SuspendingResolver(JsonSchema root, TaskCompletionSource<bool> entered, Task release)
        : JsonReferenceResolver(new JsonSchemaAppender(root, new DefaultTypeNameGenerator()))
    {
        public override async Task<IJsonReference> ResolveUrlReferenceAsync(string url, CancellationToken cancellationToken = default)
        {
            var expectedOptions = JsonSchemaSerialization.CurrentSerializerOptions;
            var expectedDialect = JsonSchemaSerialization.CurrentSchemaType;
            entered.TrySetResult(true);
            await release.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Same(expectedOptions, JsonSchemaSerialization.CurrentSerializerOptions);
            Assert.Equal(expectedDialect, JsonSchemaSerialization.CurrentSchemaType);
            Assert.False(JsonSchemaSerialization.IsWriting);
            return new JsonSchema { Type = JsonObjectType.String };
        }
    }

    private sealed class CallbackResolver(JsonSchema root, Func<Task> callback)
        : JsonReferenceResolver(new JsonSchemaAppender(root, new DefaultTypeNameGenerator()))
    {
        public override async Task<IJsonReference> ResolveUrlReferenceAsync(string url, CancellationToken cancellationToken = default)
        {
            await callback();
            return new JsonSchema { Type = JsonObjectType.String };
        }
    }

    private sealed class PrefixConverter(string prefix) : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => prefix + reader.GetString();
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
    }

    private sealed class ObservingConverter : JsonConverter<string>
    {
        public JsonSerializerOptions? Options { get; private set; }
        public int ReadCount { get; private set; }

        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // The property-filter converter intentionally uses an inner options copy.
            // The operation's original options must remain available throughout both reads.
            Options ??= JsonSchemaSerialization.CurrentSerializerOptions;
            Assert.NotNull(Options);
            Assert.Same(Options, JsonSchemaSerialization.CurrentSerializerOptions);
            ReadCount++;
            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
    }

    private sealed class NestedReadConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetString();

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            var outer = new State();
            Assert.True(JsonSchemaSerialization.IsWriting);
            Assert.Equal(SchemaType.Swagger2, JsonSchemaSerialization.CurrentSchemaType);
            JsonSchemaSerialization.FromJson<JsonSchema>("{}", Converter());
            outer.AssertCurrent();
            Assert.Throws<JsonException>(() => JsonSchemaSerialization.FromJson<JsonSchema>("{", Converter()));
            outer.AssertCurrent();
            writer.WriteStringValue(value);
        }
    }

    private sealed class ThrowingConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new InvalidOperationException("converter");
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => throw new InvalidOperationException("converter");
    }
}
