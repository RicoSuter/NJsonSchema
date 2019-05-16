//-----------------------------------------------------------------------
// <copyright file="IReflectionService.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
using System;

namespace NJsonSchema.Generation
{
    /// <summary>Provides methods to reflect on types.</summary>
    public interface IReflectionService
    {
        /// <summary>Creates a <see cref="JsonTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="typeWithContext">The type.</param>
        /// <param name="defaultReferenceTypeNullHandling">The default reference type null handling.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonTypeDescription"/>. </returns>
        JsonTypeDescription GetDescription(ContextualType typeWithContext, ReferenceTypeNullHandling defaultReferenceTypeNullHandling, JsonSchemaGeneratorSettings settings);

        /// <summary>Creates a <see cref="JsonTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="typeWithContext">The type.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonTypeDescription"/>. </returns>
        JsonTypeDescription GetDescription(ContextualType typeWithContext, JsonSchemaGeneratorSettings settings);

        /// <summary>Checks whether a type is nullable.</summary>
        /// <param name="typeWithContext">The type.</param>
        /// <param name="defaultReferenceTypeNullHandling">The default reference type null handling used when no nullability information is available.</param>
        /// <returns>true if the type can be null.</returns>
        bool IsNullable(ContextualType typeWithContext, ReferenceTypeNullHandling defaultReferenceTypeNullHandling);
    }
}