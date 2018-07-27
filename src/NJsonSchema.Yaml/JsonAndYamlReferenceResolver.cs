//-----------------------------------------------------------------------
// <copyright file="JsonAndYamlReferenceResolver.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using NJsonSchema.Generation;
using NJsonSchema.References;

namespace NJsonSchema.Yaml
{
    /// <summary>Resolves JSON Pointer references.</summary>
    public class JsonAndYamlReferenceResolver : JsonReferenceResolver
    {
        /// <summary>Initializes a new instance of the <see cref="JsonAndYamlReferenceResolver"/> class.</summary>
        /// <param name="schemaResolver">The schema resolver.</param>
        public JsonAndYamlReferenceResolver(JsonSchemaResolver schemaResolver)
            : base(schemaResolver)
        {
        }

        /// <summary>Creates the factory to be used in the FromJsonAsync method.</summary>
        /// <param name="settings">The generator settings.</param>
        /// <returns>The factory.</returns>
        public static Func<JsonSchema4, JsonReferenceResolver> CreateJsonAndYamlReferenceResolverFactory(JsonSchemaGeneratorSettings settings)
        {
            JsonReferenceResolver ReferenceResolverFactory(JsonSchema4 schema) =>
                new JsonAndYamlReferenceResolver(new JsonSchemaResolver(schema, settings));

            return ReferenceResolverFactory;
        }

        /// <summary>Resolves a file reference.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The resolved JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The System.IO.File API is not available on this platform.</exception>
        public override async Task<IJsonReference> ResolveFileReferenceAsync(string filePath)
        {
            return await JsonSchemaYaml.FromFileAsync(filePath, schema => this).ConfigureAwait(false);
        }

        /// <summary>Resolves an URL reference.</summary>
        /// <param name="url">The URL.</param>
        /// <exception cref="NotSupportedException">The HttpClient.GetAsync API is not available on this platform.</exception>
        public override async Task<IJsonReference> ResolveUrlReferenceAsync(string url)
        {
            return await JsonSchemaYaml.FromUrlAsync(url, schema => this).ConfigureAwait(false);
        }
    }
}
