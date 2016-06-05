//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Runtime.Serialization;

namespace NJsonSchema.Generation
{
    /// <summary>The JSON Schema generator settings.</summary>
    public class JsonSchemaGeneratorSettings
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchemaGeneratorSettings"/> class.</summary>
        public JsonSchemaGeneratorSettings()
        {
            DefaultEnumHandling = EnumHandling.Integer;
            PropertyNullHandling = PropertyNullHandling.OneOf;
        }

        /// <summary>Gets or sets the default enum handling (default: Integer).</summary>
        public EnumHandling DefaultEnumHandling { get; set; }

        /// <summary>Gets or sets a value indicating whether to flatten the inheritance hierarchy instead of using allOf to describe inheritance (default: false).</summary>
        public bool FlattenInheritanceHierarchy { get; set; }

        /// <summary>Gets or sets a value indicating whether to generate schemas for types in <see cref="KnownTypeAttribute"/> attributes (default: true).</summary>
        public bool GenerateKnownTypes { get; set; } = true;

        /// <summary>Gets or sets the property nullability handling.</summary>
        public PropertyNullHandling PropertyNullHandling { get; set; }

        /// <summary>Gets or sets the type name generator.</summary>
        public ITypeNameGenerator TypeNameGenerator { get; set; }
    }
}