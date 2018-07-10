//-----------------------------------------------------------------------
// <copyright file="DynamicApis.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------


using System;
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

        /// <summary>Gets or sets the current schema type.</summary>
        public static SchemaType CurrentSchemaType
        {
            get => _currentSchemaType;
            private set => _currentSchemaType = value;
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
        /// <returns></returns>
        [Obsolete("This overload will be removed soon.")]
        public static string ToJson(object obj, SchemaType schemaType, IContractResolver contractResolver)
        {
            return ToJson(obj, schemaType, contractResolver, Formatting.Indented);
        }

        /// <summary>Serializes an object to a JSON string with reference handling.</summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="schemaType">The schema type.</param>
        /// <param name="contractResolver">The contract resolver.</param>
        /// <param name="formatting">The formatting.</param>
        /// <returns></returns>
        public static string ToJson(object obj, SchemaType schemaType, IContractResolver contractResolver, Formatting formatting)
        {
            IsWriting = false;
            CurrentSchemaType = schemaType;

            JsonSchemaReferenceUtilities.UpdateSchemaReferencePaths(obj, false, contractResolver);
            var json = JsonConvert.SerializeObject(obj, formatting, new JsonSerializerSettings
            {
                ContractResolver = contractResolver
            });

            return JsonSchemaReferenceUtilities.ConvertPropertyReferences(json);
        }

        /// <summary>Deserializes JSON data to a schema with reference handling.</summary>
        /// <param name="json">The JSON data.</param>
        /// <param name="schemaType">The schema type.</param>
        /// <param name="documentPath">The document path.</param>
        /// <param name="referenceResolverFactory">The reference resolver factory.</param>
        /// <param name="contractResolver">The contract resolver.</param>
        /// <returns>The deserialized schema.</returns>
        public static async Task<T> FromJsonAsync<T>(string json, SchemaType schemaType, string documentPath,
            Func<T, JsonReferenceResolver> referenceResolverFactory, IContractResolver contractResolver)
        {
            IsWriting = true;
            CurrentSchemaType = schemaType;

            json = JsonSchemaReferenceUtilities.ConvertJsonReferences(json);
            var settings = new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                ConstructorHandling = ConstructorHandling.Default,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };

            var schema = JsonConvert.DeserializeObject<T>(json, settings);
            if (schema is IDocumentPathProvider documentPathProvider)
                documentPathProvider.DocumentPath = documentPath;

            var referenceResolver = referenceResolverFactory.Invoke(schema);
            if (schema is IJsonReference referenceSchema)
            {
                if (!string.IsNullOrEmpty(documentPath))
                    referenceResolver.AddDocumentReference(documentPath, referenceSchema);
            }

            await JsonSchemaReferenceUtilities.UpdateSchemaReferencesAsync(schema, referenceResolver, contractResolver).ConfigureAwait(false);
            return schema;
        }
    }
}
