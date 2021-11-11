using NJsonSchema.Generation;
using System;

namespace NJsonSchema.NewtonsoftJson.Generation
{
    public static class NewtonsoftJsonSchemaGenerator
    {
        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>()
        {
            return FromType<TType>(new NewtonsoftJsonSchemaGeneratorSettings());
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type)
        {
            return FromType(type, new NewtonsoftJsonSchemaGeneratorSettings());
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>(NewtonsoftJsonSchemaGeneratorSettings settings)
        {
            var generator = new JsonSchemaGenerator(settings);
            return generator.Generate(typeof(TType));
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type, NewtonsoftJsonSchemaGeneratorSettings settings)
        {
            var generator = new JsonSchemaGenerator(settings);
            return generator.Generate(type);
        }
    }
}
