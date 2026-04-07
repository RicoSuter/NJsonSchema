//-----------------------------------------------------------------------
// <copyright file="SwaggerYamlDocument.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Dynamic;
using System.Text.Json.Nodes;
using NJsonSchema.Infrastructure;
using YamlDotNet.Serialization;

namespace NJsonSchema.Yaml
{
    /// <summary>Extension methods to load and save <see cref="JsonSchema"/> from/to YAML.</summary>
    public static class JsonSchemaYaml
    {
        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema" />.</summary>
        /// <param name="data">The JSON string.</param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema> FromYamlAsync(string data)
        {
            var factory = JsonAndYamlReferenceResolver.CreateJsonAndYamlReferenceResolverFactory(new DefaultTypeNameGenerator());
            return await JsonSchemaYaml.FromYamlAsync(data, null, factory).ConfigureAwait(false);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema" />.</summary>
        /// <param name="data">The JSON string.</param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema> FromYamlAsync(string data, string? documentPath)
        {
            var factory = JsonAndYamlReferenceResolver.CreateJsonAndYamlReferenceResolverFactory(new DefaultTypeNameGenerator());
            return await FromYamlAsync(data, documentPath, factory).ConfigureAwait(false);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema" />.</summary>
        /// <param name="data">The JSON string.</param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema> FromYamlAsync(string data, string? documentPath, Func<JsonSchema, JsonReferenceResolver> referenceResolverFactory, CancellationToken cancellationToken = default)
        {
            var deserializer = new DeserializerBuilder()
                .WithAttemptingUnquotedStringTypeDeserialization()
                .Build();
            var yamlObject = deserializer.Deserialize(new StringReader(data));
            var serializer = new SerializerBuilder()
                .JsonCompatible()
                .Build();

            var json = serializer.Serialize(yamlObject);
            return await JsonSchema.FromJsonAsync(json, documentPath, referenceResolverFactory, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Converts the JSON Schema to YAML.</summary>
        /// <returns>The YAML string.</returns>
        public static string ToYaml(this JsonSchema document)
        {
            var json = document.ToJson()!;
            var jsonNode = JsonNode.Parse(json);
            var expandoObject = ConvertJsonNodeToExpandoObject(jsonNode);

            var serializer = new Serializer();
            return serializer.Serialize(expandoObject);
        }

        /// <summary>Creates a JSON Schema from a JSON file.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static async Task<JsonSchema> FromFileAsync(string filePath)
        {
            var factory = JsonAndYamlReferenceResolver.CreateJsonAndYamlReferenceResolverFactory(new DefaultTypeNameGenerator());
            return await FromFileAsync(filePath, factory).ConfigureAwait(false);
        }

        /// <summary>Creates a JSON Schema from a JSON file.</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static async Task<JsonSchema> FromFileAsync(string filePath, Func<JsonSchema, JsonReferenceResolver> referenceResolverFactory, CancellationToken cancellationToken = default)
        {
            var data = File.ReadAllText(filePath);
            return await FromYamlAsync(data, filePath, referenceResolverFactory, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Creates a JSON Schema from an URL.</summary>
        /// <param name="url">The URL.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The <see cref="JsonSchema"/>.</returns>
        public static async Task<JsonSchema> FromUrlAsync(string url, Func<JsonSchema, JsonReferenceResolver> referenceResolverFactory, CancellationToken cancellationToken = default)
        {
            var data = await DynamicApis.HttpGetAsync(url, cancellationToken).ConfigureAwait(false);
            return await FromYamlAsync(data, url, referenceResolverFactory, cancellationToken).ConfigureAwait(false);
        }

        private static object? ConvertJsonNodeToExpandoObject(JsonNode? node)
        {
            if (node is JsonObject jsonObject)
            {
                var expando = new ExpandoObject();
                var dict = (IDictionary<string, object?>)expando;
                foreach (var property in jsonObject)
                {
                    dict[property.Key] = property.Value != null ? ConvertJsonNodeToExpandoObject(property.Value) : null;
                }
                return expando;
            }
            else if (node is JsonArray jsonArray)
            {
                return jsonArray.Select(item => item != null ? ConvertJsonNodeToExpandoObject(item) : null).ToList();
            }
            else if (node is JsonValue jsonValue)
            {
                if (jsonValue.TryGetValue<bool>(out var boolValue)) return boolValue;
                if (jsonValue.TryGetValue<long>(out var longValue)) return longValue;
                if (jsonValue.TryGetValue<double>(out var doubleValue)) return doubleValue;
                if (jsonValue.TryGetValue<string>(out var stringValue)) return stringValue;
                return node.ToJsonString();
            }
            return null;
        }

        private static string ConvertYamlToJson(string data)
        {
            var deserializer = new DeserializerBuilder()
                .WithAttemptingUnquotedStringTypeDeserialization()
                .Build();
            var yamlObject = deserializer.Deserialize(new StringReader(data));

            var serializer = new SerializerBuilder()
                .JsonCompatible()
                .Build();

            var json = serializer.Serialize(yamlObject);
            return json;
        }
    }
}
