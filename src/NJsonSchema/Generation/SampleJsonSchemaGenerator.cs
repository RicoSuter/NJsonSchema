//-----------------------------------------------------------------------
// <copyright file="SampleJsonSchemaGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Generation
{
    /// <summary>Generates a JSON Schema from sample JSON data.</summary>
    public class SampleJsonSchemaGenerator
    {
        /// <summary>Generates the JSON Schema for the given JSON data.</summary>
        /// <param name="json">The JSON data.</param>
        /// <returns>The JSON Schema.</returns>
        public JsonSchema4 Generate(string json)
        {
            var token = JsonConvert.DeserializeObject<JToken>(json, new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            });

            var schema = new JsonSchema4();
            Generate(token, schema, schema);
            return schema;
        }

        private void Generate(JToken token, JsonSchema4 schema, JsonSchema4 rootSchema)
        {
            if (schema != rootSchema && token.Type == JTokenType.Object)
            {
                var referencedSchema = new JsonSchema4();
                AddSchemaDefinition(rootSchema, referencedSchema);

                schema.Reference = referencedSchema;
                GenerateWithoutReference(token, referencedSchema, rootSchema);
                return;
            }

            GenerateWithoutReference(token, schema, rootSchema);
        }

        private void GenerateWithoutReference(JToken token, JsonSchema4 schema, JsonSchema4 rootSchema)
        {
            if (token == null)
                return;

            switch (token.Type)
            {
                case JTokenType.Object:
                    GenerateObject(token, schema, rootSchema);
                    break;

                case JTokenType.Array:
                    GenerateArray(token, schema, rootSchema);
                    break;

                case JTokenType.Date:
                    schema.Type = JsonObjectType.String;
                    schema.Format = token.Value<DateTime>() == token.Value<DateTime>().Date
                        ? JsonFormatStrings.Date
                        : JsonFormatStrings.DateTime;
                    break;

                case JTokenType.String:
                    schema.Type = JsonObjectType.String;
                    break;

                case JTokenType.Boolean:
                    schema.Type = JsonObjectType.Boolean;
                    break;

                case JTokenType.Integer:
                    schema.Type = JsonObjectType.Integer;
                    break;

                case JTokenType.Float:
                    schema.Type = JsonObjectType.Number;
                    break;

                case JTokenType.Bytes:
                    schema.Type = JsonObjectType.String;
                    schema.Format = JsonFormatStrings.Byte;
                    break;

                case JTokenType.TimeSpan:
                    schema.Type = JsonObjectType.String;
                    schema.Format = JsonFormatStrings.TimeSpan;
                    break;

                case JTokenType.Guid:
                    schema.Type = JsonObjectType.String;
                    schema.Format = JsonFormatStrings.Guid;
                    break;

                case JTokenType.Uri:
                    schema.Type = JsonObjectType.String;
                    schema.Format = JsonFormatStrings.Uri;
                    break;
            }

            if (schema.Type == JsonObjectType.String && Regex.IsMatch(token.Value<string>(), "^[0-2][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]$"))
                schema.Format = JsonFormatStrings.Date;

            if (schema.Type == JsonObjectType.String && Regex.IsMatch(token.Value<string>(), "^[0-2][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9](:[0-9][0-9])?$"))
                schema.Format = JsonFormatStrings.DateTime;

            if (schema.Type == JsonObjectType.String && Regex.IsMatch(token.Value<string>(), "^[0-9][0-9]:[0-9][0-9](:[0-9][0-9])?$"))
                schema.Format = JsonFormatStrings.TimeSpan;
        }

        private void GenerateObject(JToken token, JsonSchema4 schema, JsonSchema4 rootSchema)
        {
            schema.Type = JsonObjectType.Object;
            foreach (var property in ((JObject)token).Properties())
            {
                var propertySchema = new JsonProperty();
                Generate(property.Value, propertySchema, rootSchema);
                schema.Properties[property.Name] = propertySchema;
            }
        }

        private void GenerateArray(JToken token, JsonSchema4 schema, JsonSchema4 rootSchema)
        {
            schema.Type = JsonObjectType.Array;

            var itemSchemas = ((JArray)token).Select(item =>
            {
                var itemSchema = new JsonSchema4();
                GenerateWithoutReference(item, itemSchema, rootSchema);
                return itemSchema;
            }).ToList();

            if (itemSchemas.Count == 0)
                schema.Item = new JsonSchema4();
            else if (itemSchemas.GroupBy(s => s.Type).Count() == 1)
                MergeAndAssignItemSchemas(rootSchema, schema, itemSchemas);
            else
                schema.Item = itemSchemas.First();
        }

        private void MergeAndAssignItemSchemas(JsonSchema4 rootSchema, JsonSchema4 schema, List<JsonSchema4> itemSchemas)
        {
            var firstItemSchema = itemSchemas.First();
            var itemSchema = new JsonSchema4
            {
                Type = firstItemSchema.Type
            };

            if (firstItemSchema.Type == JsonObjectType.Object)
            {
                foreach (var property in itemSchemas.SelectMany(s => s.Properties).GroupBy(p => p.Key))
                    itemSchema.Properties[property.Key] = property.First().Value;
            }

            AddSchemaDefinition(rootSchema, itemSchema);
            schema.Item = new JsonSchema4 { Reference = itemSchema };
        }

        private void AddSchemaDefinition(JsonSchema4 rootSchema, JsonSchema4 schema)
        {
            rootSchema.Definitions["Anonymous" + (rootSchema.Definitions.Count + 1)] = schema;
        }
    }
}
