//-----------------------------------------------------------------------
// <copyright file="ISchemaResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema
{
    /// <summary>Manager which resolves types to schemas.</summary>
    public interface ISchemaResolver
    {
        /// <summary>Determines whether the specified type has a schema.</summary>
        /// <param name="type">The type.</param>
        /// <param name="enumType">The enum type or Object.</param>
        /// <returns><c>true</c> when the mapping exists.</returns>
        bool HasSchema(Type type, JsonObjectType enumType);

        /// <summary>Gets the schema for a given type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="enumType">The enum type or Object.</param>
        /// <returns>The schema.</returns>
        JsonSchema4 GetSchema(Type type, JsonObjectType enumType);

        /// <summary>Adds a schema to type mapping.</summary>
        /// <param name="type">The type.</param>
        /// <param name="enumType">The enum type or Object.</param>
        /// <param name="schema">The schema.</param>
        void AddSchema(Type type, JsonObjectType enumType, JsonSchema4 schema);
    }
}