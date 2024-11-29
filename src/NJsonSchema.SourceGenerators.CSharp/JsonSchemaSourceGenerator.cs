using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using NJsonSchema.CodeGeneration.CSharp;
using System.Text;

namespace NJsonSchema.SourceGenerators.CSharp
{
    /// <summary>
    /// Generates C# classes from JSON schema
    /// </summary>
    [Generator]
    public class JsonSchemaSourceGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// Configure source generation.
        /// </summary>
        /// <param name="context">Source generator context.</param>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var emitLoggingPipeline = context.AdditionalTextsProvider
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Select((pair, _) => (pair.Left.Path, Config: pair.Right.ToJsonSchemaSourceGeneratorConfig(pair.Left)));

            context.RegisterSourceOutput(emitLoggingPipeline,
                (ctx, pair) =>
                {

                    (string path, JsonSchemaSourceGeneratorConfig config) = pair;
                    var settings = new CSharpGeneratorSettings();

                    if (config.GenerateOptionalPropertiesAsNullable.HasValue)
                    {
                        settings.GenerateOptionalPropertiesAsNullable = config.GenerateOptionalPropertiesAsNullable.Value;
                    }

                    if (!string.IsNullOrEmpty(config.Namespace))
                    {
                        settings.Namespace = config.Namespace!;
                    }

                    try
                    {
                        var schema = JsonSchema.FromFileAsync(path).GetAwaiter().GetResult();
                        var generator = new CSharpGenerator(schema, settings);
                        var classesFileContent = string.IsNullOrEmpty(config.TypeNameHint)
                            ? generator.GenerateFile()
                            : generator.GenerateFile(config.TypeNameHint!);

                        var fileName = string.IsNullOrEmpty(config.FileName) ? "NJsonSchemaGenerated.g.cs" : config.FileName;
                        ctx.AddSource(fileName!, SourceText.From(classesFileContent, Encoding.UTF8));
                    }
                    catch (Exception ex)
                    {
                        ctx.ReportDiagnostic(
                            Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "NJSG001", "Source Generator error", ex.ToString(), "SourceGenerator", DiagnosticSeverity.Error, true),
                                Location.None));
                    }
                });
        }
    }
}
