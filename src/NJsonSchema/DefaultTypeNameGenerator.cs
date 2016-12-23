//-----------------------------------------------------------------------
// <copyright file="DefaultTypeNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema
{
    /// <summary>Converts the last part of the full type name to upper case.</summary>
    public class DefaultTypeNameGenerator : ITypeNameGenerator
    {
        /// <summary>Generates the type name for the given schema respecting the reserved type names.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <param name="reservedTypeNames">The reserved type names.</param>
        /// <returns>The type name.</returns>
        public virtual string Generate(JsonSchema4 schema, string typeNameHint, ICollection<string> reservedTypeNames)
        {
            if (string.IsNullOrEmpty(typeNameHint) && !string.IsNullOrEmpty(schema.DocumentPath))
                typeNameHint = schema.DocumentPath.Replace("\\", "/").Split('/').Last();

            var typeName = Generate(schema, typeNameHint);
            if (string.IsNullOrEmpty(typeName) || reservedTypeNames.Contains(typeName))
                typeName = GenerateTypeName(typeNameHint, reservedTypeNames);

            return typeName
                .Replace("[", "Of")
                .Replace("]", string.Empty)
                .Replace(",", "And")
                .Replace(" ", string.Empty);
        }

        /// <summary>Generates the type name for the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        protected virtual string Generate(JsonSchema4 schema, string typeNameHint)
        {
            var lastSegment = typeNameHint?.Split('.').Last();
            return ConversionUtilities.ConvertToUpperCamelCase(lastSegment ?? "Anonymous", true);
        }

        private string GenerateTypeName(string typeNameHint, ICollection<string> reservedTypeNames)
        {
            if (!string.IsNullOrEmpty(typeNameHint))
            {
                typeNameHint = typeNameHint.Split('.').Last();
                
                if (!reservedTypeNames.Contains(typeNameHint))
                    return typeNameHint;

                var count = 1;
                do
                {
                    count++;
                } while (reservedTypeNames.Contains(typeNameHint + count));

                return typeNameHint + count;
            }

            return GenerateTypeName("Anonymous", reservedTypeNames);
        }
    }
}