//-----------------------------------------------------------------------
// <copyright file="SampleJsonSchemaGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.Generation
{
    /// <summary>Generates a sample JSON object from a JSON Schema.</summary>
    public class SampleJsonDataGenerator
    {
        /// <summary>Generates a sample JSON object from a JSON Schema.</summary>
        /// <param name="schema">The JSON Schema.</param>
        /// <returns>The JSON token.</returns>
        public JToken Generate(JsonSchema schema)
        {
            return Generate(schema, new HashSet<JsonSchema>());
        }

        private JToken Generate(JsonSchema schema, HashSet<JsonSchema> usedSchemas)
        {
            var property = schema as JsonSchemaProperty;
            schema = schema.ActualSchema;
            if (usedSchemas.Contains(schema))
            {
                return null;
            }

            if (schema.Type.HasFlag(JsonObjectType.Object))
            {
                usedSchemas.Add(schema);

                var obj = new JObject();
                foreach (var p in schema.ActualProperties)
                {
                    obj[p.Key] = Generate(p.Value, usedSchemas);
                }
                return obj;
            }
            else if (schema.Type.HasFlag(JsonObjectType.Array))
            {
                if (schema.Item != null)
                {
                    var array = new JArray();
                    array.Add(Generate(schema.Item, usedSchemas));
                    return array;
                }
                else if (schema.Items.Count > 0)
                {
                    var array = new JArray();
                    foreach (var item in schema.Items)
                    {
                        array.Add(Generate(item, usedSchemas));
                    }
                    return array;
                }
            }
            else
            {
                if (schema.Default != null)
                {
                    return JToken.FromObject(schema.Default);
                }
                else if (schema.IsEnumeration)
                {
                    return JToken.FromObject(schema.Enumeration.First());
                }
                else if (schema.Type.HasFlag(JsonObjectType.Integer) || schema.Type.HasFlag(JsonObjectType.Number))
                {
                    return JToken.FromObject(0);
                }
                else if (schema.Type.HasFlag(JsonObjectType.String))
                {
                    if (schema.Format == JsonFormatStrings.Date)
                    {
                        return JToken.FromObject(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"));
                    }
                    else if (schema.Format == JsonFormatStrings.DateTime)
                    {
                        return JToken.FromObject(DateTimeOffset.UtcNow.ToString("o"));
                    }
                    else if (property != null)
                    {
                        return JToken.FromObject(property.Name);
                    }
                    else
                    {
                        return JToken.FromObject("");
                    }
                }
                else if (schema.Type.HasFlag(JsonObjectType.Boolean))
                {
                    return JToken.FromObject(false);
                }
            }

            return null;
        }
    }
}
