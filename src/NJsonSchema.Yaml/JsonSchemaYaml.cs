//-----------------------------------------------------------------------
// <copyright file="SwaggerYamlDocument.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.Generation;
using NJsonSchema.Infrastructure;
using YamlDotNet.Serialization;

namespace NJsonSchema.Yaml
{
    /// <summary>Extension methods to load and save <see cref="JsonSchema4"/> from/to YAML.</summary>
    public static class JsonSchemaYaml
    {
        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema4" />.</summary>
        /// <param name="data">The JSON string.</param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema4> FromYamlAsync(string data)
        {
            var factory = JsonAndYamlReferenceResolver.CreateJsonAndYamlReferenceResolverFactory(new JsonSchemaGeneratorSettings());
            return await JsonSchemaYaml.FromYamlAsync(data, null, factory).ConfigureAwait(false);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema4" />.</summary>
        /// <param name="data">The JSON string.</param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema4> FromYamlAsync(string data, string documentPath)
        {
            var factory = JsonAndYamlReferenceResolver.CreateJsonAndYamlReferenceResolverFactory(new JsonSchemaGeneratorSettings());
            return await FromYamlAsync(data, documentPath, factory).ConfigureAwait(false);
        }

        /// <summary>Deserializes a JSON string to a <see cref="JsonSchema4" />.</summary>
        /// <param name="data">The JSON string.</param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <returns>The JSON Schema.</returns>
        public static async Task<JsonSchema4> FromYamlAsync(string data, string documentPath, Func<JsonSchema4, JsonReferenceResolver> referenceResolverFactory)
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize(new StringReader(data));
            var serializer = new SerializerBuilder()
                .JsonCompatible()
                .Build();

            var json = serializer.Serialize(yamlObject);
            return await JsonSchema4.FromJsonAsync(json, documentPath, referenceResolverFactory).ConfigureAwait(false);
        }

        /// <summary>Converts the JSON Schema to YAML.</summary>
        /// <returns>The YAML string.</returns>
        public static string ToYaml(this JsonSchema4 document)
        {
            var json = document.ToJson();
            var expConverter = new ExpandoObjectConverter();
            dynamic deserializedObject = JsonConvert.DeserializeObject<ExpandoObject>(json, expConverter);

            var serializer = new Serializer();
            return serializer.Serialize(deserializedObject);
        }

        /// <summary>Creates a JSON Schema from a JSON file.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The <see cref="JsonSchema4" />.</returns>
        public static async Task<JsonSchema4> FromFileAsync(string filePath)
        {
            var factory = JsonAndYamlReferenceResolver.CreateJsonAndYamlReferenceResolverFactory(new JsonSchemaGeneratorSettings());
            return await FromFileAsync(filePath, factory).ConfigureAwait(false);
        }

        /// <summary>Creates a JSON Schema from a JSON file.</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <returns>The <see cref="JsonSchema4" />.</returns>
        public static async Task<JsonSchema4> FromFileAsync(string filePath, Func<JsonSchema4, JsonReferenceResolver> referenceResolverFactory)
        {
            var data = await DynamicApis.FileReadAllTextAsync(filePath).ConfigureAwait(false);
            return await FromYamlAsync(data, filePath, referenceResolverFactory).ConfigureAwait(false);
        }

        /// <summary>Creates a JSON Schema from an URL.</summary>
        /// <param name="url">The URL.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <returns>The <see cref="JsonSchema4"/>.</returns>
        public static async Task<JsonSchema4> FromUrlAsync(string url, Func<JsonSchema4, JsonReferenceResolver> referenceResolverFactory)
        {
            var data = await DynamicApis.HttpGetAsync(url).ConfigureAwait(false);
            return await FromYamlAsync(data, url, referenceResolverFactory).ConfigureAwait(false);
        }

        private static string ConvertYamlToJson(string data)
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize(new StringReader(data));

            var serializer = new SerializerBuilder()
                .JsonCompatible()
                .Build();

            var json = serializer.Serialize(yamlObject);
            return json;
        }
    }
}
