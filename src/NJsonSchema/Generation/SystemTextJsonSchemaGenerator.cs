//-----------------------------------------------------------------------
// <copyright file="SystemTextJsonSchemaGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.Generation
{
    /// <summary>
    /// 
    /// </summary>
    public static class SystemTextJsonSchemaGenerator
    {
        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>()
        {
            return JsonSchemaGenerator.FromType<TType>(new SystemTextJsonSchemaGeneratorSettings());
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type)
        {
            return JsonSchemaGenerator.FromType(type, new SystemTextJsonSchemaGeneratorSettings());
        }
    }
}