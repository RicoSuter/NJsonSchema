using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Reflection;
using Xunit.Abstractions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NJsonSchema.SourceGenerators.CSharp.Tests
{
    /// <summary>
    /// Helps with AdditionalFiles by exposing them as text files.
    /// </summary>
    public class AdditionalTextStub(string path, Dictionary<string, string> options = null) : AdditionalText
    {
        public Dictionary<string, string> Options { get; } = options;

        public override string Path => path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class TestsBase(ITestOutputHelper output)
    {
        private static List<MetadataReference> _metadataReferences;
        private static readonly object Lock = new object();

        /// <summary>
        /// Retrieves and caches referenced assemblies, so that tested compilations can make use of them.
        /// </summary>
        private static List<MetadataReference> MetadataReferences
        {
            get
            {
                lock (Lock)
                {
                    if (_metadataReferences == null)
                    {
                        _metadataReferences = [];
                        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                        foreach (var assembly in assemblies)
                        {
                            if (!assembly.IsDynamic)
                            {
                                _metadataReferences.Add(MetadataReference.CreateFromFile(assembly.Location));
                            }
                        }
                    }
                }

                return _metadataReferences;
            }
        }

        /// <summary>
        /// Takes compiled input and runs the code.
        /// </summary>
        /// <param name="compilation">Compiled code</param>
        /// <param name="diagnostics">If specified, this list will be populated with diagnostics that can be used for debugging</param>
        /// <returns></returns>
        protected string RunTest(Compilation compilation, List<Diagnostic> diagnostics = null)
        {
            if (compilation == null)
            {
                throw new ArgumentException($"Argument {nameof(compilation)} must not be null");
            }

            // Get the compilation and load the assembly
            using var memoryStream = new MemoryStream();
            EmitResult result = compilation.Emit(memoryStream);

            if (result.Success)
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(memoryStream.ToArray());

                // We assume the generated code has a type Example.Test that contains a method RunTest(Async), to run the test
                Type testClassType = assembly.GetType("Example.Test");
                var method = testClassType?.GetMethod("RunTest") ?? testClassType?.GetMethod("RunTestAsync");
                if (method == null)
                {
                    return "-- could not find test method --";
                }

                // Actually invoke the method and return the result
                var resultObj = method.Invoke(null, []);
                if (resultObj is not string stringResult)
                {
                    return "-- result was not a string --";
                }

                // Log the test output, for debugging purposes
                output.WriteLine($"Generated test output:\r\n===\r\n{stringResult}\r\n===\r\n");

                return stringResult;
            }

            // If diagnostics list is specified, fill it with any diagnostics. If not, fail the unit test directly.
            if (diagnostics == null)
            {
                Assert.Fail(
                    $"Compilation did not succeed:\r\n{string.Join("\r\n", result.Diagnostics.Select(d => $"{Enum.GetName(typeof(DiagnosticSeverity), d.Severity)} ({d.Location}) - {d.GetMessage()}"))}");
            }
            else
            {
                diagnostics.AddRange(result.Diagnostics);
            }

            return null;
        }

        /// <summary>
        /// Build a compilation and run the source generator.
        /// </summary>
        /// <param name="source">Input source</param>
        /// <param name="additionalTexts">Additional files that must be added to the compilation</param>
        /// <param name="globalOptions">Global options</param>
        /// <returns></returns>
        protected (Compilation, IImmutableList<Diagnostic>) GetGeneratedOutput(
            string source,
            AdditionalTextStub[] additionalTexts,
            Dictionary<string, string> globalOptions = null)
        {
            List<SyntaxTree> syntaxTrees = null;
            if (source != null)
            {
                syntaxTrees = [CSharpSyntaxTree.ParseText(source)];
            }

            var references = MetadataReferences;

            var compilation = CSharpCompilation.Create(
                    "TestImplementation",
                    syntaxTrees,
                    references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var generator = new JsonSchemaSourceGenerator();

            var analyzerConfigOptionsProvider = new TestAnalyzerConfigOptionsProvider(additionalTexts, globalOptions);

            CSharpGeneratorDriver.Create(
                [generator.AsSourceGenerator()],
                additionalTexts,
                new CSharpParseOptions(),
                analyzerConfigOptionsProvider)
                .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);

            var output1 = outputCompilation
                .SyntaxTrees
                .Where(t => t.GetText().ToString().Contains("<auto-generated>"))
                .ToArray();

            if (output1.Length > 0)
            {
                output.WriteLine($"Generated code:\r\n===\r\n{string.Join("\r\n===\r\n", output1.AsEnumerable())}\r\n===\r\n");
            }

            return (outputCompilation, generateDiagnostics);
        }
    }

    public class TestAnalyzerConfigOptionsProvider(
        IEnumerable<AdditionalTextStub> additionalTexts,
        Dictionary<string, string> globalOptions = null)
        : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new AdditionalFilesTestAnalyzerConfigOptions();

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return new AdditionalFilesTestAnalyzerConfigOptions(additionalTexts.FirstOrDefault(x => x.Path == textFile.Path)?.Options);
        }

        public override AnalyzerConfigOptions GlobalOptions { get; } = new GlobalTestAnalyzerConfigOptions(globalOptions);

        private abstract class TestAnalyzerConfigOptions(Dictionary<string, string> options = null)
            : AnalyzerConfigOptions
        {
            private readonly Dictionary<string, string> _options = options ?? [];

            public override bool TryGetValue(string key, out string value) => _options.TryGetValue(key, out value);
        }

        private class AdditionalFilesTestAnalyzerConfigOptions(Dictionary<string, string> options = null)
            : TestAnalyzerConfigOptions(options?.Select(x => ("build_metadata.AdditionalFiles." + x.Key, x.Value))
                .ToDictionary(pair => pair.Item1, p => p.Value));

        private class GlobalTestAnalyzerConfigOptions(Dictionary<string, string> options) : TestAnalyzerConfigOptions(
            options?.Select(x => ("build_property." + x.Key, x.Value)).ToDictionary(pair => pair.Item1, p => p.Value));
    }
}
