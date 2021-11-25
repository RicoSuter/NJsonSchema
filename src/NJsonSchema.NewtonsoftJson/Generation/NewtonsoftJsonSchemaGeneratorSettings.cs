//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NJsonSchema.Generation
{
    /// <inheritdocs />
    public class NewtonsoftJsonSchemaGeneratorSettings : JsonSchemaGeneratorSettings
    {
        private Dictionary<string, JsonContract> _cachedContracts = new Dictionary<string, JsonContract>();

        private EnumHandling _defaultEnumHandling;
        private PropertyNameHandling _defaultPropertyNameHandling;
        private JsonSerializerSettings _serializerSettings;

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaGeneratorSettings"/> class.</summary>
        public NewtonsoftJsonSchemaGeneratorSettings()
        {
            ReflectionService = new NewtonsoftJsonReflectionService();
            SerializerSettings = new JsonSerializerSettings();

#pragma warning disable CS0618
            DefaultEnumHandling = EnumHandling.Integer;
            DefaultPropertyNameHandling = PropertyNameHandling.Default;
#pragma warning restore CS0618
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

        /// <summary>Gets or sets the default property name handling (default: Default).</summary>
        [Obsolete("Use SerializerSettings directly instead. In NSwag.AspNetCore the property is set automatically.")]
        public PropertyNameHandling DefaultPropertyNameHandling
        {
            get => _defaultPropertyNameHandling; set
            {
                _defaultPropertyNameHandling = value;
                UpdateActualContractResolverAndSerializerSettings();
            }
        }

        /// <summary>Gets or sets the default enum handling (default: Integer).</summary>
        [Obsolete("Use SerializerSettings directly instead. In NSwag.AspNetCore the property is set automatically.")]
        public EnumHandling DefaultEnumHandling
        {
            get => _defaultEnumHandling; set
            {
                _defaultEnumHandling = value;
                UpdateActualSerializerSettings();
            }
        }

        /// <summary>Gets the contract resolver.</summary>
        /// <returns>The contract resolver.</returns>
        /// <exception cref="InvalidOperationException">A setting is misconfigured.</exception>
        [JsonIgnore]
        public IContractResolver ActualContractResolver => SerializerSettings.ContractResolver ?? new DefaultContractResolver();

        /// <summary>Gets the contract for the given type.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The contract.</returns>
        public JsonContract ResolveContract(Type type)
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

#pragma warning disable CS0618
        private void UpdateActualContractResolverAndSerializerSettings()
        {
            _cachedContracts = new Dictionary<string, JsonContract>();

            if (DefaultPropertyNameHandling == PropertyNameHandling.CamelCase)
            {
                SerializerSettings.ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy(false, true) };
            }
            else if (DefaultPropertyNameHandling == PropertyNameHandling.SnakeCase)
            {
                SerializerSettings.ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy(false, true) };
            }

            UpdateActualSerializerSettings();
        }

        private void UpdateActualSerializerSettings()
        {
            if (DefaultEnumHandling == EnumHandling.String)
            {
                SerializerSettings.Converters.Add(new StringEnumConverter());
            }
            else if (DefaultEnumHandling == EnumHandling.CamelCaseString)
            {
                SerializerSettings.Converters.Add(new StringEnumConverter(true));
            }

            _cachedContracts.Clear();
        }
#pragma warning restore CS0618
    }
}