//-----------------------------------------------------------------------
// <copyright file="IReflectionService.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
using System;

namespace NJsonSchema.Generation
{
    /// <summary>Provides methods to reflect on types.</summary>
    public interface IReflectionService
    {
        /// <summary>
        /// Converts an enum value to a JSON string.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        string ConvertEnumValue(object value, JsonSchemaGeneratorSettings settings);

        /// <summary>
        /// Gets the property name for the given accessor info.
        /// </summary>
        /// <param name="accessorInfo">The accessor info.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The property name.</returns>
        string GetPropertyName(ContextualAccessorInfo accessorInfo, JsonSchemaGeneratorSettings settings);

        /// <summary>
        /// Generates the properties for the given type.
        /// </summary>
        /// <param name="schema">The schema.</param>
        /// <param name="contextualType">The type.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="schemaGenerator">The schema generator.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        void GenerateProperties(JsonSchema schema, ContextualType contextualType, JsonSchemaGeneratorSettings settings, JsonSchemaGenerator schemaGenerator, JsonSchemaResolver schemaResolver);

        /// <summary>Creates a <see cref="JsonTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="contextualType">The type.</param>
        /// <param name="defaultReferenceTypeNullHandling">The default reference type null handling.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonTypeDescription"/>. </returns>
        JsonTypeDescription GetDescription(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling, JsonSchemaGeneratorSettings settings);

        /// <summary>Creates a <see cref="JsonTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="contextualType">The type.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonTypeDescription"/>. </returns>
        JsonTypeDescription GetDescription(ContextualType contextualType, JsonSchemaGeneratorSettings settings);

        /// <summary>Checks whether a type is nullable.</summary>
        /// <param name="contextualType">The type.</param>
        /// <param name="defaultReferenceTypeNullHandling">The default reference type null handling used when no nullability information is available.</param>
        /// <returns>true if the type can be null.</returns>
        bool IsNullable(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling);

        /// <summary>Checks whether the give type is a string enum.</summary>
        /// <param name="contextualType">The type.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The result.</returns>
        bool IsStringEnum(ContextualType contextualType, JsonSchemaGeneratorSettings settings);
    }
}