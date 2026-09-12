#nullable enable

using System.Collections;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using NJsonSchema.Infrastructure;
using NJsonSchema.References;
using NJsonSchema.Visitors;

namespace NJsonSchema.Tests.References;

public class ReferenceGraphTraversalTests
{
    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.Swagger2)]
    [InlineData(SchemaType.OpenApi3)]
    public async Task Embedded_graph_is_normalized_without_changing_literals(SchemaType dialect)
    {
        // Arrange
        var locations = new[]
        {
            ("patternProperties", """{"x":{"x-model":MODEL}}""", "#/patternProperties/x/x-model"),
            ("items", """[{"x-model":MODEL}]""", "#/items/0/x-model"),
            ("x-dictionaryKey", """{"x-model":MODEL}""", "#/x-dictionaryKey/x-model"),
            ("x-model", """{"type":"object","x-inner":MODEL}""", "#/x-model/x-inner"),
            ("x-array", """[null,{"models":{"value":MODEL}}]""", "#/x-array/1/models/value")
        };
        var example = dialect == SchemaType.OpenApi3 ? "example" : "x-example";
        var literal = """{"type":"foo","properties":{"a":1},"null":null}""";
        foreach (var (name, container, target) in locations)
        {
            foreach (var data in new[] { "7", literal })
            {
                var model = """{"type":"string","default":DATA,"EXAMPLE":LITERAL,"enum":[LITERAL]}"""
                    .Replace("DATA", data).Replace("EXAMPLE", example).Replace("LITERAL", literal);
                var json = """{"properties":{"use":{"$ref":"TARGET"}},"NAME":CONTAINER}"""
                    .Replace("TARGET", target).Replace("NAME", name).Replace("CONTAINER", container.Replace("MODEL", model));

                // Act
                var schema = await Load<JsonSchema>(json, dialect);
                var actual = schema.Properties["use"].ActualSchema;
                var serialized = JsonNode.Parse(actual.ToJson());

                // Assert
                Assert.Equal(JsonObjectType.String, actual.Type);
                if (data == "7")
                {
                    var baseline = await Load<JsonSchema>("""{"default":7}""", dialect);
                    Assert.Equal(baseline.Default, actual.Default);
                    Assert.Equal(baseline.Default!.GetType(), actual.Default!.GetType());
                }
                Assert.True(JsonNode.DeepEquals(JsonNode.Parse(data), serialized!["default"]));
                Assert.True(JsonNode.DeepEquals(JsonNode.Parse(literal), System.Text.Json.JsonSerializer.SerializeToNode(actual.Example)));
                Assert.True(JsonNode.DeepEquals(JsonNode.Parse(literal), serialized["enum"]![0]));
            }
        }
    }

    [Fact]
    public async Task Typed_plain_root_materializes_nested_extensions()
    {
        // Arrange
        const string json = """{"child":{"x-model":{"type":"string"},"properties":{"use":{"$ref":"#/child/x-model"}}}}""";

        // Act
        var document = await Load<TypedDocument>(json, SchemaType.JsonSchema);

        // Assert
        Assert.Equal(JsonObjectType.String, document.Child.Properties["use"].ActualSchema.Type);
    }

    [Fact]
    public async Task Generic_only_dictionary_supports_reference_updates_and_key_paths()
    {
        // Arrange
        var target = new JsonSchema { Type = JsonObjectType.String };
        var reference = new JsonSchema();
        ((IJsonReference)reference).ReferencePath = "#/components/schemas/Value";
        var callbacks = new GenericOnlyDictionary<JsonSchema>();
        ((IDictionary<string, JsonSchema>)callbacks)["onEvent"] = reference;
        var root = new { components = new { schemas = new Dictionary<string, JsonSchema> { ["Value"] = target } }, callbacks };
        var resolver = new JsonReferenceResolver(new JsonSchemaAppender(root, new DefaultTypeNameGenerator()));

        // Act
        await JsonSchemaReferenceUtilities.UpdateSchemaReferencesAsync(root, resolver);
        JsonSchemaReferenceUtilities.UpdateSchemaReferencePaths(root);

        // Assert
        Assert.False(callbacks is IDictionary);
        Assert.Same(target, reference.Reference);
        Assert.Equal("#/components/schemas/Value", ((IJsonReference)reference).ReferencePath);
        Assert.Equal("#/callbacks/onEvent", JsonPathUtilities.GetJsonPath(root, reference));
        Assert.Same(target, resolver.ResolveDocumentReference(root, "#/callbacks/onEvent", typeof(JsonSchema)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Visitors_replace_non_schema_dictionary_values_and_keep_real_list_indices(bool asynchronous)
    {
        // Arrange
        var original = new ReferenceValue();
        var replacement = new ReferenceValue();
        var dictionary = new GenericOnlyDictionary<ReferenceValue>();
        var values = (IDictionary<string, ReferenceValue>)dictionary;
        values["first"] = original;
        values["remove"] = new ReferenceValue();
        var list = new object?[] { null, dictionary };
        var visited = new List<string>();
        IJsonReference Transform(IJsonReference value, string path)
        {
            visited.Add(path);
            return ReferenceEquals(value, original) ? replacement : null!;
        }

        // Act
        if (asynchronous) await new AsyncVisitor(Transform).VisitAsync(list, default);
        else new Visitor(Transform).Visit(list);

        // Assert
        Assert.Same(replacement, values["first"]);
        Assert.False(values.ContainsKey("remove"));
        Assert.Equal(new[] { "#[1]/first", "#[1]/remove" }, visited);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Visitors_use_identity_and_terminate_cycles(bool asynchronous)
    {
        // Arrange
        var first = new ReferenceValue();
        var second = new ReferenceValue();
        first.Child = second;
        second.Child = first;
        var root = new[] { first, second, first };
        var count = 0;
        IJsonReference Transform(IJsonReference value, string path) { count++; return value; }

        // Act
        if (asynchronous) await new AsyncVisitor(Transform).VisitAsync(root, default);
        else new Visitor(Transform).Visit(root);

        // Assert
        Assert.Equal(2, count);
        var paths = JsonPathUtilities.GetJsonPaths(root, new object[] { first, second });
        Assert.Equal(2, paths.Count);
    }

    [Fact]
    public void Reference_paths_distinguish_equal_reference_objects()
    {
        // Arrange
        var first = new ReferenceValue();
        var second = new ReferenceValue();
        var firstUse = new ReferenceValue { Reference = first };
        var secondUse = new ReferenceValue { Reference = second };
        var root = new { targets = new[] { first, second }, uses = new[] { firstUse, secondUse } };

        // Act
        JsonSchemaReferenceUtilities.UpdateSchemaReferencePaths(root);

        // Assert
        Assert.Equal("#/targets/0", firstUse.ReferencePath);
        Assert.Equal("#/targets/1", secondUse.ReferencePath);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Callback_dictionary_is_visited_before_children_and_cycles_terminate(bool asynchronous)
    {
        // Arrange
        var callback = new CallbackDictionary();
        ((IDictionary<string, IJsonReference>)callback)["self"] = callback;
        var child = new ReferenceValue();
        ((IDictionary<string, IJsonReference>)callback)["child"] = child;
        var visited = new List<IJsonReference>();
        IJsonReference Transform(IJsonReference value, string path) { visited.Add(value); return value; }

        // Act
        if (asynchronous) await new AsyncVisitor(Transform).VisitAsync(callback, default);
        else new Visitor(Transform).Visit(callback);

        // Assert
        Assert.Equal(2, visited.Count);
        Assert.Same(callback, visited[0]);
        Assert.Same(child, visited[1]);
        var resolver = new JsonReferenceResolver(new JsonSchemaAppender(callback, new DefaultTypeNameGenerator()));
        Assert.Same(callback, resolver.ResolveDocumentReference(callback, "#", typeof(CallbackDictionary)));
    }

    [Fact]
    public async Task Postprocessing_preserves_shared_nodes_and_handles_equal_nodes_and_cycles()
    {
        // Arrange
        var first = new EqualSchema { Default = System.Text.Json.JsonSerializer.Deserialize<object>("7") };
        var second = new EqualSchema { Default = System.Text.Json.JsonSerializer.Deserialize<object>("7") };
        first.ExtensionData = new Dictionary<string, object?> { ["next"] = second };
        second.ExtensionData = new Dictionary<string, object?> { ["next"] = first };
        var root = new JsonSchema { ExtensionData = new Dictionary<string, object?> { ["nodes"] = new[] { first, second, first } } };
        var baseline = await Load<JsonSchema>("""{"default":7}""", SchemaType.JsonSchema);

        // Act
        await JsonSchemaSerialization.FromJsonAsync<JsonSchema>("{}", SchemaType.JsonSchema, null, loaded =>
        {
            loaded.ExtensionData = root.ExtensionData;
            return new JsonReferenceResolver(new JsonSchemaAppender(loaded, new DefaultTypeNameGenerator()));
        }, JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema));

        // Assert
        Assert.Equal(baseline.Default, first.Default);
        Assert.Equal(baseline.Default, second.Default);
        Assert.Same(second, first.ExtensionData["next"]);
        Assert.Same(first, second.ExtensionData["next"]);
    }

    public sealed class EqualSchema : JsonSchema
    {
        public override bool Equals(object? obj) => obj is EqualSchema;
        public override int GetHashCode() => 1;
    }

    public sealed class CallbackDictionary : GenericOnlyDictionary<IJsonReference>, IJsonReference
    {
        public string? ReferencePath { get; set; }
        public string? DocumentPath { get; set; }
        public IJsonReference? Reference { get; set; }
        public IJsonReference ActualObject => this;
        public object? PossibleRoot => null;
    }

    [Fact]
    public async Task References_inside_materialized_extension_schemas_are_resolved()
    {
        // Arrange
        const string json = """{"definitions":{"Value":{"type":"string"}},"x-model":{"type":"object","properties":{"use":{"$ref":"#/definitions/Value"}}}}""";

        // Act
        var root = await Load<JsonSchema>(json, SchemaType.JsonSchema);

        // Assert
        var model = Assert.IsType<JsonSchema>(root.ExtensionData!["x-model"]);
        Assert.Same(root.Definitions["Value"], model.Properties["use"].Reference);
    }

    [Fact]
    public async Task Generic_dictionary_target_materializes_with_active_options()
    {
        // Arrange
        var dictionary = new GenericOnlyDictionary<object>();
        var values = (IDictionary<string, object>)dictionary;
        values["type"] = "string";
        values["nullable"] = true;

        // Act
        var root = await JsonSchemaSerialization.FromJsonAsync<JsonSchema>(
            """{"properties":{"use":{"$ref":"#/x-target"}}}""", SchemaType.OpenApi3, null, loaded =>
            {
                loaded.ExtensionData = new Dictionary<string, object?> { ["x-target"] = dictionary };
                return new JsonReferenceResolver(new JsonSchemaAppender(loaded, new DefaultTypeNameGenerator()));
            }, JsonSchema.CreateSchemaSerializationConverter(SchemaType.OpenApi3));

        // Assert
        Assert.Equal(JsonObjectType.String, root.Properties["use"].ActualSchema.Type);
        Assert.True(root.Properties["use"].ActualSchema.IsNullableRaw);
        var resolver = new JsonReferenceResolver(new JsonSchemaAppender(dictionary, new DefaultTypeNameGenerator()));
        Assert.Throws<InvalidOperationException>(() => resolver.ResolveDocumentReference(dictionary, "#", typeof(JsonSchema)));
    }

    private static Task<T> Load<T>(string json, SchemaType dialect) where T : notnull =>
        JsonSchemaSerialization.FromJsonAsync<T>(json, dialect, null,
            root => new JsonReferenceResolver(new JsonSchemaAppender(root!, new DefaultTypeNameGenerator())),
            JsonSchema.CreateSchemaSerializationConverter(dialect));

    public sealed class TypedDocument
    {
        [JsonPropertyName("child")]
        public JsonSchema Child { get; set; } = null!;
    }

    public class ReferenceValue : IJsonReference
    {
        public ReferenceValue? Child { get; set; }
        public string? ReferencePath { get; set; }
        public string? DocumentPath { get; set; }
        [JsonIgnore] public IJsonReference? Reference { get; set; }
        [JsonIgnore] public IJsonReference ActualObject => this;
        [JsonIgnore] public object? PossibleRoot => null;
        public override bool Equals(object? obj) => obj is ReferenceValue;
        public override int GetHashCode() => 1;
    }

    private sealed class Visitor(Func<IJsonReference, string, IJsonReference> transform) : JsonReferenceVisitorBase
    {
        protected override IJsonReference VisitJsonReference(IJsonReference reference, string path, string? typeNameHint) => transform(reference, path);
    }

    private sealed class AsyncVisitor(Func<IJsonReference, string, IJsonReference> transform) : AsyncJsonReferenceVisitorBase
    {
        protected override Task<IJsonReference> VisitJsonReferenceAsync(IJsonReference reference, string path, string? typeNameHint, CancellationToken cancellationToken) => Task.FromResult(transform(reference, path));
    }

    public class GenericOnlyDictionary<T> : IDictionary<string, T>
    {
        private readonly Dictionary<string, T> values = new();
        T IDictionary<string, T>.this[string key] { get => values[key]; set => values[key] = value; }
        ICollection<string> IDictionary<string, T>.Keys => values.Keys;
        ICollection<T> IDictionary<string, T>.Values => values.Values;
        int ICollection<KeyValuePair<string, T>>.Count => values.Count;
        bool ICollection<KeyValuePair<string, T>>.IsReadOnly => false;
        void IDictionary<string, T>.Add(string key, T value) => values.Add(key, value);
        bool IDictionary<string, T>.ContainsKey(string key) => values.ContainsKey(key);
        bool IDictionary<string, T>.Remove(string key) => values.Remove(key);
        bool IDictionary<string, T>.TryGetValue(string key, out T value) => values.TryGetValue(key, out value!);
        void ICollection<KeyValuePair<string, T>>.Add(KeyValuePair<string, T> item) => ((ICollection<KeyValuePair<string, T>>)values).Add(item);
        void ICollection<KeyValuePair<string, T>>.Clear() => values.Clear();
        bool ICollection<KeyValuePair<string, T>>.Contains(KeyValuePair<string, T> item) => ((ICollection<KeyValuePair<string, T>>)values).Contains(item);
        void ICollection<KeyValuePair<string, T>>.CopyTo(KeyValuePair<string, T>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, T>>)values).CopyTo(array, arrayIndex);
        bool ICollection<KeyValuePair<string, T>>.Remove(KeyValuePair<string, T> item) => ((ICollection<KeyValuePair<string, T>>)values).Remove(item);
        IEnumerator<KeyValuePair<string, T>> IEnumerable<KeyValuePair<string, T>>.GetEnumerator() => values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<string, T>>)this).GetEnumerator();
    }
}
