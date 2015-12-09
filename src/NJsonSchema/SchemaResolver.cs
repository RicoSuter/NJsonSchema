//-----------------------------------------------------------------------
// <copyright file="SchemaResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace NJsonSchema
{
    /// <summary>Manager which resolves types to schemas.</summary>
    public class SchemaResolver : ISchemaResolver
    {
        private readonly Dictionary<string, JsonSchema4> _mappings = new Dictionary<string, JsonSchema4>();

        /// <summary>Determines whether the specified type has a schema.</summary>
        /// <param name="type">The type.</param>
        /// <param name="enumType">The enum type or null.</param>
        /// <returns><c>true</c> when the mapping exists.</returns>
        public bool HasSchema(Type type, JsonObjectType enumType)
        {
            return _mappings.ContainsKey(GetKey(type, enumType));
        }

        /// <summary>Gets the schema for a given type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="enumType">The enum type or null.</param>
        /// <returns>The schema.</returns>
        public JsonSchema4 GetSchema(Type type, JsonObjectType enumType)
        {
            return _mappings[GetKey(type, enumType)];
        }

        /// <summary>Adds a schema to type mapping.</summary>
        /// <param name="type">The type.</param>
        /// <param name="enumType">The enum type or null.</param>
        /// <param name="schema">The schema.</param>
        public void AddSchema(Type type, JsonObjectType enumType, JsonSchema4 schema)
        {
            _mappings.Add(GetKey(type, enumType), schema);
        }

        /// <summary>Gets all the schemas.</summary>
        public IEnumerable<JsonSchema4> Schemes
        {
            get { return _mappings.Values; }
        }

        private string GetKey(Type type, JsonObjectType enumType)
        {
            return type.Name + ":" + enumType;
        }
    }
}