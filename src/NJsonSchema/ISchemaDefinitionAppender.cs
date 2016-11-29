//-----------------------------------------------------------------------
// <copyright file="ISchemaDefinitionAppender.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema
{
    /// <summary>Appends a schema to the definition list of the root object.</summary>
    public interface ISchemaDefinitionAppender
    {
        /// <summary>Tries to set the root of the appender.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <returns>true when the root was not set before.</returns>
        bool TrySetRoot(object rootObject);

        /// <summary>Appends the schema to the root object.</summary>
        /// <param name="schema">The schema to append.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        void AppendSchema(JsonSchema4 schema, string typeNameHint);
    }
}