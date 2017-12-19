//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using NJsonSchema.Converters;
using System;

namespace NJsonSchema.Generation
{
    /// <summary>Describes an externally defined base type with a discriminator property.</summary>
    public class DiscriminatorDefinition
    {
        /// <summary>Initializes a new instance of the <see cref="DiscriminatorDefinition"/> class.</summary>
        /// <param name="baseType">The base type.</param>
        public DiscriminatorDefinition(Type baseType) 
            : this(baseType, JsonInheritanceConverter.DefaultDiscriminatorName)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DiscriminatorDefinition"/> class.</summary>
        /// <param name="baseType">The base type.</param>
        /// <param name="discriminatorPropertyName">The discriminator property name.</param>
        public DiscriminatorDefinition(Type baseType, string discriminatorPropertyName)
        {
            BaseType = baseType;
            PropertyName = discriminatorPropertyName;
        }

        /// <summary>Gets the base type to add the discriminator property.</summary>
        public Type BaseType { get; }

        /// <summary>Gets the discriminator property name.</summary>
        public string PropertyName { get; }
    }
}