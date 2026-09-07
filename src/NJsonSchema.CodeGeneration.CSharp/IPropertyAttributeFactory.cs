//-----------------------------------------------------------------------
// <copyright file="IPropertyAttributeFactory.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using NJsonSchema.CodeGeneration.CSharp.Models;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Creates additional attributes which are rendered on a generated property.</summary>
    public interface IPropertyAttributeFactory
    {
        /// <summary>Creates the additional attributes to render for the given property (without leading indentation, e.g. "[MyAttribute]").</summary>
        /// <param name="property">The property template model.</param>
        /// <param name="schema">The property schema.</param>
        /// <returns>The attributes to render.</returns>
        IEnumerable<string> CreateAttributes(PropertyModel property, JsonSchemaProperty schema);
    }
}
