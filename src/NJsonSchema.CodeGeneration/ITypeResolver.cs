//-----------------------------------------------------------------------
// <copyright file="ITypeResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The type resolver interface.</summary>
    public interface ITypeResolver
    {
        /// <summary>Gets or generates the type name for the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        string GetOrGenerateTypeName(JsonSchema4 schema, string typeNameHint);

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        string Resolve(JsonSchema4 schema, bool isNullable, string typeNameHint);
    }
}