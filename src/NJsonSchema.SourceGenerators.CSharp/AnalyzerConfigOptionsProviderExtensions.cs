using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using static NJsonSchema.SourceGenerators.CSharp.GeneratorConfigurationKeys;

namespace NJsonSchema.SourceGenerators.CSharp
{
    /// <summary>
    /// Extension methods for <see cref="AnalyzerConfigOptionsProvider"/>.
    /// </summary>
    public static class AnalyzerConfigOptionsProviderExtensions
    {
        /// <summary>
        /// Converts options from <see cref="AnalyzerConfigOptionsProvider"/> to <see cref="JsonSchemaSourceGeneratorConfig"/>.
        /// </summary>
        /// <param name="analyzerConfigOptionsProvider">Analyzer options provider.</param>
        /// <param name="additionalText">Additional text item.</param>
        /// <returns></returns>
        public static JsonSchemaSourceGeneratorConfig ToJsonSchemaSourceGeneratorConfig(
           this AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
           AdditionalText additionalText)
        {
            var additionTextOptions = analyzerConfigOptionsProvider.GetOptions(additionalText);
            var globalOptions = analyzerConfigOptionsProvider.GlobalOptions;

            return new JsonSchemaSourceGeneratorConfig(
                        GetOptionBoolean(GenerateOptionalPropertiesAsNullable, globalOptions, additionTextOptions),
                        GetOption(Namespace, globalOptions, additionTextOptions),
                        GetAdditionalFileOption(TypeNameHint, additionTextOptions),
                        GetAdditionalFileOption(FileName, additionTextOptions));
        }

        private static bool? GetOptionBoolean(
            string key,
            AnalyzerConfigOptions globalOptions,
            AnalyzerConfigOptions additionTextOptions)
        {
            var option = GetOption(key, globalOptions, additionTextOptions);

            return bool.TryParse(option, out var result) ? result : null;
        }

        private static string? GetOption(
            string key,
            AnalyzerConfigOptions globalOptions,
            AnalyzerConfigOptions additionTextOptions)
        {
            return GetAdditionalFileOption(key, additionTextOptions) ?? GetGlobalOption(key, globalOptions);
        }

        private static string? GetGlobalOption(
            string key,
            AnalyzerConfigOptions globalOptions)
        {
            globalOptions.TryGetValue($"build_property.{key}", out var value);
            return value;
        }

        private static string? GetAdditionalFileOption(
            string key,
            AnalyzerConfigOptions additionTextOptions)
        {
            additionTextOptions.TryGetValue($"build_metadata.AdditionalFiles.{key}", out var value);
            return value;
        }
    }
}
