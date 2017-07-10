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
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation.TypeMappers;

namespace NJsonSchema.Generation
{
    /// <summary>The JSON Schema generator settings.</summary>
    public class JsonSchemaGeneratorSettings
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchemaGeneratorSettings"/> class.</summary>
        public JsonSchemaGeneratorSettings()
        {
            DefaultEnumHandling = EnumHandling.Integer;
            NullHandling = NullHandling.JsonSchema;
            DefaultPropertyNameHandling = PropertyNameHandling.Default;

            TypeNameGenerator = new DefaultTypeNameGenerator();
            SchemaNameGenerator = new DefaultSchemaNameGenerator();
        }
        
        /// <summary>Gets or sets the default enum handling (default: Integer).</summary>
        public EnumHandling DefaultEnumHandling { get; set; }

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
        
        public readonly IgnoredPropertyAttributes IgnoredPropertyAttributes = new IgnoredPropertyAttributes(new List<Type> { typeof(JsonIgnoreAttribute) });

        /// <summary>
        /// Properties marked with <see cref="ObsoleteAttribute"/> are ignored in schema generation
        /// </summary>
        public bool IgnoreDeprecatedProperties
        {
            get => IgnoredPropertyAttributes.IgnoredAttributeTypes.Contains(typeof(ObsoleteAttribute));
            set
            {
                if (value)
                {
                    if (!IgnoredPropertyAttributes.IgnoredAttributeTypes.Contains(typeof(ObsoleteAttribute)))
                        IgnoredPropertyAttributes.IgnoredAttributeTypes.Add(typeof(ObsoleteAttribute));
                }
                else
                {
                    if (IgnoredPropertyAttributes.IgnoredAttributeTypes.Contains(typeof(ObsoleteAttribute)))
                        IgnoredPropertyAttributes.IgnoredAttributeTypes.Remove(typeof(ObsoleteAttribute));
                }
            }
        }
        
        /// <summary>Gets or sets the property nullability handling.</summary>
        public NullHandling NullHandling { get; set; }

        /// <summary>Gets or sets the contract resolver.</summary>
        public IContractResolver ContractResolver { get; set; }

        /// <summary>Gets or sets the type name generator.</summary>
        [JsonIgnore]
        public ITypeNameGenerator TypeNameGenerator { get; set; }

        /// <summary>Gets or sets the schema name generator.</summary>
        [JsonIgnore]
        public ISchemaNameGenerator SchemaNameGenerator { get; set; }

        /// <summary>Gets or sets the type mappings.</summary>
        [JsonIgnore]
        public ICollection<ITypeMapper> TypeMappers { get; set; } = new Collection<ITypeMapper>();

        /// <summary>Gets or sets the schema processors.</summary>
        [JsonIgnore]
        public IList<ISchemaProcessor> SchemaProcessors { get; } = new List<ISchemaProcessor>();

        /// <summary>Gets the contract resolver.</summary>
        /// <returns>The contract resolver.</returns>
        /// <exception cref="InvalidOperationException">The settings DefaultPropertyNameHandling and ContractResolver cannot be used at the same time.</exception>
        public IContractResolver ActualContractResolver
        {
            get
            {
                if (DefaultPropertyNameHandling != PropertyNameHandling.Default && ContractResolver != null)
                    throw new InvalidOperationException("The settingsDefaultPropertyNameHandling and ContractResolver cannot be used at the same time.");

                if (ContractResolver != null)
                    return ContractResolver;

                if (DefaultPropertyNameHandling == PropertyNameHandling.CamelCase)
                    return new CamelCasePropertyNamesContractResolver();

                if (DefaultPropertyNameHandling == PropertyNameHandling.SnakeCase)
                    return new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() };

                return new DefaultContractResolver();
            }
        }
    }
}