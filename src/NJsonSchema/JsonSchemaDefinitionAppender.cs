//-----------------------------------------------------------------------
// <copyright file="JsonSchemaDefinitionAppender.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using NJsonSchema.Generation;

namespace NJsonSchema
{
    /// <summary>Appends a JSON Schema to the Definitions of another JSON Schema.</summary>
    public class JsonSchemaDefinitionAppender : ISchemaDefinitionAppender
    {
        private readonly ITypeNameGenerator _typeNameGenerator;
        private readonly JsonSchema4 _rootSchema;

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaDefinitionAppender" /> class.</summary>
        /// <param name="rootSchema">The root object.</param>
        /// <param name="settings">The settings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="rootSchema" /> is <see langword="null" /></exception>
        public JsonSchemaDefinitionAppender(JsonSchema4 rootSchema, JsonSchemaGeneratorSettings settings)
        {
            if (rootSchema == null)
                throw new ArgumentNullException(nameof(rootSchema));

            _rootSchema = rootSchema;
            _typeNameGenerator = settings.TypeNameGenerator;
        }

        /// <summary>Appends the schema to the root object.</summary>
        /// <param name="schema">The schema to append.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <exception cref="InvalidOperationException">Could not find the JSON path of a child object.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null"/></exception>
        public void AppendSchema(JsonSchema4 schema, string typeNameHint)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            if (_rootSchema == null)
                throw new InvalidOperationException("Could not find the JSON path of a child object.");

            if (schema != _rootSchema)
            {
                var typeName = schema.GetTypeName(_typeNameGenerator, typeNameHint);
                if (!string.IsNullOrEmpty(typeName) && !_rootSchema.Definitions.ContainsKey(typeName))
                    _rootSchema.Definitions[typeName] = schema;
                else
                    _rootSchema.Definitions["ref_" + Guid.NewGuid().ToString().Replace("-", "_")] = schema;
            }
        }
    }
}