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

namespace NJsonSchema
{
    /// <summary>Generates a sample JSON object from a JSON Schema.</summary>
    public class SampleJsonDataGenerator
    {
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
        /// <returns>The JSON token.</returns>
        public JToken Generate(JsonSchema schema)
        {
            var stack = new Stack<JsonSchema>();
            stack.Push(schema);
            return Generate(schema, stack);
        }

        private JToken Generate(JsonSchema schema, Stack<JsonSchema> schemaStack)
        {
            var property = schema as JsonSchemaProperty;
            schema = schema.ActualSchema;
            try
            {
                schemaStack.Push(schema);
                if (schemaStack.Count(s => s == schema) > _settings.MaxRecursionLevel)
                {
                    return null;
                }

                if (schema.Type.IsObject() || GetPropertiesToGenerate(schema.AllOf).Any())
                {
                    var schemas = new[] { schema }.Concat(schema.AllOf.Select(x => x.ActualSchema));
                    var properties = GetPropertiesToGenerate(schemas);

                    var obj = new JObject();
                    foreach (var p in properties)
                    {
                        obj[p.Key] = Generate(p.Value, schemaStack);
                    }

                    return obj;
                }
                else if (schema.Default != null)
                {
                    return JToken.FromObject(schema.Default);
                }
                else if (schema.Type.IsArray())
                {
                    if (schema.Item != null)
                    {
                        var array = new JArray();

                        var item = Generate(schema.Item, schemaStack);
                        if (item != null)
                        {
                            array.Add(item);
                        }

                        return array;
                    }
                    else if (schema.Items.Count > 0)
                    {
                        var array = new JArray();
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
                        return JToken.FromObject(schema.Enumeration.First());
                    }
                    else if (schema.Type.IsInteger())
                    {
                        return HandleIntegerType(schema);
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
                        return JToken.FromObject(false);
                    }
                }

                return null;
            }
            finally
            {
                schemaStack.Pop();
            }
        }
        private JToken HandleNumberType(JsonSchema schema)
        {
            if (schema.ExclusiveMinimumRaw != null)
            {
                return JToken.FromObject((decimal)(float.Parse(schema.Minimum.ToString()) + 0.1));
            }
            else if (schema.ExclusiveMinimum != null)
            {
                return JToken.FromObject(decimal.Parse(schema.ExclusiveMinimum.ToString()));
            }
            else if (schema.Minimum.HasValue)
            {
                return decimal.Parse(schema.Minimum.ToString());
            }
            return JToken.FromObject(0.0);
        }

        private JToken HandleIntegerType(JsonSchema schema)
        {
            if (schema.ExclusiveMinimumRaw != null)
            {
                return JToken.FromObject(Convert.ToInt32(schema.ExclusiveMinimumRaw));
            }
            else if (schema.ExclusiveMinimum != null)
            {
                return JToken.FromObject(Convert.ToInt32(schema.ExclusiveMinimum));
            }
            else if (schema.Minimum.HasValue)
            {
                return Convert.ToInt32(schema.Minimum);
            }
            return JToken.FromObject(0);
        }

        private JToken HandleStringType(JsonSchema schema, JsonSchemaProperty property)
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
