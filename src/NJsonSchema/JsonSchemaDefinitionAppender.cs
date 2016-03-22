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
        /// <summary>Appends the schema to the root object.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="objectToAppend">The object to append.</param>
        /// <exception cref="InvalidOperationException">Could not find the JSON path of a child object.</exception>
        public void Append(object root, JsonSchema4 objectToAppend)
        {
            var rootSchema = root as JsonSchema4;
            if (rootSchema != null && objectToAppend != null)
                rootSchema.Definitions["ref_" + Guid.NewGuid()] = objectToAppend;
            else
                throw new InvalidOperationException("Could not find the JSON path of a child object.");
        }
    }
}