//-----------------------------------------------------------------------
// <copyright file="CSharpJsonConverters.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NJsonSchema.CodeGeneration.CSharp.Templates;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Generates JSON converter code.</summary>
    public static class CSharpJsonConverters
    {
        /// <summary>Generates the JSON converter code.</summary>
        /// <param name="jsonConverterTypes">The Json.NET converter types.</param>
        /// <returns>The code.</returns>
        public static string GenerateConverters(IEnumerable<string> jsonConverterTypes)
        {
            return jsonConverterTypes != null && jsonConverterTypes.Any() ?
                ", new JsonConverter[] { " + string.Join(", ", jsonConverterTypes.Select(c => "new " + c + "()")) + " }" :
                string.Empty;
        }
    }
}
