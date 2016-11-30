//-----------------------------------------------------------------------
// <copyright file="SchemaResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NJsonSchema.Generation;

namespace NJsonSchema
{
    /// <summary>Manager which resolves types to schemas and appends missing schemas to the root object.</summary>
    public class SchemaResolver
    {
        private readonly Dictionary<string, JsonSchema4> _mappings = new Dictionary<string, JsonSchema4>();

        private readonly ISchemaDefinitionAppender _schemaDefinitionAppender;
        private readonly ISchemaNameGenerator _schemaNameGenerator;

        /// <summary>Initializes a new instance of the <see cref="SchemaResolver" /> class.</summary>
        /// <param name="settings">The settings.</param>
        public SchemaResolver(JsonSchemaGeneratorSettings settings)
            : this(settings, new JsonSchemaDefinitionAppender(settings.TypeNameGenerator))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="SchemaResolver" /> class.</summary>
        /// <param name="settings">The settings.</param>
        /// <param name="schemaDefinitionAppender">The schema definition appender.</param>
        public SchemaResolver(JsonSchemaGeneratorSettings settings, ISchemaDefinitionAppender schemaDefinitionAppender)
        {
            _schemaNameGenerator = settings.SchemaNameGenerator;
            _schemaDefinitionAppender = schemaDefinitionAppender;
        }

        /// <summary>Determines whether the specified type has a schema.</summary>
        /// <param name="type">The type.</param>
        /// <param name="isIntegerEnumeration">Specifies whether the type is an integer enum.</param>
        /// <returns><c>true</c> when the mapping exists.</returns>
        public bool HasSchema(Type type, bool isIntegerEnumeration)
        {
            return _mappings.ContainsKey(GetKey(type, isIntegerEnumeration));
        }

        /// <summary>Gets the schema for a given type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="isIntegerEnumeration">Specifies whether the type is an integer enum.</param>
        /// <returns>The schema.</returns>
        public JsonSchema4 GetSchema(Type type, bool isIntegerEnumeration)
        {
            return _mappings[GetKey(type, isIntegerEnumeration)];
        }

        /// <summary>Adds a schema to type mapping.</summary>
        /// <param name="type">The type.</param>
        /// <param name="isIntegerEnumeration">Specifies whether the type is an integer enum.</param>
        /// <param name="schema">The schema.</param>
        /// <exception cref="InvalidOperationException">Added schema is not a JsonSchema4 instance.</exception>
        public virtual void AddSchema(Type type, bool isIntegerEnumeration, JsonSchema4 schema)
        {
            if (schema.GetType() != typeof(JsonSchema4))
                throw new InvalidOperationException("Added schema is not a JsonSchema4 instance.");

            if (_schemaDefinitionAppender.TrySetRoot(schema) == false)
                _schemaDefinitionAppender.AppendSchema(schema, _schemaNameGenerator.Generate(type));

            _mappings.Add(GetKey(type, isIntegerEnumeration), schema);
        }

        /// <summary>Gets all the schemas.</summary>
        public IEnumerable<JsonSchema4> Schemas => _mappings.Values;

        private string GetKey(Type type, bool isIntegerEnum)
        {
            return type.FullName + (isIntegerEnum ? ":Integer" : string.Empty);
        }
    }
}