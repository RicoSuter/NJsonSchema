#nullable enable
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NJsonSchema.Infrastructure;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Serialization;

public class FinalMigrationRegressionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void F1_Property_converter_delegation_stays_property_scoped(bool factory)
    {
        // Arrange
        object holder = factory ? new FactoryHolder() : new DirectHolder();
        var converter = new SchemaSerializationConverter();
        converter.IgnoreProperty(holder.GetType());

        // Act
        var json = JsonSchemaSerialization.ToJson(holder, SchemaType.JsonSchema, converter, false);

        // Assert
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(holder, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })), JsonNode.Parse(json)));
    }

    [Fact]
    public void F2_Explicit_null_contracts_override_default_omission()
    {
        // Arrange
        var holder = new NullHolder();
        var converter = new SchemaSerializationConverter();
        converter.IgnoreProperty(typeof(NullHolder));
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        // Act
        var json = JsonSchemaSerialization.ToJson(holder, SchemaType.JsonSchema, converter, false);

        // Assert
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(holder, options)), JsonNode.Parse(json)), json);
    }

    [Theory]
    [InlineData("isBinary")]
    [InlineData("actualProperties")]
    [InlineData("discriminatorObject")]
    [InlineData("documentPath")]
    [InlineData("reference")]
    [InlineData("parent")]
    [InlineData("name")]
    public async Task F3_Core_metadata_names_remain_extension_data(string name)
    {
        // Arrange
        var json = "{\"" + name + "\":{\"a\":1},\"properties\":{\"nested\":{\"" + name + "\":{\"a\":2}}}}";

        // Act
        var schema = await JsonSchema.FromJsonAsync(json);
        var output = JsonNode.Parse(schema.ToJson(false))!;

        // Assert
        Assert.Equal(1, output[name]!["a"]!.GetValue<int>());
        Assert.Equal(2, output["properties"]!["nested"]![name]!["a"]!.GetValue<int>());
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.Swagger2)]
    [InlineData(SchemaType.OpenApi3)]
    public void F4_Public_node_overloads_preserve_identity_and_no_source_locations(SchemaType dialect)
    {
        // Arrange
        JsonNode?[] nodes = [JsonValue.Create(1), new JsonObject(), new JsonArray(), null, JsonValue.Create("ordinary"), JsonNode.Parse("\"parsed\"")];

        // Act / Assert
        foreach (var node in nodes)
        {
            var schema = new JsonSchema { Type = JsonObjectType.Boolean };
            foreach (var errors in new[] { schema.Validate(node), schema.Validate(node, dialect) })
            {
                var error = Assert.Single(errors);
                Assert.Same(node, error.Token);
                Assert.False(error.HasLineInfo);
            }
        }
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema, false)]
    [InlineData(SchemaType.Swagger2, false)]
    [InlineData(SchemaType.OpenApi3, false)]
    [InlineData(SchemaType.JsonSchema, true)]
    [InlineData(SchemaType.Swagger2, true)]
    [InlineData(SchemaType.OpenApi3, true)]
    public async Task F5_Quoted_boolean_unions_control_validation(SchemaType dialect, bool allow)
    {
        // Arrange
        var literal = allow ? "true" : "false";
        var converter = JsonSchema.CreateSchemaSerializationConverter(dialect);
        async Task<JsonSchema> Read(string json) => await JsonSchemaSerialization.FromJsonAsync<JsonSchema>(json, dialect, null,
            schema => new JsonReferenceResolver(new JsonSchemaAppender(schema, new DefaultTypeNameGenerator())), converter);

        // Act
        var objects = await Read("{\"type\":\"object\",\"additionalProperties\":\"" + literal + "\",\"default\":\"false\",\"example\":\"false\",\"x-literal\":\"false\"}");
        var arrays = await Read("{\"type\":\"array\",\"items\":[{\"type\":\"string\"}],\"additionalItems\":\"" + literal + "\"}");

        // Assert
        Assert.Equal(allow, objects.AllowAdditionalProperties);
        Assert.Equal(allow, arrays.AllowAdditionalItems);
        Assert.Equal(allow, objects.Validate("{\"extra\":1}", dialect).Count == 0);
        Assert.Equal(allow, arrays.Validate("[\"first\",2]", dialect).Count == 0);
        var output = JsonNode.Parse(JsonSchemaSerialization.ToJson(objects, dialect, converter, false))!;
        Assert.Equal("false", output["default"]!.GetValue<string>());
        Assert.Equal("false", output["example"]!.GetValue<string>());
        Assert.Equal("false", output["x-literal"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("additionalItems", "\"invalid\"")]
    [InlineData("additionalProperties", "\"invalid\"")]
    [InlineData("additionalItems", "42")]
    [InlineData("additionalProperties", "42")]
    public async Task F5_Invalid_union_scalars_are_rejected(string keyword, string value)
    {
        // Arrange
        var json = "{\"" + keyword + "\":" + value + "}";

        // Act
        var error = await Record.ExceptionAsync(() => JsonSchema.FromJsonAsync(json));

        // Assert
        Assert.NotNull(error);
    }

    [Fact]
    public void F6_Custom_string_uses_serialized_text_for_constraints_enum_and_uniqueness()
    {
        // Arrange
        var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
        options.Converters.Add(new ExpandedStringConverter());
        var node = JsonValue.Create("x", (JsonTypeInfo<string>)options.GetTypeInfo(typeof(string)))!;
        var enumeration = new JsonSchema();
        enumeration.Enumeration.Add("expanded");
        var schemas = new[] { new JsonSchema { Type = JsonObjectType.String, MinLength = 4 },
            new JsonSchema { Type = JsonObjectType.String, MaxLength = 4 },
            new JsonSchema { Type = JsonObjectType.String, Pattern = "^expanded$" },
            new JsonSchema { Type = JsonObjectType.String, Format = "expanded" }, enumeration };
        var parent = new JsonObject { ["value"] = node };
        var settings = new JsonSchemaValidatorSettings { FormatValidators = [new ExpandedFormatValidator()] };
        var validator = new JsonSchemaValidator(settings);

        // Act / Assert
        foreach (var schema in schemas)
        {
            var expected = schema.Validate(node.ToJsonString(), settings).Select(error => error.Kind).ToArray();
            Assert.Equal(expected, validator.Validate(node, schema).Select(error => error.Kind));
            Assert.Equal(expected, schema.Validate(node, settings).Select(error => error.Kind));
            var nestedSchema = new JsonSchema { Type = JsonObjectType.Object };
            nestedSchema.Properties["value"] = new JsonSchemaProperty { Reference = schema };
            Assert.Equal(nestedSchema.Validate(parent.ToJsonString(), settings).Select(error => error.Kind),
                nestedSchema.Validate(parent, settings).Select(error => error.Kind));
        }
        var array = new JsonArray(JsonValue.Create("x", (JsonTypeInfo<string>)options.GetTypeInfo(typeof(string))), JsonValue.Create("expanded"));
        Assert.NotEmpty(new JsonSchema { Type = JsonObjectType.Array, UniqueItems = true }.Validate(array));
        Assert.Same(node, parent["value"]);
        Assert.Equal("x", node.GetValue<string>());
    }

    [Fact]
    public async Task F3_Extension_reference_targets_and_user_ignores_remain_authoritative()
    {
        // Arrange
        var json = """{"documentPath":{"type":"string"},"properties":{"value":{"$ref":"#/documentPath"}}}""";

        // Act
        var schema = await JsonSchema.FromJsonAsync(json);
        var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);
        converter.IgnoreProperty(typeof(UserSchema), "actualProperties");
        var derived = await JsonSchemaSerialization.FromJsonAsync<UserSchema>(
            """{"isBinary":{"a":1},"documentPath":{"a":2},"actualProperties":{"a":3}}""",
            SchemaType.JsonSchema, null,
            root => new JsonReferenceResolver(new JsonSchemaAppender(root, new DefaultTypeNameGenerator())), converter);

        // Assert
        Assert.Equal(JsonObjectType.String, schema.Properties["value"].ActualSchema.Type);
        Assert.Equal("#/documentPath", JsonNode.Parse(schema.ToJson(false))!["properties"]!["value"]!["$ref"]!.GetValue<string>());
        Assert.True(derived.ExtensionData == null || derived.ExtensionData.Count == 0, string.Join(",", derived.ExtensionData?.Keys ?? []));
    }

    [Theory]
    [InlineData(JsonIgnoreCondition.Never)]
    [InlineData(JsonIgnoreCondition.WhenWritingNull)]
    [InlineData(JsonIgnoreCondition.WhenWritingDefault)]
    public void F2_Resolver_predicates_and_operation_defaults_match_STJ(JsonIgnoreCondition condition)
    {
        // Arrange
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info =>
        {
            foreach (var property in info.Properties)
            {
                if (property.Name == "Include") property.ShouldSerialize = (_, _) => true;
                if (property.Name == "Exclude") property.ShouldSerialize = (_, _) => false;
            }
        });
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = condition, TypeInfoResolver = resolver };
        var holder = new PredicateHolder();
        var expected = JsonSerializer.Serialize(holder, options);
        var filteredOptions = new JsonSerializerOptions(options);
        var converter = new SchemaSerializationConverter();
        converter.IgnoreProperty(typeof(PredicateHolder));
        filteredOptions.Converters.Add(converter);

        // Act
        var actual = JsonSerializer.Serialize(holder, filteredOptions);

        // Assert
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)), actual);
    }

    [Fact]
    public void F1_Nullable_property_factory_uses_underlying_converter_compatibly()
    {
        // Arrange
        var holder = new NullableHolder();
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        var converter = new SchemaSerializationConverter();
        converter.IgnoreProperty(typeof(NullableHolder));

        // Act
        var actual = JsonSchemaSerialization.ToJson(holder, SchemaType.JsonSchema, converter, false);

        // Assert
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(holder, options)), JsonNode.Parse(actual)), actual);
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.Swagger2)]
    [InlineData(SchemaType.OpenApi3)]
    public void F4_Public_node_overloads_forward_settings_and_dialect(SchemaType dialect)
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.String, Format = "email", IsNullableRaw = true };
        var settings = new JsonSchemaValidatorSettings { FormatValidators = [] };
        var validator = new JsonSchemaValidator(settings);
        var node = JsonValue.Create("not email")!;

        // Act / Assert
        Assert.Empty(schema.Validate(node, settings));
        Assert.Empty(schema.Validate(node, dialect, settings));
        Assert.Equal(validator.Validate((JsonNode?)null, schema, dialect).Select(error => error.Kind),
            schema.Validate((JsonNode?)null, dialect, settings).Select(error => error.Kind));
    }

    [Fact]
    public void F4_Nested_errors_point_to_caller_owned_children()
    {
        // Arrange
        var child = JsonValue.Create(42)!;
        var input = new JsonObject { ["items"] = new JsonArray(child) };
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["items"] = new JsonSchemaProperty { Type = JsonObjectType.Array,
            Item = new JsonSchema { Type = JsonObjectType.String } };

        // Act
        var error = Assert.Single(schema.Validate(input));

        // Assert
        Assert.False(error.HasLineInfo);
        Assert.Same(child, error.Token);
        Assert.Same(child, input["items"]![0]);
    }

    public class ExpandedFormatValidator : NJsonSchema.Validation.FormatValidators.IFormatValidator
    {
        public string Format => "expanded";
        public ValidationErrorKind ValidationErrorKind => ValidationErrorKind.StringExpected;
        public bool IsValid(string value, JsonValueKind tokenType) => value == "expanded";
    }

    public class UserSchema : JsonSchema
    {
        [JsonIgnore]
        public new object? IsBinary { get; set; }
        [JsonIgnore]
        public new string? DocumentPath { get; set; }
    }
    public class PredicateHolder
    {
        public object? Include { get; set; }
        public object? Exclude { get; set; }
        public object? Default { get; set; }
        public int Zero { get; set; }
    }
    public class NullableHolder
    {
        [JsonConverter(typeof(NullableFactory))]
        public int? Number { get; set; } = 7;
        [JsonConverter(typeof(NullableFactory))]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int? Null { get; set; }
    }
    public class NullableFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(int);
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Assert.Equal(typeof(int), typeToConvert);
            return new IntegerConverter();
        }
    }
    public class IntegerConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();
        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public class Value
    {
        public int A { get; set; } = 1;
        public Value? Child { get; set; }
    }
    public class DirectHolder
    {
        [JsonConverter(typeof(DelegatingConverter))]
        public Value Data { get; set; } = new() { Child = new() };
        public Value Sibling { get; set; } = new();
    }
    public class FactoryHolder
    {
        [JsonConverter(typeof(DelegatingFactory))]
        public Value Data { get; set; } = new() { Child = new() };
        public Value Sibling { get; set; } = new();
    }
    public class DelegatingFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Value);
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new DelegatingConverter();
    }
    public class DelegatingConverter : JsonConverter<Value>
    {
        private int depth;
        public override Value Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();
        public override void Write(Utf8JsonWriter writer, Value value, JsonSerializerOptions options)
        {
            if (++depth > 2) { depth--; throw new InvalidOperationException("Property converter recursively reapplied."); }
            try { JsonSerializer.Serialize(writer, value, options); }
            finally { depth--; }
        }
    }
    public class NullHolder
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [JsonConverter(typeof(NullConverter))]
        public Value? Converted { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [JsonConverter(typeof(NullFactory))]
        public Value? Factory { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [JsonConverter(typeof(DelegatingConverter))]
        public Value? Unhandled { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public object? Plain { get; set; }
        public object? Omitted { get; set; }
    }
    public class NullFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Value);
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new NullConverter();
    }
    public class NullConverter : JsonConverter<Value>
    {
        public override bool HandleNull => true;
        public override Value? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => null;
        public override void Write(Utf8JsonWriter writer, Value value, JsonSerializerOptions options) => writer.WriteStringValue(value == null ? "nil" : "value");
    }
    public class ExpandedStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetString()!;
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue("expanded");
    }
}
