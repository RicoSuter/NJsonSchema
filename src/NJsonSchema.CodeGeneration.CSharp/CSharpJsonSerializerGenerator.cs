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
        /// <returns>The code.</returns>
        public static string GenerateJsonSerializerParameterCode(CSharpGeneratorSettings settings)
        {
            var useSettingsTransformationMethod = !string.IsNullOrEmpty(settings.JsonSerializerSettingsTransformationMethod);
            if (settings.HandleReferences || useSettingsTransformationMethod)
            {
                var hasCustomConverters = settings.JsonConverters != null && settings.JsonConverters.Any();
                return ", " +
                       (useSettingsTransformationMethod ? settings.JsonSerializerSettingsTransformationMethod + "(" : string.Empty) +
                       "new Newtonsoft.Json.JsonSerializerSettings { " +
                       (settings.HandleReferences
                           ? "PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.All, "
                           : string.Empty) +
                       (hasCustomConverters
                           ? "Converters = " + GenerateConverters(settings.JsonConverters) + ", "
                           : string.Empty) +
                       " }" +
                       (useSettingsTransformationMethod ? ")" : string.Empty);
            }
            else
            {
                if (settings.JsonConverters != null && settings.JsonConverters.Any())
                    return ", " + GenerateConverters(settings.JsonConverters);
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
