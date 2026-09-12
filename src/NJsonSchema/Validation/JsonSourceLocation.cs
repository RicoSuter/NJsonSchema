//-----------------------------------------------------------------------
// <copyright file="JsonSourceLocation.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
//-----------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;

namespace NJsonSchema.Validation
{
    /// <summary>Associates source coordinates with tree identity, independently of display paths.</summary>
    internal static class JsonSourceLocation
    {
        // Non-null values use JsonNode reference identity. Null values use (owner, name, -1)
        // for properties or (owner, null, index) for array items. Property-name coordinates
        // use (owner, name, -2), keeping names distinct from value locations and indices.
        private static readonly object RootNull = new();

        internal static void SetNullIdentity(IEnumerable<ValidationError> errors, object identity)
        {
            foreach (var error in errors)
            {
                SetNullIdentity(error, identity);
            }
        }

        // Bind at the tree edge after the virtual validation call returns. Nested edges
        // have already bound their nulls; preserve those identities when unwinding.
        internal static void SetNullIdentity(ValidationError error, object identity)
        {
            if (error.Token == null && error.SourceIdentity == null)
            {
                error.SourceIdentity = identity;
            }
            foreach (var children in Children(error))
            {
                SetNullIdentity(children, identity);
            }
        }

        internal static void Apply(string json, JsonNode? root, IEnumerable<ValidationError> errors)
        {
            var locations = new Dictionary<object, (int Line, int Position)>();
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(bytes, new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
            var containers = new Stack<(JsonNode Node, int Index)>();
            string? propertyName = null;
            var offset = 0;
            var line = 1;
            var position = 0;

            // Reader offsets increase monotonically, so each source byte is counted once.
            // Count UTF-16 characters (including surrogate pairs), and CRLF as one newline.
            (int Line, int Position) PositionAt(long end)
            {
                while (offset < end)
                {
                    var value = bytes[offset++];
                    if (value == '\r')
                    {
                        line++;
                        position = 0;
                    }
                    else if (value == '\n')
                    {
                        if (offset < 2 || bytes[offset - 2] != '\r')
                        {
                            line++;
                        }
                        position = 0;
                    }
                    else if ((value & 0xc0) != 0x80)
                    {
                        position += value >= 0xf0 ? 2 : 1;
                    }
                }
                return (line, position);
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    propertyName = reader.GetString()!;
                    // BytesConsumed includes the colon, matching property-token coordinates.
                    locations[(containers.Peek().Node, propertyName, -2)] = PositionAt(reader.BytesConsumed);
                    continue;
                }
                if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
                {
                    containers.Pop();
                    continue;
                }

                JsonNode? node;
                object identity;
                if (containers.Count == 0)
                {
                    node = root;
                    identity = (object?)node ?? RootNull;
                }
                else
                {
                    var container = containers.Pop();
                    if (container.Node is JsonObject obj)
                    {
                        node = obj[propertyName!];
                        identity = (object?)node ?? (container.Node, propertyName, -1);
                    }
                    else
                    {
                        node = ((JsonArray)container.Node)[container.Index];
                        identity = (object?)node ?? (container.Node, (string?)null, container.Index);
                        container.Index++;
                    }
                    containers.Push(container);
                }
                locations[identity] = PositionAt(reader.BytesConsumed);
                propertyName = null;
                if (node is JsonObject or JsonArray)
                {
                    containers.Push((node, 0));
                }
            }

            ApplyErrors(errors, locations);
        }

        private static void ApplyErrors(IEnumerable<ValidationError> errors, Dictionary<object, (int Line, int Position)> locations)
        {
            foreach (var error in errors)
            {
                var identity = error.Token is JsonPropertyToken property
                    ? property.SourceIdentity
                    : error.Token ?? error.SourceIdentity ?? RootNull;
                if (locations.TryGetValue(identity, out var location))
                {
                    error.HasLineInfo = true;
                    error.LineNumber = location.Line;
                    error.LinePosition = location.Position;
                }
                foreach (var children in Children(error))
                {
                    ApplyErrors(children, locations);
                }
            }
        }

        private static IEnumerable<ICollection<ValidationError>> Children(ValidationError error)
        {
            if (error is ChildSchemaValidationError child)
            {
                return child.Errors.Values;
            }
            if (error is MultiTypeValidationError multi)
            {
                return multi.Errors.Values;
            }
            return [];
        }
    }
}
