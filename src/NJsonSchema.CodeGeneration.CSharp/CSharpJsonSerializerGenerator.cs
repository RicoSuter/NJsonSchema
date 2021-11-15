//-----------------------------------------------------------------------
// <copyright file="CSharpJsonSerializerGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
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
            var jsonConverters = GetJsonConverters(settings, additionalJsonConverters);
            var hasJsonConverters = jsonConverters.Any();

            return GenerateForJsonLibrary(settings, jsonConverters, hasJsonConverters);
        }

        /// <summary>Generates the JSON converters array code.</summary>
        /// <param name="settings">The settings.</param>
        /// <param name="additionalJsonConverters">The additional JSON converters.</param>
        /// <returns>The code.</returns>
        public static string GenerateJsonConvertersArrayCode(CSharpGeneratorSettings settings, IEnumerable<string> additionalJsonConverters)
        {
            var jsonConverters = GetJsonConverters(settings, additionalJsonConverters);

            return GenerateConverters(jsonConverters, settings.JsonLibrary);
        }

        private static List<string> GetJsonConverters(CSharpGeneratorSettings settings, IEnumerable<string> additionalJsonConverters)
        {
            return (settings.JsonConverters ?? Array.Empty<string>()).Concat(additionalJsonConverters ?? Array.Empty<string>()).ToList();
        }

        private static string GenerateForJsonLibrary(CSharpGeneratorSettings settings, List<string> jsonConverters, bool hasJsonConverters)
        {
            var useSettingsTransformationMethod = !string.IsNullOrEmpty(settings.JsonSerializerSettingsTransformationMethod);
            switch (settings.JsonLibrary)
            {
                case CSharpJsonLibrary.NewtonsoftJson:
                    if (settings.HandleReferences || useSettingsTransformationMethod)
                    {
                        return
                            (useSettingsTransformationMethod ? settings.JsonSerializerSettingsTransformationMethod + "(" : string.Empty) +
                            "new Newtonsoft.Json.JsonSerializerSettings { " +
                            (settings.HandleReferences
                                ? "PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.All"
                                : string.Empty) +
                            (hasJsonConverters
                                ? (settings.HandleReferences ? ", " : string.Empty) + "Converters = " + GenerateConverters(jsonConverters, settings.JsonLibrary)
                                : string.Empty) +
                            " }" +
                            (useSettingsTransformationMethod ? ")" : string.Empty);
                    }
                    else
                    {
                        if (hasJsonConverters)
                        {
                            return GenerateConverters(jsonConverters, settings.JsonLibrary);
                        }
                        else
                        {
                            return "new Newtonsoft.Json.JsonSerializerSettings()";
                        }
                    }

                case CSharpJsonLibrary.SystemTextJson:
                    if (useSettingsTransformationMethod || hasJsonConverters)
                    {
                        return
                            (useSettingsTransformationMethod ? settings.JsonSerializerSettingsTransformationMethod + "(" : string.Empty) +
                            "new System.Text.Json.JsonSerializerOptions()" +
                            (useSettingsTransformationMethod ? ")" : string.Empty);
                    }
                    else
                    {
                        return "new System.Text.Json.JsonSerializerOptions()";
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
