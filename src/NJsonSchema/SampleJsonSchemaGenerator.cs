//-----------------------------------------------------------------------
// <copyright file="SampleJsonSchemaGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace NJsonSchema
{
    /// <summary>Generates a JSON Schema from sample JSON data.</summary>
    public class SampleJsonSchemaGenerator
    {
        private readonly SampleJsonSchemaGeneratorSettings _settings;

        /// <summary>
        /// Default constructor
        /// </summary>
        public SampleJsonSchemaGenerator()
        {
            _settings = new SampleJsonSchemaGeneratorSettings();
        }

        /// <summary>
        /// Constructor with settings
        /// </summary>
        /// <param name="settings"></param>
        public SampleJsonSchemaGenerator(SampleJsonSchemaGeneratorSettings settings)
        {
            _settings = settings;
        }

        /// <summary>Generates the JSON Schema for the given JSON data.</summary>
        /// <param name="json">The JSON data.</param>
        /// <returns>The JSON Schema.</returns>
        public JsonSchema Generate(string json)
        {
            var node = ParseLenientJson(json);
            var schema = new JsonSchema();
            Generate(node, schema, schema, "Anonymous");
            return schema;
        }

        private static JsonNode? ParseLenientJson(string json)
        {
            var documentOptions = new System.Text.Json.JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
            };

            try
            {
                return JsonNode.Parse(json, documentOptions: documentOptions);
            }
            catch (System.Text.Json.JsonException)
            {
                var fixedJson = Infrastructure.JsonSchemaSerialization.FixLenientJson(json);
                return JsonNode.Parse(fixedJson, documentOptions: documentOptions);
            }
        }

        /// <summary>Generates the JSON Schema for the given JSON data.</summary>
        /// <param name="stream">The JSON data stream.</param>
        /// <returns>The JSON Schema.</returns>
        public JsonSchema Generate(Stream stream)
        {
            var node = JsonNode.Parse(stream);

            var schema = new JsonSchema();
            Generate(node, schema, schema, "Anonymous");
            return schema;
        }

        private void Generate(JsonNode? node, JsonSchema schema, JsonSchema rootSchema, string typeNameHint)
        {
            if (schema != rootSchema && node is JsonObject)
            {
                JsonSchema? referencedSchema = null;
                if (node is JsonObject obj)
                {
                    var propertyNames = obj.Select(p => p.Key).ToList();

                    referencedSchema = rootSchema.Definitions
                        .Select(t => t.Value)
                        .FirstOrDefault(s =>
                            s.Type == JsonObjectType.Object &&
                            propertyNames.Count > 0 &&
                            propertyNames.All(p => s.Properties.ContainsKey(p)));
                }

                if (referencedSchema == null)
                {
                    referencedSchema = new JsonSchema();
                    AddSchemaDefinition(rootSchema, referencedSchema, typeNameHint);
                }

                schema.Reference = referencedSchema;
                GenerateWithoutReference(node, referencedSchema, rootSchema, typeNameHint);
                return;
            }

            GenerateWithoutReference(node, schema, rootSchema, typeNameHint);
        }

        private void GenerateWithoutReference(JsonNode? node, JsonSchema schema, JsonSchema rootSchema, string typeNameHint)
        {
            if (node == null)
            {
                return;
            }

            if (node is JsonObject)
            {
                GenerateObject(node, schema, rootSchema);
            }
            else if (node is JsonArray)
            {
                GenerateArray(node, schema, rootSchema, typeNameHint);
            }
            else if (node is JsonValue jsonValue)
            {
                var element = jsonValue.GetValue<JsonElement>();
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        schema.Type = JsonObjectType.String;
                        var stringValue = element.GetString()!;

                        // Try to detect date/datetime formats
                        if (DateTime.TryParse(stringValue, out var dateTime))
                        {
                            schema.Format = dateTime == dateTime.Date
                                ? JsonFormatStrings.Date
                                : JsonFormatStrings.DateTime;
                        }
                        else if (Guid.TryParse(stringValue, out _))
                        {
                            schema.Format = JsonFormatStrings.Guid;
                        }
                        else if (Uri.TryCreate(stringValue, UriKind.Absolute, out _) && stringValue.Contains("://"))
                        {
                            schema.Format = JsonFormatStrings.Uri;
                        }
                        break;

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        schema.Type = JsonObjectType.Boolean;
                        break;

                    case JsonValueKind.Number:
                        if (element.TryGetInt64(out _))
                        {
                            schema.Type = JsonObjectType.Integer;
                        }
                        else
                        {
                            schema.Type = JsonObjectType.Number;
                        }
                        break;
                }

                if (schema.Type == JsonObjectType.String)
                {
                    var str = element.GetString()!;

                    if (Regex.IsMatch(str, "^[0-2][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]$"))
                    {
                        schema.Format = JsonFormatStrings.Date;
                    }

                    if (Regex.IsMatch(str, "^[0-2][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9](:[0-9][0-9])?$"))
                    {
                        schema.Format = JsonFormatStrings.DateTime;
                    }

                    if (Regex.IsMatch(str, "^[0-9][0-9]:[0-9][0-9](:[0-9][0-9])?$"))
                    {
                        schema.Format = JsonFormatStrings.Duration;
                    }
                }

                if (_settings.SchemaType == SchemaType.OpenApi3)
                {
                    if (schema.Type == JsonObjectType.Integer)
                    {
                        var value = element.GetInt64();
                        if (value is < int.MinValue or > int.MaxValue)
                        {
                            schema.Format = JsonFormatStrings.Long;
                        }
                        else
                        {
                            schema.Format = JsonFormatStrings.Integer;
                        }
                    }

                    if (schema.Type == JsonObjectType.Number)
                    {
                        var value = element.GetDouble();
                        if (value is < float.MinValue or > float.MaxValue)
                        {
                            schema.Format = JsonFormatStrings.Double;
                        }
                        else
                        {
                            schema.Format = JsonFormatStrings.Float;
                        }
                    }
                }
            }
        }

        private void GenerateObject(JsonNode node, JsonSchema schema, JsonSchema rootSchema)
        {
            schema.Type = JsonObjectType.Object;
            foreach (var property in ((JsonObject)node))
            {
                var propertySchema = new JsonSchemaProperty();
                var propertyName = property.Value is JsonArray ? ConversionUtilities.Singularize(property.Key) : property.Key;
                var typeNameHint = ConversionUtilities.ConvertToUpperCamelCase(propertyName, true);

                Generate(property.Value, propertySchema, rootSchema, typeNameHint);
                schema.Properties[property.Key] = propertySchema;
            }
        }

        private void GenerateArray(JsonNode node, JsonSchema schema, JsonSchema rootSchema, string typeNameHint)
        {
            schema.Type = JsonObjectType.Array;

            var itemSchemas = ((JsonArray)node).Select(item =>
            {
                var itemSchema = new JsonSchema();
                GenerateWithoutReference(item, itemSchema, rootSchema, typeNameHint);
                return itemSchema;
            }).ToList();

            if (itemSchemas.Count == 0)
            {
                schema.Item = new JsonSchema();
            }
            else if (itemSchemas.GroupBy(s => s.Type).Count() == 1)
            {
                MergeAndAssignItemSchemas(rootSchema, schema, itemSchemas, typeNameHint);
            }
            else
            {
                schema.Item = itemSchemas.First();
            }
        }

        private static void MergeAndAssignItemSchemas(JsonSchema rootSchema, JsonSchema schema, List<JsonSchema> itemSchemas, string typeNameHint)
        {
            var firstItemSchema = itemSchemas.First();
            var itemSchema = new JsonSchema
            {
                Type = firstItemSchema.Type
            };

            if (firstItemSchema.Type == JsonObjectType.Object)
            {
                foreach (var property in itemSchemas.SelectMany(s => s.Properties).GroupBy(p => p.Key))
                {
                    itemSchema.Properties[property.Key] = property.First().Value;
                }
            }

            AddSchemaDefinition(rootSchema, itemSchema, typeNameHint);
            schema.Item = new JsonSchema { Reference = itemSchema };
        }

        private static void AddSchemaDefinition(JsonSchema rootSchema, JsonSchema schema, string typeNameHint)
        {
            if (string.IsNullOrEmpty(typeNameHint) || rootSchema.Definitions.ContainsKey(typeNameHint))
            {
                rootSchema.Definitions["Anonymous" + (rootSchema.Definitions.Count + 1)] = schema;
            }
            else
            {
                rootSchema.Definitions[typeNameHint] = schema;
            }
        }
    }
}
