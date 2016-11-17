//-----------------------------------------------------------------------
// <copyright file="DefaultTypeNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;

namespace NJsonSchema
{
    /// <summary>Converts the last part of the full type name to upper case.</summary>
    public class DefaultTypeNameGenerator : ITypeNameGenerator
    {
        /// <summary>Generates the type name.</summary>
        /// <param name="schema">The property.</param>
        /// <param name="typeNameHint">The type name hint (the property name or definition key).</param>
        /// <returns>The new name.</returns>
        public virtual string Generate(JsonSchema4 schema, string typeNameHint)
        {
            return ConversionUtilities.ConvertToUpperCamelCase(schema.TypeNameRaw?.Split('.').Last(), true);
        }
    }
}