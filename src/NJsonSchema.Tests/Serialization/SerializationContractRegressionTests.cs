#nullable enable
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NJsonSchema.Infrastructure;
using NJsonSchema.References;

namespace NJsonSchema.Tests.Serialization;

public class SerializationContractRegressionTests
{
    [Fact]
    public void Derived_schema_json_ignore_still_owns_its_input_contract()
    {
        // Arrange
        const string json = """{"Secret":{"incompatible":true}}""";

        // Act
        var schema = JsonSchemaSerialization.FromJson<IgnoredDerivedSchema>(json, JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema))!;

        // Assert
        Assert.Equal("kept", schema.Secret);
        Assert.True(schema.ExtensionData == null || !schema.ExtensionData.ContainsKey("Secret"));
        Assert.Null(JsonNode.Parse(schema.ToJson())!["Secret"]);
    }

    public class IgnoredDerivedSchema : JsonSchema
    {
        [JsonIgnore]
        public string Secret { get; set; } = "kept";
    }

    [Fact]
    public async Task Unknown_schema_keys_do_not_bind_to_ignored_clr_metadata()
    {
        // Arrange
        const string json = """{"properties":{"bar":{"name":{"type":"string"},"parent":{"type":"integer"}}}}""";

        // Act
        var schema = await JsonSchema.FromJsonAsync(json);
        var output = JsonNode.Parse(schema.ToJson())!;

        // Assert
        Assert.Equal("bar", schema.Properties["bar"].Name);
        Assert.Same(schema, schema.Properties["bar"].Parent);
        Assert.Equal("string", output["properties"]!["bar"]!["name"]!["type"]!.GetValue<string>());
        Assert.Equal("integer", output["properties"]!["bar"]!["parent"]!["type"]!.GetValue<string>());
    }

    [Fact]
    public void Exact_reverse_aliases_win_before_case_insensitive_aliases()
    {
        // Arrange
        var converter = new SchemaSerializationConverter();
        converter.RenameProperty(typeof(AliasHolder), "First", "Alias");
        converter.RenameProperty(typeof(AliasHolder), "Second", "alias");

        // Act
        var result = JsonSchemaSerialization.FromJson<AliasHolder>("""{"alias":2}""", converter)!;

        // Assert
        Assert.Equal(0, result.First);
        Assert.Equal(2, result.Second);
    }

    public class AliasHolder
    {
        public int First { get; set; }
        public int Second { get; set; }
    }

    [Theory]
    [InlineData(true, 9)]
    [InlineData(false, 7)]
    public void Reverse_renames_follow_the_active_case_policy(bool insensitive, int expected)
    {
        // Arrange
        var converter = new SchemaSerializationConverter();
        converter.RenameProperty(typeof(IgnoredHolder), "secret", "wire_secret");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = insensitive };
        options.Converters.Add(converter);

        // Act
        var result = JsonSerializer.Deserialize<IgnoredHolder>("""{"WIRE_SECRET":9}""", options)!;
        var collision = JsonSerializer.Deserialize<IgnoredHolder>("""{"secret":3,"wire_secret":9}""", options)!;

        // Assert
        Assert.Equal(expected, result.Secret);
        Assert.Equal(3, collision.Secret);
        Assert.Equal(9, ((JsonElement)collision.ExtensionData!["wire_secret"]).GetInt32());
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.Swagger2)]
    [InlineData(SchemaType.OpenApi3)]
    public void Schema_member_matching_preserves_dictionary_and_vendor_key_case(SchemaType dialect)
    {
        foreach (var spelling in new[] { "readOnly", "readonly", "READONLY" })
        {
            // Arrange
            var json = $$$$"""{"TYPE":"object","PROPERTIES":{"values":{"type":"object","{{{{spelling}}}}":true,"ADDITIONALPROPERTIES":{"type":"string"}},"Foo":{"type":"string","{{{{spelling}}}}":true},"foo":{"type":"string"}},"x-payload":{"Foo":1,"foo":2,"READONLY":"true"}}""";

            // Act
            var schema = JsonSchemaSerialization.FromJson<JsonSchema>(json, JsonSchema.CreateSchemaSerializationConverter(dialect))!;

            // Assert
            Assert.True(schema.Properties["values"].IsReadOnly);
            Assert.Equal(JsonObjectType.String, schema.Properties["values"].AdditionalPropertiesSchema!.Type);
            Assert.True(schema.Properties["Foo"].IsReadOnly);
            Assert.False(schema.Properties["foo"].IsReadOnly);
            var wire = JsonNode.Parse(JsonSchemaSerialization.ToJson(schema, dialect, JsonSchema.CreateSchemaSerializationConverter(dialect), false))!;
            var outputName = dialect == SchemaType.JsonSchema ? "readonly" : "readOnly";
            Assert.True(wire["properties"]!["values"]![outputName]!.GetValue<bool>());
            Assert.Null(wire["properties"]!["values"]!["x-readOnly"]);
            Assert.True(schema.Properties["values"].ExtensionData == null || !schema.Properties["values"].ExtensionData!.ContainsKey(spelling));
            var payload = JsonSerializer.SerializeToNode(schema.ExtensionData!["x-payload"])!;
            Assert.Equal(1, payload["Foo"]!.GetValue<int>());
            Assert.Equal(2, payload["foo"]!.GetValue<int>());
            Assert.Equal("true", payload["READONLY"]!.GetValue<string>());
        }
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.Swagger2)]
    [InlineData(SchemaType.OpenApi3)]
    public void Xml_metadata_roundtrips(SchemaType dialect)
    {
        // Arrange
        const string json = """{"xml":{"name":"record","namespace":"urn:records","prefix":"r","wrapped":true,"attribute":true}}""";
        var converter = JsonSchema.CreateSchemaSerializationConverter(dialect);

        // Act
        var schema = JsonSchemaSerialization.FromJson<JsonSchema>(json, converter)!;
        var output = JsonNode.Parse(JsonSchemaSerialization.ToJson(schema, dialect, converter, false))!;

        // Assert
        Assert.Equal("record", schema.Xml!.Name);
        Assert.Equal("urn:records", schema.Xml.Namespace);
        Assert.Equal("r", schema.Xml.Prefix);
        Assert.True(schema.Xml.Wrapped);
        Assert.True(schema.Xml.Attribute);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json)!["xml"], output["xml"]));
        Assert.Null(output["xml"]!["ParentSchema"]);
        var disabled = JsonSchemaSerialization.FromJson<JsonSchema>("""{"xml":{"wrapped":false,"attribute":false}}""", converter)!;
        Assert.False(disabled.Xml!.Wrapped);
        Assert.False(disabled.Xml.Attribute);
        Assert.Null(JsonSchemaSerialization.FromJson<JsonSchema>("{}", converter)!.Xml);
    }

    [Fact]
    public void Property_converters_preserve_precedence_and_mapping_contract()
    {
        // Arrange
        var converter = new SchemaSerializationConverter();
        converter.IgnoreProperty(typeof(ConvertedHolder));
        converter.RenameProperty(typeof(ConvertedHolder), "Renamed", "wire_name");
        converter.AddConverter(new OptionsMarkerConverter());
        var holder = new ConvertedHolder();
        ((IJsonReferenceBase)holder.Mapping["pet"]).ReferencePath = "#/definitions/Pet";

        // Act
        var output = JsonNode.Parse(JsonSchemaSerialization.ToJson(holder, SchemaType.JsonSchema, converter, false))!;
        var read = JsonSchemaSerialization.FromJson<ConvertedHolder>(output.ToJsonString(), converter)!;

        // Assert
        Assert.Equal("Active", output["Mode"]!.GetValue<string>());
        Assert.Equal("property", output["Property"]!.GetValue<string>());
        Assert.Equal("options", output["Options"]!.GetValue<string>());
        Assert.Equal("property", output["Factory"]!.GetValue<string>());
        Assert.Equal("property", output["wire_name"]!.GetValue<string>());
        Assert.Null(output["Renamed"]);
        Assert.True(output.AsObject().ContainsKey("Null"));
        Assert.Null(output["Null"]);
        Assert.Equal("#/definitions/Pet", output["Mapping"]!["pet"]!.GetValue<string>());
        Assert.Equal("#/definitions/Pet", ((IJsonReferenceBase)read.Mapping["pet"]).ReferencePath);
        Assert.Equal("property", read.Property.Value);
        Assert.Equal("options", read.Options.Value);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(holder))!["Mapping"], output["Mapping"]));
    }

    [Fact]
    public void Explicit_type_and_added_converters_own_registered_types()
    {
        // Arrange
        var converter = new SchemaSerializationConverter();
        converter.IgnoreProperty(typeof(Marker));

        // Act
        var typeOutput = JsonSchemaSerialization.ToJson(new Marker(), SchemaType.JsonSchema, converter, false);
        converter.AddConverter(new OptionsMarkerConverter());
        var optionsOutput = JsonSchemaSerialization.ToJson(new Marker(), SchemaType.JsonSchema, converter, false);

        // Assert
        Assert.Equal("\"type\"", typeOutput);
        Assert.Equal("\"options\"", optionsOutput);
    }

    [Fact]
    public void Ignored_inputs_are_removed_before_reading_nested_values()
    {
        // Arrange
        var converter = new SchemaSerializationConverter();
        converter.IgnoreProperty(typeof(IgnoredHolder), "secret");
        converter.RenameProperty(typeof(IgnoredHolder), "secret", "renamed_secret");
        converter.IgnoreProperty(typeof(NestedHolder));
        const string child = """{"secret":{"incompatible":true},"renamed_secret":[],"kept":9,"x-vendor":{"secret":"literal"}}""";
        var json = "{\"Single\":" + child + ",\"List\":[" + child + "],\"Dictionary\":{\"key\":" + child + "}}";

        // Act
        var root = JsonSchemaSerialization.FromJson<NestedHolder>(json, converter)!;
        var direct = JsonSchemaSerialization.FromJson<InheritedHolder>(child, converter)!;

        // Assert
        foreach (var holder in new IgnoredHolder[] { direct, root.Single, root.List[0], root.Dictionary["key"] })
        {
            Assert.Equal(7, holder.Secret);
            Assert.Equal(9, holder.Kept);
            Assert.Single(holder.ExtensionData!);
            Assert.Equal("literal", ((JsonElement)holder.ExtensionData!["x-vendor"]).GetProperty("secret").GetString());
        }
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.Swagger2)]
    [InlineData(SchemaType.OpenApi3)]
    public void Runtime_schema_members_survive_declared_base_containers(SchemaType dialect)
    {
        // Arrange
        var root = new DerivedSchema { Item = new DerivedSchema(), Child = new DerivedSchema() };
        root.Definitions["Child"] = new DerivedSchema();
        root.AllOf.Add(new DerivedSchema());
        var converter = JsonSchema.CreateSchemaSerializationConverter(dialect);

        // Act
        var output = JsonNode.Parse(JsonSchemaSerialization.ToJson(root, dialect, converter, false))!;

        // Assert
        foreach (var node in new[] { output, output["definitions"]!["Child"]!, output["allOf"]![0]!, output["items"]!, output["Child"]! })
        {
            Assert.Equal("kept", node["custom_name"]!.GetValue<string>());
            Assert.Equal("Active", node["Mode"]!.GetValue<string>());
            Assert.Equal("included", node["Included"]!.GetValue<string>());
            Assert.Null(node["Hidden"]);
        }
        Assert.Equal("property", output["ConvertedChild"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("kept", true)]
    [InlineData("omit", false)]
    public void Runtime_metadata_respects_resolver_and_ShouldSerialize(string label, bool emitted)
    {
        // Arrange
        var converter = new SchemaSerializationConverter();
        converter.IgnoreProperty(typeof(JsonSchema));
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            if (typeInfo.Type == typeof(DerivedSchema))
            {
                var property = typeInfo.Properties.Single(property => property.Name == "custom_name");
                property.Name = "resolver_name";
                property.ShouldSerialize = (_, value) => Equals(value, "kept");
            }
        });
        var options = new JsonSerializerOptions { TypeInfoResolver = resolver };
        options.Converters.Add(converter);
        var root = new JsonSchema();
        root.Definitions["Child"] = new DerivedSchema { Label = label };

        // Act
        var child = JsonNode.Parse(JsonSerializer.Serialize(root, options))!["definitions"]!["Child"]!;

        // Assert
        Assert.Equal(emitted, child.AsObject().ContainsKey("resolver_name"));
        Assert.Null(child["custom_name"]);
    }

    [Fact]
    public void Added_base_converter_wins_over_filter_and_runtime_dispatch()
    {
        // Arrange
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);
        converter.IgnoreProperty(typeof(BaseSchemaHolder));
        converter.AddConverter(new SchemaMarkerConverter());
        var holder = new BaseSchemaHolder { Value = new DerivedSchema() };

        // Act
        var output = JsonNode.Parse(JsonSchemaSerialization.ToJson(holder, SchemaType.JsonSchema, converter, false))!;

        // Assert
        Assert.Equal("property", output["Value"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("secret")]
    [InlineData("renamed_secret")]
    public void Ignore_matches_original_and_wire_names_with_active_case_policy(string ignoredName)
    {
        // Arrange
        var converter = new SchemaSerializationConverter();
        converter.IgnoreProperty(typeof(IgnoredHolder), ignoredName);
        converter.RenameProperty(typeof(IgnoredHolder), "secret", "renamed_secret");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(converter);

        // Act
        var result = JsonSerializer.Deserialize<IgnoredHolder>("""{"SECRET":{},"RENAMED_SECRET":[],"KEPT":9}""", options)!;

        // Assert
        Assert.Equal(7, result.Secret);
        Assert.Equal(9, result.Kept);
        Assert.True(result.ExtensionData == null || result.ExtensionData.Count == 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Nested_type_and_options_converters_receive_unchanged_input(bool useOptionsConverter)
    {
        // Arrange
        var converter = new SchemaSerializationConverter();
        var payloadType = useOptionsConverter ? typeof(OptionsPayload) : typeof(AttributedPayload);
        converter.IgnoreProperty(payloadType, "secret");
        converter.RenameProperty(payloadType, "original", "renamed");
        converter.IgnoreProperty(typeof(RawHolder<AttributedPayload>));
        converter.IgnoreProperty(typeof(RawHolder<OptionsPayload>));
        if (useOptionsConverter) converter.AddConverter(new RawPayloadConverter<OptionsPayload>());
        const string payload = """{"secret":"literal","renamed":9}""";
        var json = "{\"Value\":" + payload + "}";

        // Act
        var captured = useOptionsConverter
            ? JsonSchemaSerialization.FromJson<RawHolder<OptionsPayload>>(json, converter)!.Value.Payload
            : JsonSchemaSerialization.FromJson<RawHolder<AttributedPayload>>(json, converter)!.Value.Payload;

        // Assert
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(payload), JsonNode.Parse(captured)));
    }

    [Fact]
    public void Converter_owned_dictionary_payload_is_not_traversed_as_dictionary_values()
    {
        // Arrange
        var converter = new SchemaSerializationConverter();
        converter.IgnoreProperty(typeof(RawHolder<RawDictionary>));
        converter.IgnoreProperty(typeof(IgnoredHolder), "secret");
        converter.RenameProperty(typeof(IgnoredHolder), "original", "renamed");
        converter.AddConverter(new RawPayloadConverter<RawDictionary>());
        const string payload = """{"entry":{"secret":"literal","renamed":9}}""";
        var json = "{\"Value\":" + payload + "}";

        // Act
        var result = JsonSchemaSerialization.FromJson<RawHolder<RawDictionary>>(json, converter)!;

        // Assert
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(payload), JsonNode.Parse(result.Value.Payload)));
    }

    public interface IRawPayload
    {
        string Payload { get; set; }
    }
    [JsonConverter(typeof(RawPayloadConverter<AttributedPayload>))]
    public class AttributedPayload : IRawPayload
    {
        public string Payload { get; set; } = "{}";
    }
    public class OptionsPayload : IRawPayload
    {
        public string Payload { get; set; } = "{}";
    }
    public class RawDictionary : Dictionary<string, IgnoredHolder>, IRawPayload
    {
        public string Payload { get; set; } = "{}";
    }
    public class RawHolder<T> where T : new()
    {
        public T Value { get; set; } = new();
    }
    public class RawPayloadConverter<T> : JsonConverter<T> where T : IRawPayload, new()
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return new T { Payload = document.RootElement.GetRawText() };
        }
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            using var document = JsonDocument.Parse(value.Payload);
            document.RootElement.WriteTo(writer);
        }
    }

    public class BaseSchemaHolder
    {
        public JsonSchema Value { get; set; } = new();
    }

    public enum Mode { None, Active }
    public class DerivedSchema : JsonSchema
    {
        [JsonPropertyName("custom_name")]
        public string Label { get; set; } = "kept";
        [JsonIgnore]
        public string Hidden => "hidden";
        [JsonConverter(typeof(JsonStringEnumConverter<Mode>))]
        public Mode Mode { get; set; } = Mode.Active;
        [JsonInclude]
        public string Included { get; private set; } = "included";
        public JsonSchema? Child { get; set; }
        [JsonConverter(typeof(SchemaMarkerConverter))]
        public JsonSchema ConvertedChild { get; set; } = new JsonSchema();
    }
    public class IgnoredHolder
    {
        [JsonPropertyName("secret")]
        public int Secret { get; set; } = 7;
        [JsonPropertyName("kept")]
        public int Kept { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object>? ExtensionData { get; set; }
    }
    public class InheritedHolder : IgnoredHolder { }
    public class NestedHolder
    {
        public IgnoredHolder Single { get; set; } = new();
        public List<IgnoredHolder> List { get; set; } = [];
        public Dictionary<string, IgnoredHolder> Dictionary { get; set; } = [];
    }
    [JsonConverter(typeof(TypeMarkerConverter))]
    public class Marker { public string? Value { get; set; } }
    public class TypeMarkerConverter : JsonConverter<Marker>
    {
        protected virtual string Text => "type";
        public override Marker? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new() { Value = reader.GetString() };
        public override void Write(Utf8JsonWriter writer, Marker value, JsonSerializerOptions options) => writer.WriteStringValue(Text);
    }
    public class OptionsMarkerConverter : TypeMarkerConverter { protected override string Text => "options"; }
    public class PropertyMarkerConverter : TypeMarkerConverter { protected override string Text => "property"; }
    public class NullMarkerConverter : TypeMarkerConverter
    {
        public override void Write(Utf8JsonWriter writer, Marker value, JsonSerializerOptions options) => writer.WriteNullValue();
    }
    public class MarkerFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Marker);
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new PropertyMarkerConverter();
    }
    public class SchemaMarkerConverter : JsonConverter<JsonSchema>
    {
        public override JsonSchema? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new();
        public override void Write(Utf8JsonWriter writer, JsonSchema value, JsonSerializerOptions options) => writer.WriteStringValue("property");
    }
    public class ConvertedHolder
    {
        [JsonConverter(typeof(JsonStringEnumConverter<Mode>))]
        public Mode Mode { get; set; } = Mode.Active;
        [JsonConverter(typeof(PropertyMarkerConverter))]
        public Marker Property { get; set; } = new();
        public Marker Options { get; set; } = new();
        [JsonConverter(typeof(MarkerFactory))]
        public Marker Factory { get; set; } = new();
        [JsonConverter(typeof(PropertyMarkerConverter))]
        public Marker Renamed { get; set; } = new();
        [JsonConverter(typeof(NullMarkerConverter))]
        public Marker Null { get; set; } = new();
        [JsonConverter(typeof(MappingConverter))]
        public IDictionary<string, JsonSchema> Mapping { get; set; } = new Dictionary<string, JsonSchema> { ["pet"] = new() };
    }
    public class MappingConverter : JsonConverter<IDictionary<string, JsonSchema>>
    {
        public override IDictionary<string, JsonSchema> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.EnumerateObject().ToDictionary(property => property.Name, property =>
            {
                var schema = new JsonSchema();
                ((IJsonReferenceBase)schema).ReferencePath = property.Value.GetString();
                return schema;
            });
        }
        public override void Write(Utf8JsonWriter writer, IDictionary<string, JsonSchema> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var pair in value) writer.WriteString(pair.Key, ((IJsonReferenceBase)pair.Value).ReferencePath);
            writer.WriteEndObject();
        }
    }
}
