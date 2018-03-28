//-----------------------------------------------------------------------
// <copyright file="JsonSchemaVisitorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Threading.Tasks;
using NJsonSchema.References;

namespace NJsonSchema.Visitors
{
    /// <summary>Visitor to transform an object with <see cref="JsonSchema4"/> objects.</summary>
    public abstract class JsonSchemaVisitorBase : JsonReferenceVisitorBase
    {
        /// <summary>Called when a <see cref="JsonSchema4"/> is visited.</summary>
        /// <param name="schema">The visited schema.</param>
        /// <param name="path">The path.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The task.</returns>
        protected abstract Task<JsonSchema4> VisitSchemaAsync(JsonSchema4 schema, string path, string typeNameHint);

        /// <summary>Called when a <see cref="IJsonReference"/> is visited.</summary>
        /// <param name="reference">The visited schema.</param>
        /// <param name="path">The path.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The task.</returns>
        protected override async Task<IJsonReference> VisitJsonReferenceAsync(IJsonReference reference, string path, string typeNameHint)
        {
            if (reference is JsonSchema4 schema)
                return await VisitSchemaAsync(schema, path, typeNameHint).ConfigureAwait(false);

            return reference;
        }
    }
}