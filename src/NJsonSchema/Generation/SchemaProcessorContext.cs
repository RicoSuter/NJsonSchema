//-----------------------------------------------------------------------
// <copyright file="SchemaProcessorContext.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;

namespace NJsonSchema.Generation
{
    /// <summary>The schema processor context.</summary>
    public class SchemaProcessorContext
    {
        /// <summary>Initializes a new instance of the <see cref="SchemaProcessorContext" /> class.</summary>
        /// <param name="contextualType">The source contextual type.</param>
        /// <param name="schema">The JSON Schema.</param>
        /// <param name="resolver">The resolver.</param>
        /// <param name="generator">The generator.</param>
        /// <param name="settings">The settings.</param>
        public SchemaProcessorContext(ContextualType contextualType, JsonSchema schema, JsonSchemaResolver resolver, JsonSchemaGenerator generator, JsonSchemaGeneratorSettings settings)
        {
            ContextualType = contextualType;
            Schema = schema;
            Resolver = resolver;
            Generator = generator;
            Settings = settings;
        }

        /// <summary>The source type.</summary>
        [Obsolete("Use ContextualType to obtain this instead.")]
        public Type Type { get => ContextualType.OriginalType; }

        /// <summary>The source contextual type.</summary>
        public ContextualType ContextualType { get; }

        /// <summary>The JSON Schema to process.</summary>
        public JsonSchema Schema { get; }

        /// <summary>The JSON Schema resolver.</summary>
        public JsonSchemaResolver Resolver { get; }

        /// <summary>Gets the JSON Schema generator.</summary>
        public JsonSchemaGenerator Generator { get; }

        /// <summary>Gets the settings.</summary>
        public JsonSchemaGeneratorSettings Settings { get; }
    }
}