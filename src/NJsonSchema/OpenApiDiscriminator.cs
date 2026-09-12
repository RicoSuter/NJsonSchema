//-----------------------------------------------------------------------
// <copyright file="OpenApiDiscriminator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NJsonSchema.References;

namespace NJsonSchema
{
    /// <summary>Describes a schema discriminator.</summary>
    public class OpenApiDiscriminator
    {
        /// <summary>Gets or sets the discriminator property name.</summary>
        [JsonPropertyName("propertyName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PropertyName { get; set; }

        /// <summary>Gets or sets the discriminator mappings.</summary>
        [JsonPropertyName("mapping")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonConverter(typeof(DiscriminatorMappingConverter))]
        public IDictionary<string, JsonSchema> Mapping { get; set; } = new Dictionary<string, JsonSchema>();

        /// <summary>The currently used <see cref="JsonInheritanceConverter"/>.</summary>
        [JsonIgnore]
        public object? JsonInheritanceConverter { get; set; }

        /// <summary>Adds a discriminator mapping for the given type and schema based on the used <see cref="JsonInheritanceConverter"/>.</summary>
        /// <param name="type">The type.</param>
        /// <param name="schema">The schema.</param>
        public void AddMapping(Type type, JsonSchema schema)
        {
            var getDiscriminatorValueMethod = JsonInheritanceConverter?.GetType()
                .GetRuntimeMethod("GetDiscriminatorValue", [typeof(Type)]);

            if (getDiscriminatorValueMethod != null)
            {
                var discriminatorValue = (string)getDiscriminatorValueMethod.Invoke(JsonInheritanceConverter, [type])!;
                Mapping[discriminatorValue] = new JsonSchema { Reference = schema.ActualSchema };
            }
            else
            {
                Mapping[type.Name] = new JsonSchema { Reference = schema.ActualSchema };
            }
        }

        /// <summary>
        /// Used to convert from Dictionary{string, JsonSchema} (NJsonSchema model) to Dictionary{string, string} (OpenAPI).
        /// </summary>
        private sealed class DiscriminatorMappingConverter : JsonConverter<IDictionary<string, JsonSchema>>
        {
            public override IDictionary<string, JsonSchema>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var openApiMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
                if (openApiMapping != null)
                {
                    var internalMapping = new Dictionary<string, JsonSchema>();
                    foreach (var tuple in openApiMapping)
                    {
                        var schema = new JsonSchema();
                        ((IJsonReferenceBase)schema).ReferencePath = tuple.Value;
                        internalMapping[tuple.Key] = schema;
                    }
                    return internalMapping;
                }

                return null;
            }

            public override void Write(Utf8JsonWriter writer, IDictionary<string, JsonSchema> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                foreach (var tuple in value)
                {
                    writer.WriteString(tuple.Key, ((IJsonReferenceBase)tuple.Value).ReferencePath);
                }
                writer.WriteEndObject();
            }
        }
    }
}
