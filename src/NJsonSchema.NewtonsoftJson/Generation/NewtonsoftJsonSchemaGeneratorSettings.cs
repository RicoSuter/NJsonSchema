//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Reflection;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using NJsonSchema.Generation;

namespace NJsonSchema.NewtonsoftJson.Generation
{
    /// <inheritdoc />
    public class NewtonsoftJsonSchemaGeneratorSettings : JsonSchemaGeneratorSettings
    {
        private readonly Dictionary<string, JsonContract?> _cachedContracts = [];

        private JsonSerializerSettings _serializerSettings;

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
            get => _serializerSettings; set
            {
                _serializerSettings = value;
                _cachedContracts.Clear();
            }
        }

        /// <summary>Gets the contract resolver.</summary>
        /// <returns>The contract resolver.</returns>
        /// <exception cref="InvalidOperationException">A setting is misconfigured.</exception>
        [JsonIgnore]
        public IContractResolver ActualContractResolver => SerializerSettings?.ContractResolver ?? new DefaultContractResolver();

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

            if (!_cachedContracts.ContainsKey(key))
            {
                lock (_cachedContracts)
                {
                    if (!_cachedContracts.ContainsKey(key))
                    {
                        _cachedContracts[key] = !type.GetTypeInfo().IsGenericTypeDefinition ?
                            ActualContractResolver.ResolveContract(type) :
                            null;
                    }
                }
            }

            return _cachedContracts[key];
        }
    }
}