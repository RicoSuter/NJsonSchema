using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NJsonSchema.Infrastructure;

/// <summary>Compares JSON values structurally, with exact numeric identity.</summary>
internal sealed class JsonValueComparer : IEqualityComparer<JsonNode?>
{
    public static JsonValueComparer Instance { get; } = new();

    /// <summary>Adapts public enum values without modifying user-owned nodes.</summary>
    public static JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        JsonNode node => node,
        JsonElement element => JsonNode.Parse(element.GetRawText()),
        _ => JsonSerializer.SerializeToNode(value)
    };

    public bool Equals(JsonNode? left, JsonNode? right)
    {
        left = JsonValueNormalization.Normalize(left);
        right = JsonValueNormalization.Normalize(right);
        if (left == null || right == null)
        {
            return left == null && right == null;
        }

        var kind = left.GetValueKind();
        if (kind != right.GetValueKind())
        {
            return false;
        }

        switch (kind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.True:
            case JsonValueKind.False:
                return true;
            case JsonValueKind.String:
                return StringComparer.Ordinal.Equals(left.GetValue<string>(), right.GetValue<string>());
            case JsonValueKind.Number:
                JsonNumber.TryCreate((JsonValue)left, out var leftNumber);
                JsonNumber.TryCreate((JsonValue)right, out var rightNumber);
                return leftNumber!.Equals(rightNumber);
            case JsonValueKind.Array:
                var leftArray = left.AsArray();
                var rightArray = right.AsArray();
                return leftArray.Count == rightArray.Count && leftArray.SequenceEqual(rightArray, this);
            case JsonValueKind.Object:
                var leftObject = left.AsObject();
                var rightObject = right.AsObject();
                if (leftObject.Count != rightObject.Count)
                {
                    return false;
                }

                // JsonObject may use case-insensitive lookup; JSON member names are ordinal.
                var rightProperties = rightObject.ToDictionary(property => property.Key, property => property.Value, StringComparer.Ordinal);
                return leftObject.All(property => rightProperties.TryGetValue(property.Key, out var value) && Equals(property.Value, value));
            default:
                return false;
        }
    }

    public int GetHashCode(JsonNode? value)
    {
        value = JsonValueNormalization.Normalize(value);
        if (value == null)
        {
            return (int)JsonValueKind.Null;
        }

        var kind = value.GetValueKind();
        unchecked
        {
            var hash = (int)kind;
            switch (kind)
            {
                case JsonValueKind.String:
                    return hash * 397 ^ StringComparer.Ordinal.GetHashCode(value.GetValue<string>());
                case JsonValueKind.Number:
                    JsonNumber.TryCreate((JsonValue)value, out var number);
                    return hash * 397 ^ number!.GetHashCode();
                case JsonValueKind.Array:
                    foreach (var item in value.AsArray())
                    {
                        hash = hash * 397 ^ GetHashCode(item);
                    }
                    return hash;
                case JsonValueKind.Object:
                    // Addition makes member ordering irrelevant while retaining name/value pairs.
                    foreach (var property in value.AsObject())
                    {
                        hash += StringComparer.Ordinal.GetHashCode(property.Key) * 397 ^ GetHashCode(property.Value);
                    }
                    return hash;
                default:
                    return hash;
            }
        }
    }
}
