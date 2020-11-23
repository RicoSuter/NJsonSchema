//-----------------------------------------------------------------------
// <copyright file="JsonSchemaVisitorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using NJsonSchema.References;

namespace NJsonSchema.Visitors
{
    /// <summary>Visitor to transform an object with <see cref="JsonSchema"/> objects.</summary>
    public abstract class JsonSchemaVisitorBase : JsonReferenceVisitorBase
    {
        /// <summary>Called when a <see cref="JsonSchema"/> is visited.</summary>
        /// <param name="schema">The visited schema.</param>
        /// <param name="path">The path.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The task.</returns>
        protected abstract JsonSchema VisitSchema(JsonSchema schema, string path, string typeNameHint);

        /// <summary>Called when a <see cref="IJsonReference"/> is visited.</summary>
        /// <param name="reference">The visited schema.</param>
        /// <param name="path">The path.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The task.</returns>
        protected override IJsonReference VisitJsonReference(IJsonReference reference, string path, string typeNameHint)
        {
            if (reference is JsonSchema schema)
            {
                return VisitSchema(schema, path, typeNameHint);
            }

            return reference;
        }
    }
}