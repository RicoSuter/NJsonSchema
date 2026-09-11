using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Nodes;
using NJsonSchema.Generation;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation;

public class DirectNodeValidationTests
{
    [Fact]
    public void SerializedStringNodes_EnforceTypesFormatsAndLengthsWithoutMutation()
    {
        // Arrange
        var nodes = new JsonNode[] { JsonValue.Create(Guid.Empty)!, JsonValue.Create(new DateTime(2020, 1, 1))!,
            JsonValue.Create(TimeSpan.FromMinutes(1))!, JsonValue.Create(new Uri("https://example.org"))!, JsonValue.Create('x')! };
        var validator = new JsonSchemaValidator();

        // Act / Assert
        foreach (var node in nodes)
        {
            foreach (var schema in new[] { new JsonSchema { Type = JsonObjectType.String },
                new JsonSchema { Type = JsonObjectType.String, MaxLength = 0 },
                new JsonSchema { Type = JsonObjectType.String, Format = "email" } })
            {
                Assert.Equal(validator.Validate(node.ToJsonString(), schema).Select(error => error.Kind),
                    validator.Validate(node, schema).Select(error => error.Kind));
                var parent = new JsonObject { ["value"] = node.DeepClone() };
                var original = parent["value"];
                var objectSchema = new JsonSchema { Type = JsonObjectType.Object };
                objectSchema.Properties["value"] = new JsonSchemaProperty { Type = schema.Type, MaxLength = schema.MaxLength, Format = schema.Format };
                Assert.Equal(validator.Validate(parent.ToJsonString(), objectSchema).Select(error => error.Kind),
                    validator.Validate(parent, objectSchema).Select(error => error.Kind));
                Assert.Same(original, parent["value"]);
            }
        }
    }

    [Fact]
    public void CustomizedShapes_MatchParsedValidationAtRootAndNested()
    {
        // Arrange
        var nodes = new JsonNode[] { JsonValue.Create(new Dictionary<string, int> { ["a"] = 1 })!,
            JsonValue.Create(new[] { 1, 2 })!, JsonValue.Create(new SerializedNull())! };
        var validator = new JsonSchemaValidator();

        // Act / Assert
        foreach (var node in nodes)
        {
            foreach (var type in new[] { JsonObjectType.Object, JsonObjectType.Array, JsonObjectType.Null, JsonObjectType.String })
            {
                var schema = new JsonSchema { Type = type, MinItems = 3 };
                Assert.Equal(validator.Validate(node.ToJsonString(), schema).Select(error => error.Kind),
                    validator.Validate(node, schema).Select(error => error.Kind));
                var array = new JsonArray(node.DeepClone());
                var arraySchema = new JsonSchema { Type = JsonObjectType.Array, Item = schema };
                Assert.Equal(validator.Validate(array.ToJsonString(), arraySchema).Select(error => error.Kind),
                    validator.Validate(array, arraySchema).Select(error => error.Kind));
            }
        }
    }
    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("1")]
    [InlineData("1.5")]
    [InlineData("9007199254740993")]
    [InlineData("1.00000000000000001")]
    public void CustomizedScalars_MatchParsedValidationWithoutMutation(string json)
    {
        // Arrange
        var node = JsonValue.Create(new SerializedScalar(json))!;
        AssertScalarParity(node);
    }

    [Fact]
    public void EnumBackedNumber_MatchesParsedValidationWithoutMutation()
    {
        // Arrange
        var node = JsonValue.Create(NumericChoice.Second)!;
        AssertScalarParity(node);
    }

    [Fact]
    public void CustomizedPrimitiveNumber_UsesSerializedValueForBoundsAndIntegrality()
    {
        // Arrange
        var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
        options.Converters.Add(new FractionalIntegerConverter());
        var node = JsonValue.Create(42, (JsonTypeInfo<int>)options.GetTypeInfo(typeof(int)))!;

        // Act / Assert
        AssertScalarParity(node);
    }

    public sealed class FractionalIntegerConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) => writer.WriteNumberValue(1.5M);
    }

    private static void AssertScalarParity(JsonNode node)
    {
        // Arrange
        var validator = new JsonSchemaValidator();
        var enumSchema = new JsonSchema();
        enumSchema.Enumeration.Add(true);
        enumSchema.Enumeration.Add(1);
        enumSchema.Enumeration.Add(9007199254740993L);
        var schemas = new[] { new JsonSchema { Type = JsonObjectType.Boolean },
            new JsonSchema { Type = JsonObjectType.Integer }, new JsonSchema { Type = JsonObjectType.Number },
            new JsonSchema { Minimum = 5 }, new JsonSchema { Maximum = 0 },
            new JsonSchema { MultipleOf = 2 }, enumSchema };
        var originalJson = node.ToJsonString();
        var parent = new JsonObject { ["value"] = node };
        var array = new JsonArray(parent);

        foreach (var schema in schemas)
        {
            var objectSchema = new JsonSchema { Type = JsonObjectType.Object };
            objectSchema.Properties["value"] = new JsonSchemaProperty { Reference = schema };
            var arraySchema = new JsonSchema { Type = JsonObjectType.Array, Item = objectSchema };

            // Act
            var expected = validator.Validate(originalJson, schema).Select(error => error.Kind).ToArray();
            var actual = validator.Validate(node, schema).Select(error => error.Kind).ToArray();
            var expectedNested = validator.Validate(array.ToJsonString(), arraySchema).Select(error => error.Kind).ToArray();
            var actualNested = validator.Validate(array, arraySchema).Select(error => error.Kind).ToArray();

            // Assert
            Assert.Equal(expected, actual);
            Assert.Equal(expectedNested, actualNested);
            Assert.Same(node, parent["value"]);
            Assert.Same(parent, node.Parent);
            Assert.Equal(originalJson, node.ToJsonString());
        }
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1e+")]
    public void CustomizedScalar_WithInvalidNumericJson_IsRejected(string json)
    {
        // Arrange
        var node = JsonValue.Create(new SerializedScalar(json))!;
        var schema = new JsonSchema { Type = JsonObjectType.Number, Maximum = 5 };

        // Act
        var exception = Record.Exception(() => new JsonSchemaValidator().Validate(node, schema));

        // Assert
        Assert.IsAssignableFrom<JsonException>(exception);
    }

    public enum NumericChoice { First, Second }

    [JsonConverter(typeof(SerializedScalarConverter))]
    public sealed record SerializedScalar(string Json);

    public sealed class SerializedScalarConverter : JsonConverter<SerializedScalar>
    {
        public override SerializedScalar Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, SerializedScalar value, JsonSerializerOptions options) => writer.WriteRawValue(value.Json);
    }

    [JsonConverter(typeof(SerializedNullConverter))]
    public sealed class SerializedNull;

    public sealed class SerializedNullConverter : JsonConverter<SerializedNull>
    {
        public override SerializedNull Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new();

        public override void Write(Utf8JsonWriter writer, SerializedNull value, JsonSerializerOptions options) => writer.WriteNullValue();
    }
}
