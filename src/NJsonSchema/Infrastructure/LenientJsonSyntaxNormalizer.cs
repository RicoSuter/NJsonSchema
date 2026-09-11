using System.Text;
using System.Text.Json;

namespace NJsonSchema.Infrastructure
{
    // Syntax recovery only: schema keywords and literal values have identical lexical rules.
    internal static class LenientJsonSyntaxNormalizer
    {
        internal static string Normalize(string json)
        {
            var output = new StringBuilder(json.Length);
            var containers = new Stack<char>();
            var expectsProperty = false;
            for (var index = 0; index < json.Length; index++)
            {
                var character = json[index];
                if (character == '"' || character == '\'')
                {
                    var quote = character;
                    var token = new StringBuilder();
                    var start = index;
                    var closed = false;
                    while (++index < json.Length)
                    {
                        character = json[index];
                        if (character == quote)
                        {
                            closed = true;
                            break;
                        }
                        if (character == '\\')
                        {
                            if (++index == json.Length) throw new JsonException("Incomplete string escape.");
                            var escaped = json[index];
                            if (quote == '\'' && escaped == '\'') token.Append('\'');
                            else token.Append('\\').Append(escaped);
                        }
                        else if (quote == '\'' && character == '"') token.Append("\\\"");
                        else token.Append(character);
                    }
                    if (!closed) throw new JsonException("Unterminated string.");
                    if (quote == '"') output.Append(json, start, index - start + 1);
                    else output.Append(JsonSerializer.Serialize(JsonSerializer.Deserialize<string>("\"" + token + "\"")));
                    continue;
                }

                if (character == '/' && index + 1 < json.Length && (json[index + 1] == '/' || json[index + 1] == '*'))
                {
                    var start = index;
                    if (json[++index] == '/')
                    {
                        while (index + 1 < json.Length && json[index + 1] != '\r' && json[index + 1] != '\n') index++;
                    }
                    else
                    {
                        while (index + 1 < json.Length && !(json[index] == '*' && json[index + 1] == '/')) index++;
                        if (index + 1 == json.Length) throw new JsonException("Unterminated comment.");
                        index++;
                    }
                    output.Append(json, start, index - start + 1);
                    continue;
                }

                if (expectsProperty && IsIdentifierStart(character))
                {
                    var start = index;
                    while (index + 1 < json.Length && (IsIdentifierStart(json[index + 1]) || char.IsDigit(json[index + 1]))) index++;
                    output.Append('"').Append(json, start, index - start + 1).Append('"');
                    continue;
                }

                switch (character)
                {
                    case '{':
                    case '[':
                        containers.Push(character);
                        expectsProperty = character == '{';
                        break;
                    case '}':
                    case ']':
                        if (containers.Count == 0 || containers.Pop() != (character == '}' ? '{' : '['))
                            throw new JsonException("Mismatched JSON container.");
                        expectsProperty = false;
                        break;
                    case ':': expectsProperty = false; break;
                    case ',': expectsProperty = containers.Count > 0 && containers.Peek() == '{'; break;
                }
                output.Append(character == '\u00a0' ? ' ' : character);
            }
            return output.ToString();
        }

        private static bool IsIdentifierStart(char character) =>
            character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_' or '$';
    }
}
