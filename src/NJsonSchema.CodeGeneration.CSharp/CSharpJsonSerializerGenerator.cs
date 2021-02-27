//-----------------------------------------------------------------------
// <copyright file="CSharpJsonSerializerGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
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

            return GenerateForJsonLibrary(settings, jsonConverters, hasJsonConverters);
        }

        private static string GenerateForJsonLibrary(CSharpGeneratorSettings settings, List<string> jsonConverters, bool hasJsonConverters)
        {
            switch (settings.JsonLibrary)
            {
                case CSharpJsonLibrary.NewtonsoftJson:
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
                                ? (settings.HandleReferences ? ", " : string.Empty) + "Converters = " + GenerateConverters(jsonConverters, settings.JsonLibrary)
                                : string.Empty) +

                            // TODO(newtonsoft.json): Add supporting more options for Newtonsoft.Json (hided for System.Text.Json)
                            // or it can be handled in JsonSerializerSettingsTransformationMethod?

                            " }" +
                            (useSettingsTransformationMethod ? ")" : string.Empty);
                    }
                    else
                    {
                        if (hasJsonConverters)
                        {
                            return ", " + GenerateConverters(jsonConverters, settings.JsonLibrary);
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }

                case CSharpJsonLibrary.SystemTextJson:
                    var useOptionsTransformationMethod = !string.IsNullOrEmpty(settings.JsonSerializerOptionsTransformationMethod);
                    // TODO: add more conditions?
                    if (useOptionsTransformationMethod || hasJsonConverters)
                    {
                        return ", " +
                            (useOptionsTransformationMethod ? settings.JsonSerializerOptionsTransformationMethod + "(" : string.Empty) +
                            "System.Text.Json.JsonSerializerOptions { " +

                            // TODO(system.text.json): Add supporting more options for System.Text.Json (hided for Newtonsoft.Json)
                            // or it can be handled in JsonSerializerOptionsTransformationMethod?
                            //
                            // "AllowTrailingCommas" (bool), "DefaultBufferSize" (int), "IgnoreNullValues" (bool),
                            // "IgnoreReadOnlyProperties" (bool), "MaxDepth" (int), "PropertyNameCaseInsensitive" (bool)
                            // "WriteIndented" (bool), "ReadCommentHandling" (enum - System.Text.Json.JsonCommentHandling),
                            // "PropertyNamingPolicy" (abstract class - System.Text.Json.JsonNamingPolicy), "Encoder" (abstract class - System.Text.Encodings.Web.JavaScriptEncoder),
                            // "DictionaryKeyPolicy" (abstract class - System.Text.Json.JsonNamingPolicy)

                            " }" +
                            (useOptionsTransformationMethod ? ")" : string.Empty) +
                            (hasJsonConverters
                                ? "; var converters = " + GenerateConverters(jsonConverters, settings.JsonLibrary)
                                : string.Empty);
                    }
                    else
                    {
                        return string.Empty;
                    }

                default: // TODO: possibly add more json converters
                    return string.Empty;
            }
        }

        private static string GenerateConverters(List<string> jsonConverters, CSharpJsonLibrary jsonLibrary)
        {
            if (jsonConverters.Any())
            {
                switch (jsonLibrary)
                {
                    case CSharpJsonLibrary.NewtonsoftJson:
                        return "new Newtonsoft.Json.JsonConverter[] { " + string.Join(", ", jsonConverters.Select(c => "new " + c + "()")) + " }";

                    case CSharpJsonLibrary.SystemTextJson:
                        return "new System.Text.Json.Serialization.JsonConverter[] { " + string.Join(", ", jsonConverters.Select(c => "new " + c + "()")) + " }";

                    default: // TODO: possibly add more json converters
                        return string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
