//-----------------------------------------------------------------------
// <copyright file="ITypeNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema
{
    /// <summary>Generates the type name for a given <see cref="JsonSchema4"/>.</summary>
    public interface ITypeNameGenerator
    {
        /// <summary>Generates the type name.</summary>
        /// <param name="schema">The property.</param>
        /// <returns>The new name.</returns>
        string Generate(JsonSchema4 schema);
    }
}