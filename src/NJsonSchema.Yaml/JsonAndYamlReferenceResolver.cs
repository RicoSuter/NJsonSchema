//-----------------------------------------------------------------------
// <copyright file="JsonAndYamlReferenceResolver.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NJsonSchema.References;

namespace NJsonSchema.Yaml
{
    /// <summary>Resolves JSON Pointer references.</summary>
    public class JsonAndYamlReferenceResolver : JsonReferenceResolver
    {
        /// <summary>Initializes a new instance of the <see cref="JsonAndYamlReferenceResolver"/> class.</summary>
        /// <param name="schemaAppender">The schema appender.</param>
        public JsonAndYamlReferenceResolver(JsonSchemaAppender schemaAppender)
            : base(schemaAppender)
        {
        }

        /// <summary>Creates the factory to be used in the FromJsonAsync method.</summary>
        /// <param name="typeNameGenerator">The type name generator.</param>
        /// <returns>The factory.</returns>
        public static Func<JsonSchema, JsonReferenceResolver> CreateJsonAndYamlReferenceResolverFactory(ITypeNameGenerator typeNameGenerator)
        {
            JsonReferenceResolver ReferenceResolverFactory(JsonSchema schema)
            {
                return new JsonAndYamlReferenceResolver(new JsonSchemaAppender(schema, typeNameGenerator));
            }

            return ReferenceResolverFactory;
        }

        /// <summary>Resolves a file reference.</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The resolved JSON Schema.</returns>
        /// <exception cref="NotSupportedException">The System.IO.File API is not available on this platform.</exception>
        public override async Task<IJsonReference> ResolveFileReferenceAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await JsonSchemaYaml.FromFileAsync(filePath, schema => this, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Resolves an URL reference.</summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <exception cref="NotSupportedException">The HttpClient.GetAsync API is not available on this platform.</exception>
        public override async Task<IJsonReference> ResolveUrlReferenceAsync(string url, CancellationToken cancellationToken = default)
        {
            return await JsonSchemaYaml.FromUrlAsync(url, schema => this, cancellationToken).ConfigureAwait(false);
        }
    }
}
