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
        /// <summary>Gets or sets the root object to append schemas to.</summary>
        object RootObject { get; set; }

        /// <summary>Appends the schema to the root object.</summary>
        /// <param name="objectToAppend">The object to append.</param>
        void Append(JsonSchema4 objectToAppend);
    }
}