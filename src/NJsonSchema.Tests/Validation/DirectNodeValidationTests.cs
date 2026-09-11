using System.Text.Json;
using System.Text.Json.Serialization;
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
    [JsonConverter(typeof(SerializedNullConverter))]
    public sealed class SerializedNull;

    public sealed class SerializedNullConverter : JsonConverter<SerializedNull>
    {
        public override SerializedNull Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new();

        public override void Write(Utf8JsonWriter writer, SerializedNull value, JsonSerializerOptions options) => writer.WriteNullValue();
    }
}
