//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Annotations;
using NJsonSchema.Generation.TypeMappers;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Generation
{
    /// <summary>The JSON Schema generator settings.</summary>
    public class JsonSchemaGeneratorSettings
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchemaGeneratorSettings"/> class.</summary>
        public JsonSchemaGeneratorSettings()
        {
            DefaultEnumHandling = EnumHandling.Integer;
            DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.Null;
            DefaultPropertyNameHandling = PropertyNameHandling.Default;
            SchemaType = SchemaType.JsonSchema;

            TypeNameGenerator = new DefaultTypeNameGenerator();
            SchemaNameGenerator = new DefaultSchemaNameGenerator();
            ReflectionService = new DefaultReflectionService();

            ExcludedTypeNames = new string[0];
        }

        /// <summary>Gets or sets the default enum handling (default: Integer).</summary>
        public EnumHandling DefaultEnumHandling { get; set; }

        /// <summary>Gets or sets the default null handling (if NotNullAttribute and CanBeNullAttribute are missing, default: Null).</summary>
        public ReferenceTypeNullHandling DefaultReferenceTypeNullHandling { get; set; }

        /// <summary>Gets or sets the default property name handling (default: Default).</summary>
        public PropertyNameHandling DefaultPropertyNameHandling { get; set; }

        /// <summary>Gets or sets a value indicating whether to generate abstract properties (i.e. interface and abstract properties. Properties may defined multiple times in a inheritance hierarchy, default: false).</summary>
        public bool GenerateAbstractProperties { get; set; }

        /// <summary>Gets or sets a value indicating whether to flatten the inheritance hierarchy instead of using allOf to describe inheritance (default: false).</summary>
        public bool FlattenInheritanceHierarchy { get; set; }

        /// <summary>Gets or sets a value indicating whether to generate schemas for types in <see cref="KnownTypeAttribute"/> attributes (default: true).</summary>
        public bool GenerateKnownTypes { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether to generate xmlObject representation for definitions (default: false).</summary>
        public bool GenerateXmlObjects { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether to ignore properties with the <see cref="ObsoleteAttribute"/>.</summary>
        public bool IgnoreObsoleteProperties { get; set; }

        /// <summary>Gets or sets a value indicating whether to use $ref references even if additional properties are 
        /// defined on the object (otherwise allOf/oneOf with $ref is used, default: false).</summary>
        public bool AllowReferencesWithProperties { get; set; }

        /// <summary>Gets or sets the schema type to generate (default: JsonSchema).</summary>
        public SchemaType SchemaType { get; set; }

        /// <summary>Gets or sets the contract resolver.</summary>
        public IContractResolver ContractResolver { get; set; }

        /// <summary>Gets or sets the excluded type names (same as <see cref="JsonSchemaIgnoreAttribute"/>).</summary>
        public string[] ExcludedTypeNames { get; set; }

        /// <summary>Gets or sets the type name generator.</summary>
        [JsonIgnore]
        public ITypeNameGenerator TypeNameGenerator { get; set; }

        /// <summary>Gets or sets the schema name generator.</summary>
        [JsonIgnore]
        public ISchemaNameGenerator SchemaNameGenerator { get; set; }

        /// <summary>Gets or sets the reflection service.</summary>
        [JsonIgnore]
        public IReflectionService ReflectionService { get; set; }

        /// <summary>Gets or sets the type mappings.</summary>
        [JsonIgnore]
        public ICollection<ITypeMapper> TypeMappers { get; set; } = new Collection<ITypeMapper>();

        /// <summary>Gets or sets the schema processors.</summary>
        [JsonIgnore]
        public ICollection<ISchemaProcessor> SchemaProcessors { get; } = new Collection<ISchemaProcessor>();

        /// <summary>Gets the contract resolver.</summary>
        /// <returns>The contract resolver.</returns>
        /// <exception cref="InvalidOperationException">The settings DefaultPropertyNameHandling and ContractResolver cannot be used at the same time.</exception>
        public IContractResolver ActualContractResolver
        {
            get
            {
                if (DefaultPropertyNameHandling != PropertyNameHandling.Default && ContractResolver != null)
                {
                    throw new InvalidOperationException("The settings DefaultPropertyNameHandling and ContractResolver cannot be used at the same time " +
                                                        "(set DefaultPropertyNameHandling to Default when using a custom ContractResolver and change the " +
                                                        "property name handling on the custom ContractResolver).");
                }

                if (ContractResolver != null)
                    return ContractResolver;

                if (DefaultPropertyNameHandling == PropertyNameHandling.CamelCase)
                    return new CamelCasePropertyNamesContractResolver();

                if (DefaultPropertyNameHandling == PropertyNameHandling.SnakeCase)
                    return new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() };

                return new DefaultContractResolver();
            }
        }

        /// <summary>Gets the contract for the given type.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The contract.</returns>
        public JsonContract ResolveContract(Type type)
        {
            return !type.GetTypeInfo().IsGenericTypeDefinition ?
                ActualContractResolver.ResolveContract(type) :
                null;
        }
    }
}