using Microsoft.CodeAnalysis;
using Xunit.Abstractions;
using static NJsonSchema.SourceGenerators.CSharp.GeneratorConfigurationKeys;

namespace NJsonSchema.SourceGenerators.CSharp.Tests
{
    public class JsonSchemaSourceGeneratorTests(ITestOutputHelper output) : TestsBase(output)
    {
        [Fact]
        public void When_no_additional_files_specified_then_no_source_is_generated()
        {
            var (compilation, outputDiagnostics) = GetGeneratedOutput(null, []);

            Assert.Empty(outputDiagnostics);
            Assert.Empty(compilation.SyntaxTrees);
        }

        [Fact]
        public void When_invalid_path_specified_then_nothing_is_generated()
        {
            var (compilation, outputDiagnostics) = GetGeneratedOutput(null, [new AdditionalTextStub("not_existing.json")]);

            Assert.NotEmpty(outputDiagnostics);
            Assert.Single(outputDiagnostics);
            var outputDiagnostic = outputDiagnostics[0];
            Assert.Equal("NJSG001", outputDiagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, outputDiagnostic.Severity);

            Assert.Empty(compilation.SyntaxTrees);
        }

        [Fact]
        public void When_without_config_then_generated_with_default_values()
        {
            var firstName = "Alex";
            var defaultNamespace = "MyNamespace";

            string source = $@"
namespace Example
{{
    class Test
    {{
        public static string RunTest()
        {{
            var json = new {defaultNamespace}.Person()
                {{
                    FirstName = ""{firstName}""
                }};
            return json.FirstName;
        }}
    }}
}}";
            var (compilation, outputDiagnostics) = GetGeneratedOutput(source, [new AdditionalTextStub("References/schema.json")]);

            Assert.Empty(outputDiagnostics);

            Assert.Equal(2, compilation.SyntaxTrees.Count());

            Assert.Equal(firstName, RunTest(compilation));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("false", false)]
        [InlineData("False", false)]
        [InlineData("true", true)]
        [InlineData("True", true)]
        public void When_GenerateOptionalPropertiesAsNullable_in_global_options_then_generate_according_to_config(
            string generateOptionalPropertiesAsNullable,
            bool shouldBeNullable)
        {
            string source = $@"
namespace Example
{{
    class Test
    {{
        public static string RunTest()
        {{
            var json = new MyNamespace.Person();
            return System.Convert.ToString(json.Age);
        }}
    }}
}}";
            var globalOptions = new Dictionary<string, string>
            {
                { GenerateOptionalPropertiesAsNullable, generateOptionalPropertiesAsNullable }
            };
            var (compilation, outputDiagnostics) = GetGeneratedOutput(
                source,
                [new AdditionalTextStub("References/schema.json")],
                globalOptions);

            Assert.Empty(outputDiagnostics);

            Assert.Equal(2, compilation.SyntaxTrees.Count());

            var expectedOutput = shouldBeNullable ? string.Empty : "0";
            Assert.Equal(expectedOutput, RunTest(compilation));
        }

        [Theory]
        [InlineData(null, "true", true)]
        [InlineData("false", "true", false)]
        [InlineData("False", "true", false)]
        [InlineData("true", "false", true)]
        [InlineData("True", "false", true)]
        public void When_GenerateOptionalPropertiesAsNullable_in_additional_files_then_generate_according_to_config_and_override_global_if_possible(
            string generateOptionalPropertiesAsNullableAdditionalFiles,
            string generateOptionalPropertiesAsNullableGlobalOptions,
            bool shouldBeNullable)
        {
            string source = $@"
namespace Example
{{
    class Test
    {{
        public static string RunTest()
        {{
            var json = new MyNamespace.Person();
            return System.Convert.ToString(json.Age);
        }}
    }}
}}";
            var globalOptions = new Dictionary<string, string>
            {
                { GenerateOptionalPropertiesAsNullable, generateOptionalPropertiesAsNullableGlobalOptions }
            };
            var additionalFilesOptions = new Dictionary<string, string>
            {
                { GenerateOptionalPropertiesAsNullable, generateOptionalPropertiesAsNullableAdditionalFiles }
            };
            var (compilation, outputDiagnostics) = GetGeneratedOutput(
                source,
                [new AdditionalTextStub("References/schema.json", additionalFilesOptions)],
                globalOptions);

            Assert.Empty(outputDiagnostics);

            Assert.Equal(2, compilation.SyntaxTrees.Count());

            var expectedOutput = shouldBeNullable ? string.Empty : "0";
            Assert.Equal(expectedOutput, RunTest(compilation));
        }

