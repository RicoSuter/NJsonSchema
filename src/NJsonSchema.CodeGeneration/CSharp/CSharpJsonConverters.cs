//-----------------------------------------------------------------------
// <copyright file="CSharpJsonConverters.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Generates JSON converter code.</summary>
    public static class CSharpJsonConverters
    {
        // TODO: Use this converter in CS template

        /// <summary>Generates the JSON converter code.</summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The code.</returns>
        public static string GenerateConverters(CSharpGeneratorSettings settings)
        {
            return settings.JsonConverters != null && settings.JsonConverters.Any() ?
                ", new JsonConverter[] {" + string.Join(", ", settings.JsonConverters.Select(c => "new " + c + "()")) + "}" :
                string.Empty;
        }
    }
}
