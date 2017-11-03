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
        /// <param name="settings">The settings.</param>
        /// <param name="additionalJsonConverters">The additional JSON converters.</param>
        /// <returns>The code.</returns>
        public static string GenerateJsonSerializerParameterCode(CSharpGeneratorSettings settings, IEnumerable<string> additionalJsonConverters)
        {
            var jsonConverters = (settings.JsonConverters ?? new string[0]).Concat(additionalJsonConverters ?? new string[0]).ToList();
            var hasJsonConverters = jsonConverters.Any();

            var useSettingsTransformationMethod = !string.IsNullOrEmpty(settings.JsonSerializerSettingsTransformationMethod);
            if (settings.HandleReferences || useSettingsTransformationMethod)
            {
                return ", " +
                       (useSettingsTransformationMethod ? settings.JsonSerializerSettingsTransformationMethod + "(" : string.Empty) +
                       "new Newtonsoft.Json.JsonSerializerSettings { " +
                       (settings.HandleReferences
                           ? "PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.All"
                           : string.Empty) +
                       (hasJsonConverters
                           ? (settings.HandleReferences ? ", " : string.Empty) + "Converters = " + GenerateConverters(jsonConverters)
                           : string.Empty) +
                       " }" +
                       (useSettingsTransformationMethod ? ")" : string.Empty);
            }
            else
            {
                if (hasJsonConverters)
                    return ", " + GenerateConverters(jsonConverters);
                else
                    return string.Empty;
            }
        }

        private static string GenerateConverters(List<string> jsonConverters)
        {
            if (jsonConverters.Any())
                return "new Newtonsoft.Json.JsonConverter[] { " + string.Join(", ", jsonConverters.Select(c => "new " + c + "()")) + " }";
            else
                return string.Empty;
        }
    }
}
