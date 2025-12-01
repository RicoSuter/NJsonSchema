//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Concurrent;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using NJsonSchema.Generation;

namespace NJsonSchema.NewtonsoftJson.Generation
{
    /// <inheritdoc />
    public class NewtonsoftJsonSchemaGeneratorSettings : JsonSchemaGeneratorSettings
    {
        private readonly ConcurrentDictionary<string, JsonContract?> _cachedContracts = [];

        private JsonSerializerSettings _serializerSettings;
        private readonly DefaultContractResolver _defaultContractResolver = new();

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaGeneratorSettings"/> class.</summary>
        public NewtonsoftJsonSchemaGeneratorSettings()
            : base(new NewtonsoftJsonReflectionService())
        {
            _serializerSettings = new JsonSerializerSettings();
        }

        /// <summary>Gets or sets the Newtonsoft JSON serializer settings.</summary>
        [JsonIgnore]
        public JsonSerializerSettings SerializerSettings
        {
            get => _serializerSettings;
            set
            {
                _serializerSettings = value;
                _cachedContracts.Clear();
            }
        }

        /// <summary>Gets the contract resolver.</summary>
        /// <returns>The contract resolver.</returns>
        /// <exception cref="InvalidOperationException">A setting is misconfigured.</exception>
        [JsonIgnore]
        public IContractResolver ActualContractResolver => SerializerSettings?.ContractResolver ?? _defaultContractResolver;

        /// <summary>Gets the contract for the given type.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The contract.</returns>
        public JsonContract? ResolveContract(Type type)
        {
            var key = type.FullName;
            if (key == null)
            {
                return null;
            }

#if NET8_0_OR_GREATER
            return _cachedContracts.GetOrAdd(
                key, static (_, state) => !state.type.IsGenericTypeDefinition ? state.ActualContractResolver.ResolveContract(state.type) : null,
                (type, ActualContractResolver)
            );
#else
            return _cachedContracts.GetOrAdd(key, s => !type.IsGenericTypeDefinition ? ActualContractResolver.ResolveContract(type) : null);
#endif
        }
    }
}