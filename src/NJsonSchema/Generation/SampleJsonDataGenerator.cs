//-----------------------------------------------------------------------
// <copyright file="SampleJsonSchemaGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace NJsonSchema.Generation
{
    /// <summary>Generates a sample JSON object from a JSON Schema.</summary>
    public class SampleJsonDataGenerator
    {
        /// <summary>
        /// Creates a non-null JsonValue representing JSON null, equivalent to the old Newtonsoft JValue(null).
        /// System.Text.Json.Nodes does not provide a public API to create a non-null JsonNode for null values
        /// (JsonValue.Create returns null for null-kind JsonElements). We work around this by wrapping a
        /// sentinel value with a custom JsonConverter that writes JSON null.
        /// </summary>
        private static JsonValue CreateJsonNullValue()
        {
            return JsonValue.Create(new JsonNullSentinel(), JsonNullSentinel.NodeOptions)!;
        }

        [JsonConverter(typeof(JsonNullSentinelConverter))]
        private readonly struct JsonNullSentinel
        {
            internal static readonly JsonNodeOptions NodeOptions = new() { PropertyNameCaseInsensitive = false };
        }

        private sealed class JsonNullSentinelConverter : JsonConverter<JsonNullSentinel>
        {
            public override JsonNullSentinel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                reader.Read();
                return default;
            }

            public override void Write(Utf8JsonWriter writer, JsonNullSentinel value, JsonSerializerOptions options)
            {
                writer.WriteNullValue();
            }
        }

        private readonly SampleJsonDataGeneratorSettings _settings;

        /// <summary>
        /// Initializes a new instance of <see cref="SampleJsonDataGenerator"/> class with default settings..
        /// </summary>
        public SampleJsonDataGenerator()
        {
            _settings = new SampleJsonDataGeneratorSettings();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SampleJsonDataGenerator"/> class.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        public SampleJsonDataGenerator(SampleJsonDataGeneratorSettings settings)
        {
            _settings = settings;
        }

        /// <summary>Generates a sample JSON object from a JSON Schema.</summary>
        /// <param name="schema">The JSON Schema.</param>
        /// <returns>The JSON node.</returns>
        public JsonNode? Generate(JsonSchema schema)
        {
            var stack = new Stack<JsonSchema>();
            stack.Push(schema);
            return Generate(schema, stack);
        }

        private JsonNode? Generate(JsonSchema schema, Stack<JsonSchema> schemaStack)
        {
            var property = schema as JsonSchemaProperty;
            schema = schema.ActualSchema;

            // Handle oneOf/anyOf schemas (e.g. nullable references expressed as oneOf: [{ type: null }, { $ref: ... }])
            // by selecting the first non-null sub-schema and generating from it.
            if (schema.Type == JsonObjectType.None &&
                !schema.ActualProperties.Any() &&
                schema.AllOf.Count == 0)
            {
                var subSchemas = schema.OneOf.Concat(schema.AnyOf);
                var nonNullSubSchema = subSchemas.FirstOrDefault(s =>
                    s.ActualSchema.Type != JsonObjectType.Null &&
                    (s.HasReference || s.ActualSchema.Type != JsonObjectType.None || s.ActualSchema.ActualProperties.Any()));

                if (nonNullSubSchema != null)
                {
                    schema = nonNullSubSchema.ActualSchema;
                }
            }

            try
            {
                schemaStack.Push(schema);
                if (schemaStack.Count(s => s == schema) > _settings.MaxRecursionLevel)
                {
                    return CreateJsonNullValue();
                }

                if (schema.Type.IsObject() || GetPropertiesToGenerate(schema.AllOf).Any())
                {
                    var schemas = new[] { schema }.Concat(schema.AllOf.Select(x => x.ActualSchema));
                    var properties = GetPropertiesToGenerate(schemas);

                    var obj = new JsonObject();
                    foreach (var p in properties)
                    {
                        obj[p.Key] = Generate(p.Value, schemaStack);
                    }

                    return obj;
                }
                else if (schema.Default != null)
                {
                    return JsonSerializer.SerializeToNode(schema.Default);
                }
                else if (schema.Type.IsArray())
                {
                    if (schema.Item != null)
                    {
                        var array = new JsonArray();

                        var item = Generate(schema.Item, schemaStack);
                        if (item != null)
                        {
                            array.Add(item);
                        }

                        return array;
                    }
                    else if (schema.Items.Count > 0)
                    {
                        var array = new JsonArray();
                        foreach (var item in schema.Items)
                        {
                            array.Add(Generate(item, schemaStack));
                        }

                        return array;
                    }
                }
                else
                {
                    if (schema.IsEnumeration)
                    {
                        return JsonSerializer.SerializeToNode(schema.Enumeration.First()!);
                    }
                    else if (schema.Type.IsInteger())
                    {
                        return SampleJsonDataGenerator.HandleIntegerType(schema);
                    }
                    else if (schema.Type.IsNumber())
                    {
                        return HandleNumberType(schema);
                    }
                    else if (schema.Type.IsString())
                    {
                        return HandleStringType(schema, property);
                    }
                    else if (schema.Type.IsBoolean())
                    {
                        return JsonValue.Create(false);
                    }
                }

                return null;
            }
            finally
            {
                schemaStack.Pop();
            }
        }

        private static JsonValue? HandleNumberType(JsonSchema schema)
        {
            if (schema.ExclusiveMinimumRaw?.Equals(true) == true && schema.Minimum != null)
            {
                return JsonValue.Create((double)(schema.Minimum.Value + 0.1m));
            }
            else if (schema.ExclusiveMinimum != null)
            {
                return JsonValue.Create((double)schema.ExclusiveMinimum.Value);
            }
            else if (schema.Minimum.HasValue)
            {
                return JsonValue.Create((double)schema.Minimum.Value);
            }
            return JsonValue.Create(0.0);
        }

        private static JsonValue? HandleIntegerType(JsonSchema schema)
        {
            long value;
            if (schema.ExclusiveMinimumRaw != null)
            {
                value = Convert.ToInt64(schema.ExclusiveMinimumRaw, CultureInfo.InvariantCulture);
            }
            else if (schema.ExclusiveMinimum != null)
            {
                value = Convert.ToInt64(schema.ExclusiveMinimum, CultureInfo.InvariantCulture);
            }
            else if (schema.Minimum.HasValue)
            {
                value = Convert.ToInt64(schema.Minimum, CultureInfo.InvariantCulture);
            }
            else
            {
                return JsonValue.Create(0);
            }

            if (value is >= int.MinValue and <= int.MaxValue)
            {
                return JsonValue.Create((int)value);
            }

            return JsonValue.Create(value);
        }

        private static JsonValue? HandleStringType(JsonSchema schema, JsonSchemaProperty? property)
        {
            if (schema.Format == JsonFormatStrings.Date)
            {
                return JsonValue.Create(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }
            else if (schema.Format == JsonFormatStrings.DateTime)
            {
                return JsonValue.Create(DateTimeOffset.UtcNow.ToString("o"));
            }
#pragma warning disable CS0618 // Type or member is obsolete
            else if (schema.Format == JsonFormatStrings.Guid || schema.Format == JsonFormatStrings.Uuid)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                return JsonValue.Create(Guid.NewGuid().ToString());
            }
            else if (property != null)
            {
                return JsonValue.Create(property.Name);
            }
            else
            {
                return JsonValue.Create("");
            }
        }

        private IEnumerable<KeyValuePair<string, JsonSchemaProperty>> GetPropertiesToGenerate(IEnumerable<JsonSchema> schemas)
        {
            return schemas.SelectMany(GetPropertiesToGenerate);
        }

        private IEnumerable<KeyValuePair<string, JsonSchemaProperty>> GetPropertiesToGenerate(JsonSchema schema)
        {
            if (_settings.GenerateOptionalProperties)
            {
                return schema.ActualProperties;
            }

            var required = schema.RequiredProperties;
            return schema.ActualProperties.Where(x => required.Contains(x.Key));
        }
    }
}
