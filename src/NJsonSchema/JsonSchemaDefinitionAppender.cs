//-----------------------------------------------------------------------
// <copyright file="JsonSchemaDefinitionAppender.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema
{
    /// <summary>Appends a JSON Schema to the Definitions of another JSON Schema.</summary>
    public class JsonSchemaDefinitionAppender : ISchemaDefinitionAppender
    {
        private readonly ITypeNameGenerator _typeNameGenerator;
        private JsonSchema4 _rootSchema;

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaDefinitionAppender" /> class which uses the first touched schema as root object.</summary>
        /// <param name="typeNameGenerator">The type name generator.</param>
        public JsonSchemaDefinitionAppender(ITypeNameGenerator typeNameGenerator) : this(null, typeNameGenerator)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaDefinitionAppender" /> class.</summary>
        /// <param name="rootSchema">The root object.</param>
        /// <param name="typeNameGenerator">The type name generator.</param>
        public JsonSchemaDefinitionAppender(JsonSchema4 rootSchema, ITypeNameGenerator typeNameGenerator)
        {
            _rootSchema = rootSchema;
            _typeNameGenerator = typeNameGenerator;
        }

        /// <summary>Tries to set the root of the appender.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <returns>true when the root was not set before.</returns>
        public bool TrySetRoot(object rootObject)
        {
            if (_rootSchema == null && rootObject is JsonSchema4)
            {
                _rootSchema = (JsonSchema4)rootObject;
                return true;
            }
            return false;
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

            var typeName = schema.GetTypeName(_typeNameGenerator, typeNameHint);
            if (!string.IsNullOrEmpty(typeName) && !_rootSchema.Definitions.ContainsKey(typeName))
                _rootSchema.Definitions[typeName] = schema;
            else
                _rootSchema.Definitions["ref_" + Guid.NewGuid().ToString().Replace("-", "_")] = schema;
        }
    }
}