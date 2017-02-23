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
            //DefaultPropertyNameHandling = PropertyNameHandling.Default;

            TypeNameGenerator = new DefaultTypeNameGenerator();
            SchemaNameGenerator = new DefaultSchemaNameGenerator();
        }

        /// <summary>Gets or sets the default enum handling (default: Integer).</summary>
        public EnumHandling DefaultEnumHandling { get; set; }

        ///// <summary>Gets or sets the default property name handling (default: Default).</summary>
        //public PropertyNameHandling DefaultPropertyNameHandling { get; set; }

        /// <summary>Gets or sets a value indicating whether to flatten the inheritance hierarchy instead of using allOf to describe inheritance (default: false).</summary>
        public bool FlattenInheritanceHierarchy { get; set; }

        /// <summary>Gets or sets a value indicating whether to generate schemas for types in <see cref="KnownTypeAttribute"/> attributes (default: true).</summary>
        public bool GenerateKnownTypes { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether to generate xmlObject representation for definitions (default: false).</summary>
        public bool GenerateXmlObjects { get; set; } = false;

        /// <summary>Gets or sets the property nullability handling.</summary>
        public NullHandling NullHandling { get; set; }

        /// <summary>Gets or sets the type name generator.</summary>
        [JsonIgnore]
        public ITypeNameGenerator TypeNameGenerator { get; set; }

        /// <summary>Gets or sets the schema name generator.</summary>
        [JsonIgnore]
        public ISchemaNameGenerator SchemaNameGenerator { get; set; }

        /// <summary>Gets or sets the contract resolver.</summary>
        public IContractResolver ContractResolver { get; set; } = new DefaultContractResolver();

        /// <summary>Gets or sets the type mappings.</summary>
        [JsonIgnore]
        public ICollection<ITypeMapper> TypeMappers { get; set; } = new Collection<ITypeMapper>();
    }
}