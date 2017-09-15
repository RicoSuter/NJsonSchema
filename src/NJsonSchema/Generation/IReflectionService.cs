//-----------------------------------------------------------------------
// <copyright file="IReflectionService.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace NJsonSchema.Generation
{
    /// <summary>Provides methods to reflect on types.</summary>
    public interface IReflectionService
    {
        /// <summary>Creates a <see cref="JsonTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="type">The type. </param>
        /// <param name="parentAttributes">The parent's attributes (i.e. parameter or property attributes).</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonTypeDescription"/>. </returns>
        JsonTypeDescription GetDescription(Type type, IEnumerable<Attribute> parentAttributes, JsonSchemaGeneratorSettings settings);

        /// <summary>Checks whether a type is nullable.</summary>
        /// <param name="type">The type.</param>
        /// <param name="parentAttributes">The parent attributes (e.g. property or parameter attributes).</param>
        /// <param name="settings">The settings</param>
        /// <returns>true if the type can be null.</returns>
        bool IsNullable(Type type, IEnumerable<Attribute> parentAttributes, JsonSchemaGeneratorSettings settings);
    }
}