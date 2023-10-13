//-----------------------------------------------------------------------
// <copyright file="DynamicApis.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------


using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.References;

namespace NJsonSchema.Infrastructure
{
    /// <summary>The JSON Schema serialization context holding information about the current serialization.</summary>
    public class JsonSchemaSerialization
    {
        [ThreadStatic]
        private static SchemaType _currentSchemaType;

        [ThreadStatic]
        private static bool _isWriting;

        [ThreadStatic]
        private static JsonSerializerSettings? _currentSerializerSettings;

        /// <summary>Gets or sets the current schema type.</summary>
        public static SchemaType CurrentSchemaType
        {
            get => _currentSchemaType;
            private set => _currentSchemaType = value;
        }

        /// <summary>Gets the current serializer settings.</summary>
        public static JsonSerializerSettings? CurrentSerializerSettings
        {
            get => _currentSerializerSettings;
            private set => _currentSerializerSettings = value;
        }

        /// <summary>Gets or sets a value indicating whether the object is currently converted to JSON.</summary>
        public static bool IsWriting
        {
            get => _isWriting;
            private set => _isWriting = value;
        }

        /// <summary>Serializes an object to a JSON string with reference handling.</summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="schemaType">The schema type.</param>
        /// <param name="contractResolver">The contract resolver.</param>
        /// <param name="formatting">The formatting.</param>
        /// <returns>The JSON.</returns>
        public static string ToJson(object obj, SchemaType schemaType, IContractResolver contractResolver, Formatting formatting)
        {
            IsWriting = false;
            CurrentSchemaType = schemaType;

            JsonSchemaReferenceUtilities.UpdateSchemaReferencePaths(obj, false, contractResolver);

            IsWriting = false;
            CurrentSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = contractResolver
            };

            var json = JsonConvert.SerializeObject(obj, formatting, CurrentSerializerSettings);

            CurrentSerializerSettings = null;
            CurrentSchemaType = SchemaType.JsonSchema;

            return json;
        }

        /// <summary>Deserializes JSON data to a schema with reference handling.</summary>
        /// <param name="json">The JSON data.</param>
        /// <param name="schemaType">The schema type.</param>
        /// <param name="documentPath">The document path.</param>
        /// <param name="referenceResolverFactory">The reference resolver factory.</param>
        /// <param name="contractResolver">The contract resolver.</param>
        /// <returns>The deserialized schema.</returns>
        [Obsolete("Use FromJsonAsync with cancellation token instead.")]
        public static Task<T> FromJsonAsync<T>(string json, SchemaType schemaType, string? documentPath,
            Func<T, JsonReferenceResolver> referenceResolverFactory, IContractResolver contractResolver)
            where T : notnull
        {
            return FromJsonAsync(json, schemaType, documentPath, referenceResolverFactory, contractResolver, CancellationToken.None);
        }

        /// <summary>Deserializes JSON data to a schema with reference handling.</summary>
        /// <param name="json">The JSON data.</param>
        /// <param name="schemaType">The schema type.</param>
        /// <param name="documentPath">The document path.</param>
        /// <param name="referenceResolverFactory">The reference resolver factory.</param>
        /// <param name="contractResolver">The contract resolver.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The deserialized schema.</returns>
        public static Task<T> FromJsonAsync<T>(string json, SchemaType schemaType, string? documentPath,
            Func<T, JsonReferenceResolver> referenceResolverFactory, IContractResolver contractResolver, CancellationToken cancellationToken = default)
            where T : notnull
        {
            var loader = () => FromJson<T>(json, contractResolver);
            return FromJsonWithLoaderAsync(loader, schemaType, documentPath, referenceResolverFactory, contractResolver, cancellationToken);
        }

        /// <summary>Deserializes JSON data to a schema with reference handling.</summary>
        /// <param name="stream">The JSON data stream.</param>
        /// <param name="schemaType">The schema type.</param>
        /// <param name="documentPath">The document path.</param>
        /// <param name="referenceResolverFactory">The reference resolver factory.</param>
        /// <param name="contractResolver">The contract resolver.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The deserialized schema.</returns>
        public static Task<T> FromJsonAsync<T>(Stream stream, SchemaType schemaType, string? documentPath,
            Func<T, JsonReferenceResolver> referenceResolverFactory, IContractResolver contractResolver, CancellationToken cancellationToken = default)
            where T : notnull
        {
            var loader = () => FromJson<T>(stream, contractResolver);
            return FromJsonWithLoaderAsync(loader, schemaType, documentPath, referenceResolverFactory, contractResolver, cancellationToken);
        }

        private static async Task<T> FromJsonWithLoaderAsync<T>(
            Func<T> loader,
            SchemaType schemaType,
            string? documentPath,
            Func<T, JsonReferenceResolver> referenceResolverFactory,
            IContractResolver contractResolver,
            CancellationToken cancellationToken)
            where T : notnull
        {
            cancellationToken.ThrowIfCancellationRequested();
            CurrentSchemaType = schemaType;

            T schema;
            try
            {
                schema = loader();
                if (schema is IDocumentPathProvider documentPathProvider)
                {
                    documentPathProvider.DocumentPath = documentPath;
                }

                var referenceResolver = referenceResolverFactory.Invoke(schema);
                if (schema is IJsonReference referenceSchema)
                {
                    if (!string.IsNullOrEmpty(documentPath))
                    {
                        referenceResolver.AddDocumentReference(documentPath!, referenceSchema);
                    }
                }

                await JsonSchemaReferenceUtilities.UpdateSchemaReferencesAsync(schema, referenceResolver, contractResolver, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CurrentSchemaType = SchemaType.JsonSchema;
            }

            return schema;
        }

        /// <summary>Deserializes JSON data with the given contract resolver.</summary>
        /// <param name="json">The JSON data.</param>
        /// <param name="contractResolver">The contract resolver.</param>
        /// <returns>The deserialized schema.</returns>
        public static T? FromJson<T>(string json, IContractResolver contractResolver)
        {
            IsWriting = true;
            UpdateCurrentSerializerSettings<T>(contractResolver);

            try
            {
                return JsonConvert.DeserializeObject<T>(json, CurrentSerializerSettings);
            }
            finally
            {
                CurrentSerializerSettings = null;
            }
        }

        /// <summary>Deserializes JSON data with the given contract resolver.</summary>
        /// <param name="stream">The JSON data stream.</param>
        /// <param name="contractResolver">The contract resolver.</param>
        /// <returns>The deserialized schema.</returns>
        public static T? FromJson<T>(Stream stream, IContractResolver contractResolver)
        {
            IsWriting = true;
            UpdateCurrentSerializerSettings<T>(contractResolver);

            try
            {
                using var reader = new StreamReader(stream);
                using var jsonReader = new JsonTextReader(reader);

                var serializer = JsonSerializer.Create(CurrentSerializerSettings);
                return serializer.Deserialize<T>(jsonReader);
            }
            finally
            {
                CurrentSerializerSettings = null;
            }
        }

        private static void UpdateCurrentSerializerSettings<T>(IContractResolver contractResolver)
        {
            CurrentSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                ConstructorHandling = ConstructorHandling.Default,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.None
            };
        }
    }
}
