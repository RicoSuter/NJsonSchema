//-----------------------------------------------------------------------
// <copyright file="JsonSchemaVisitorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using NJsonSchema.References;

namespace NJsonSchema.Visitors
{
    /// <summary>Visitor to transform an object with <see cref="JsonSchema"/> objects.</summary>
    public abstract class AsyncJsonSchemaVisitorBase : AsyncJsonReferenceVisitorBase
    {
        /// <summary>Called when a <see cref="JsonSchema"/> is visited.</summary>
        /// <param name="schema">The visited schema.</param>
        /// <param name="path">The path.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The task.</returns>
        protected abstract Task<JsonSchema> VisitSchemaAsync(JsonSchema schema, string path, string typeNameHint, CancellationToken cancellationToken);

        /// <summary>Called when a <see cref="IJsonReference"/> is visited.</summary>
        /// <param name="reference">The visited schema.</param>
        /// <param name="path">The path.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The task.</returns>
        protected override async Task<IJsonReference> VisitJsonReferenceAsync(IJsonReference reference, string path, string typeNameHint, CancellationToken cancellationToken)
        {
            if (reference is JsonSchema schema)
            {
                return await VisitSchemaAsync(schema, path, typeNameHint, cancellationToken).ConfigureAwait(false);
            }

            return reference;
        }
    }
}