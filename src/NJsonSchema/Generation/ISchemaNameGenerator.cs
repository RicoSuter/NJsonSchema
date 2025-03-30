﻿//-----------------------------------------------------------------------
// <copyright file="ISchemaNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.Generation
{
    /// <summary>The schema name generator.</summary>
    public interface ISchemaNameGenerator
    {
        /// <summary>Generates the name of the JSON Schema for the given type.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The new name.</returns>
        string Generate(Type type);
    }
}
