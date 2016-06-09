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

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaDefinitionAppender"/> class.</summary>
        public JsonSchemaDefinitionAppender(ITypeNameGenerator typeNameGenerator)
        {
            _typeNameGenerator = typeNameGenerator; 
        }

        /// <summary>Appends the schema to the root object.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="objectToAppend">The object to append.</param>
        /// <exception cref="InvalidOperationException">Could not find the JSON path of a child object.</exception>
        public void Append(object root, JsonSchema4 objectToAppend)
        {
            var rootSchema = root as JsonSchema4;
            if (rootSchema != null && objectToAppend != null)
            {
                var typeName = objectToAppend.GetTypeName(_typeNameGenerator); 
                if (!string.IsNullOrEmpty(typeName) && !rootSchema.Definitions.ContainsKey(typeName))
                    rootSchema.Definitions[typeName] = objectToAppend;
                else
                    rootSchema.Definitions["ref_" + Guid.NewGuid().ToString().Replace("-", "_")] = objectToAppend;
            }
            else
                throw new InvalidOperationException("Could not find the JSON path of a child object.");
        }
    }
}