        [Theory]
        [InlineData(null, null, "MyNamespace")]
        [InlineData("", null, "MyNamespace")]
        [InlineData(null, "", "MyNamespace")]
        [InlineData(null, "NamespaceFromGlobalOptions", "NamespaceFromGlobalOptions")]
        [InlineData("NamespaceFromLocalOptions", null, "NamespaceFromLocalOptions")]
        [InlineData("NamespaceFromLocalOptions", "NamespaceFromGlobalOptions", "NamespaceFromLocalOptions")]
        public void When_Namespace_in_config_then_generate(
            string namespaceAdditionalFiles,
            string namespaceGlobalOptions,
            string expectedNamespace)
        {
            string source = $@"
namespace Example
{{
    class Test
    {{
        public static string RunTest()
        {{
            var json = new {expectedNamespace}.Person();
            return ""compiled"";
        }}
    }}
}}";
            var globalOptions = new Dictionary<string, string>
            {
                { Namespace, namespaceGlobalOptions }
            };
            var additionalFilesOptions = new Dictionary<string, string>
            {
                { Namespace, namespaceAdditionalFiles }
            };
            var (compilation, outputDiagnostics) = GetGeneratedOutput(
                source,
                [new AdditionalTextStub("References/schema.json", additionalFilesOptions)],
                globalOptions);

            Assert.Empty(outputDiagnostics);

            Assert.Equal(2, compilation.SyntaxTrees.Count());

            Assert.Equal("compiled", RunTest(compilation));
        }

        [Theory]
        [InlineData(null, null, "Person")]
        [InlineData(null, "", "Person")]
        [InlineData("", null, "Person")]
        [InlineData(null, "ShouldNotOverride", "Person")]
        [InlineData("ShouldOverride", null, "ShouldOverride")]
        public void When_TypeNameHint_in_config_then_generate_using_additional_files_only(
            string typeNameHintAdditionalFiles,
            string typeNameHintGlobalOptions,
            string expectedTypeName)
        {
            string source = $@"
namespace Example
{{
    class Test
    {{
        public static string RunTest()
        {{
            var json = new MyNamespace.{expectedTypeName}();
            return ""compiled"";
        }}
    }}
}}";
            var globalOptions = new Dictionary<string, string>
            {
                { TypeNameHint, typeNameHintGlobalOptions }
            };
            var additionalFilesOptions = new Dictionary<string, string>
            {
                { TypeNameHint, typeNameHintAdditionalFiles }
            };
            var (compilation, outputDiagnostics) = GetGeneratedOutput(
                source,
                [new AdditionalTextStub("References/schema.json", additionalFilesOptions)],
                globalOptions);

            Assert.Empty(outputDiagnostics);

            Assert.Equal(2, compilation.SyntaxTrees.Count());

            Assert.Equal("compiled", RunTest(compilation));
        }

        [Theory]
        [InlineData(null, null, "NJsonSchemaGenerated.g.cs")]
        [InlineData("", null, "NJsonSchemaGenerated.g.cs")]
        [InlineData(null, "", "NJsonSchemaGenerated.g.cs")]
        [InlineData(null, "ShouldNotOverride.g.cs", "NJsonSchemaGenerated.g.cs")]
        [InlineData("ShouldOverride.g.cs", null, "ShouldOverride.g.cs")]
        public void When_FileName_in_config_then_generate_using_additional_files_only(
            string fileNameAdditionalFiles,
            string fileNameGlobalOptions,
            string expectedFileName)
        {
            var globalOptions = new Dictionary<string, string>
            {
                { FileName, fileNameGlobalOptions }
            };
            var additionalFilesOptions = new Dictionary<string, string>
            {
                { FileName, fileNameAdditionalFiles }
            };
            var (compilation, outputDiagnostics) = GetGeneratedOutput(
                null,
                [new AdditionalTextStub("References/schema.json", additionalFilesOptions)],
                globalOptions);

            Assert.Empty(outputDiagnostics);

            Assert.Single(compilation.SyntaxTrees);
            var syntaxTree = compilation.SyntaxTrees.First();
            Assert.EndsWith(expectedFileName, syntaxTree.FilePath);
        }
    }
}