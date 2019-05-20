//-----------------------------------------------------------------------
// <copyright file="OpenApiDiscriminator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using NJsonSchema.Infrastructure;
using NJsonSchema.References;
using Newtonsoft.Json.Linq;

namespace NJsonSchema
{
    /// <summary>Describes a schema discriminator.</summary>
    public class OpenApiDiscriminator
    {
        /// <summary>Gets or sets the discriminator property name.</summary>
        [JsonProperty("propertyName", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string PropertyName { get; set; }

        /// <summary>Gets or sets the discriminator mappings.</summary>
        [JsonProperty("mapping", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [JsonConverter(typeof(DiscriminatorMappingConverter))]
        public IDictionary<string, JsonSchema> Mapping { get; } = new Dictionary<string, JsonSchema>();

        /// <summary>The currently used <see cref="JsonInheritanceConverter"/>.</summary>
        [JsonIgnore]
        public object JsonInheritanceConverter { get; set; }

        /// <summary>Adds a discriminator mapping for the given type and schema based on the used <see cref="JsonInheritanceConverter"/>.</summary>
        /// <param name="type">The type.</param>
        /// <param name="schema">The schema.</param>
        public void AddMapping(Type type, JsonSchema schema)
        {
            dynamic converter = JsonInheritanceConverter;

            var getDiscriminatorValueMethod = JsonInheritanceConverter.GetType()
#if LEGACY
                .GetMethod(nameof(Converters.JsonInheritanceConverter.GetDiscriminatorValue), new Type[] { typeof(Type) });
#else
                .GetRuntimeMethod(nameof(Converters.JsonInheritanceConverter.GetDiscriminatorValue), new Type[] { typeof(Type) });
#endif

            if (getDiscriminatorValueMethod != null)
            {
                var discriminatorValue = converter.GetDiscriminatorValue(type);
                Mapping[discriminatorValue] = new JsonSchema { Reference = schema.ActualSchema };
            }
            else
                Mapping[type.Name] = new JsonSchema { Reference = schema.ActualSchema };
        }

        /// <summary>
        /// Used to convert from Dictionary{string, JsonSchema4} (NJsonSchema model) to Dictionary{string, string} (OpenAPI).
        /// See https://github.com/OAI/OpenAPI-Specification/blob/master/versions/3.0.2.md#discriminator-object and
        /// issue https://github.com/RSuter/NSwag/issues/1684
        /// </summary>
        private class DiscriminatorMappingConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return true;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var openApiMapping = serializer.Deserialize<Dictionary<string, string>>(reader);
                if (openApiMapping != null && existingValue != null)
                {
                    var internalMapping = (IDictionary<string, JsonSchema>)existingValue;
                    internalMapping.Clear();

                    foreach (var tuple in openApiMapping)
                    {
                        var schema = new JsonSchema();
                        ((IJsonReferenceBase)schema).ReferencePath = tuple.Value;
                        internalMapping[tuple.Key] = schema;
                    }
                }

                return existingValue;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var internalMapping = value as IDictionary<string, JsonSchema>;
                if (internalMapping != null)
                {
                    var openApiMapping = new Dictionary<string, string>();
                    foreach (var tuple in internalMapping)
                    {
                        openApiMapping[tuple.Key] = ((IJsonReferenceBase)tuple.Value).ReferencePath;
                    }

                    var jObject = JObject.FromObject(openApiMapping, serializer);
                    writer.WriteToken(jObject.CreateReader());
                }
                else
                {
                    writer.WriteValue((string)null);
                }
            }
        }
    }
}