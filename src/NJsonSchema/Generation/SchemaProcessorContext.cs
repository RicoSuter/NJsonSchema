//-----------------------------------------------------------------------
// <copyright file="SchemaProcessorContext.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.Generation
{
    /// <summary>The schema processor context.</summary>
    public class SchemaProcessorContext
    {
        /// <summary>Initializes a new instance of the <see cref="SchemaProcessorContext" /> class.</summary>
        /// <param name="schema">The JSON Schema.</param>
        /// <param name="resolver">The resolver.</param>
        /// <param name="generator">The generator.</param>
        public SchemaProcessorContext(JsonSchema4 schema, JsonSchemaResolver resolver, JsonSchemaGenerator generator)
        {
            Schema = schema;
            Resolver = resolver;
            Generator = generator;
        }

        /// <summary>The JSON Schema to process.</summary>
        public JsonSchema4 Schema { get; }

        /// <summary>The JSON Schema resolver.</summary>
        public JsonSchemaResolver Resolver { get; }

        /// <summary>The JSON Schema generator.</summary>
        public JsonSchemaGenerator Generator { get; }
    }
}