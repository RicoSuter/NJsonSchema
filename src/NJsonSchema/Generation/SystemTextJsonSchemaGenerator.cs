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
    /// Provides method to generate JSON Schemas with the System.Text.Json schema generator.
    /// </summary>
    public static class SystemTextJsonSchemaGenerator
    {
        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>()
        {
            return FromType<TType>(new SystemTextJsonSchemaGeneratorSettings());
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type)
        {
            return FromType(type, new SystemTextJsonSchemaGeneratorSettings());
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>(SystemTextJsonSchemaGeneratorSettings settings)
        {
            var generator = new JsonSchemaGenerator(settings);
            return generator.Generate(typeof(TType));
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type, SystemTextJsonSchemaGeneratorSettings settings)
        {
            var generator = new JsonSchemaGenerator(settings);
            return generator.Generate(type);
        }
    }
}