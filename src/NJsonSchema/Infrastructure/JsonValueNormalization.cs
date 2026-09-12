using System.Text.Json;
using System.Text.Json.Nodes;

namespace NJsonSchema.Infrastructure;

/// <summary>Adapts CLR-backed values to their JSON shape without changing caller-owned nodes.</summary>
internal static class JsonValueNormalization
{
    // A JsonValue's serialized kind need not match its CLR backing type: for example,
    // chars serialize as strings and customized dictionary values serialize as objects.
    /// <summary>Normalizes only incompatible JsonValue representations; never mutates or clones a whole document.</summary>
    public static JsonNode? Normalize(JsonNode? node)
    {
        if (node is JsonValue value)
        {
            var kind = value.GetValueKind();
            if (kind == JsonValueKind.Null)
            {
                return null;
            }

            // Parsed values already expose their wire representation. CLR-backed strings
            // can carry a custom converter even when TryGetValue<string> succeeds.
            if (kind == JsonValueKind.String && !value.TryGetValue<JsonElement>(out _))
            {
                var serialized = JsonNode.Parse(value.ToJsonString());
                if (value.TryGetValue<string>(out var text) &&
                    serialized is JsonValue serializedValue && serializedValue.TryGetValue<string>(out var serializedText) &&
                    text == serializedText)
                {
                    return node;
                }

                return serialized;
            }

            if (kind == JsonValueKind.Object || kind == JsonValueKind.Array)
            {
                return JsonNode.Parse(value.ToJsonString());
            }
        }

        return node;
    }
}
