//-----------------------------------------------------------------------
// <copyright file="CSharpJsonSerializerGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Generates JSON converter code.</summary>
    public static class CSharpJsonSerializerGenerator
    {
        /// <summary>Generates the JSON converter code.</summary>
        /// <param name="handleReferences">if set to <c>true</c> uses preserve references handling.</param>
        /// <param name="jsonConverterTypes">The Json.NET converter types.</param>
        /// <returns>The code.</returns>
        public static string GenerateJsonSerializerParameterCode(bool handleReferences, ICollection<string> jsonConverterTypes)
        {
            if (handleReferences)
            {
                if (jsonConverterTypes != null && jsonConverterTypes.Any())
                    return ", new Newtonsoft.Json.JsonSerializerSettings { " +
                           "PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.All, " +
                           "Converters = " + GenerateConverters(jsonConverterTypes) +
                           " }";
                else
                    return
                        ", new Newtonsoft.Json.JsonSerializerSettings { " +
                        "PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.All }";
            }
            else
            {
                if (jsonConverterTypes != null && jsonConverterTypes.Any())
                    return ", " + GenerateConverters(jsonConverterTypes);
                else
                    return string.Empty;
            }
        }

        private static string GenerateConverters(ICollection<string> jsonConverterTypes)
        {
            if (jsonConverterTypes != null && jsonConverterTypes.Any())
                return "new Newtonsoft.Json.JsonConverter[] { " + string.Join(", ", jsonConverterTypes.Select(c => "new " + c + "()")) + " }";
            else
                return string.Empty;
        }
    }
}
