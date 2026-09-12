#nullable enable
namespace NJsonSchema.CodeGeneration.TypeScript.Tests;

public class IntegerEnumContractTests
{
    [Theory]
    [InlineData("1e0", "1e0", "1")]
    [InlineData("1.0", "1.0", "1")]
    [InlineData("1e0", "1", "1")]
    [InlineData("1", "1.0", "1")]
    [InlineData("1.0", "1e0", "1")]
    [InlineData("8.0", "8.0", "8")]
    [InlineData("8e0", "8e0", "8")]
    [InlineData("1099511627776.0", "1099511627776.0", "1099511627776")]
    [InlineData("1099511627776", "1099511627776", "1099511627776")]
    [InlineData("9007199254740993", "9007199254740993", "9007199254740993")]
    [InlineData("9007199254740993.0", "9007199254740993.0", "9007199254740993")]
    [InlineData("9223372036854775807", "9223372036854775807", "9223372036854775807")]
    [InlineData("-8e0", "-8.0", "-8")]
    public async Task Integral_json_spellings_preserve_members_and_defaults(string literal, string defaultLiteral, string expected)
    {
        foreach (var named in new[] { false, true })
        foreach (var flags in new[] { false, true })
        {
            // Arrange
            var names = named ? """, "x-enumNames":["Chosen"]""" : "";
            var schema = await JsonSchema.FromJsonAsync($$$$"""{"type":"object","properties":{"value":{"type":"integer","format":"int64","enum":[{{{{literal}}}}],"default":{{{{defaultLiteral}}}},"x-enumFlags":{{{{flags.ToString().ToLowerInvariant()}}}} {{{{names}}}}}}}""");
            var member = named ? "Chosen" : expected.StartsWith("-", StringComparison.Ordinal) ? "__" + expected.Substring(1) : "_" + expected;

            // Act
            var output = new TypeScriptGenerator(schema).GenerateFile("Container");

            // Assert
            Assert.Contains(member + " = " + expected, output);
            TypeScriptCompiler.AssertCompile(output);
            Assert.Contains("= ContainerValue." + member, output);
            if (decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture) <= 9007199254740991m)
            {
                AssertInitializedValue(output, expected);
            }
        }
    }
    [Theory]
    [InlineData("1e1000000")]
    [InlineData("-1e1000000")]
    [InlineData("1e-1000000")]
    public async Task Unrepresentable_exponents_are_not_expanded_or_rounded(string literal)
    {
        // Arrange
        var schema = await JsonSchema.FromJsonAsync("{\"type\":\"integer\",\"enum\":[" + literal + "]}");

        // Act
        var item = new NJsonSchema.CodeGeneration.TypeScript.Models.EnumTemplateModel("Value", schema, new TypeScriptGeneratorSettings()).Enums.Single();

        // Assert
        Assert.Equal(literal, item.Value);
    }

    [Fact]
    public async Task Custom_name_hints_and_values_match_between_declaration_and_default()
    {
        // Arrange
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"integer","enum":[1e0],"default":1.0}}}""");
        var names = new PrefixEnumNameGenerator();
        var settings = new TypeScriptGeneratorSettings { EnumNameGenerator = names };

        // Act
        var output = new TypeScriptGenerator(schema, settings).GenerateFile("Container");

        // Assert
        Assert.Contains("Prefix_1e0 = 1", output);
        Assert.Contains("ContainerValue.Prefix_1e0", output);
        TypeScriptCompiler.AssertCompile(output);
        Assert.All(names.Inputs, input =>
        {
            Assert.Equal("_1e0", input.Name);
            Assert.Equal("1e0", Assert.IsType<System.Text.Json.JsonElement>(input.Value).GetRawText());
        });
    }

    [Fact]
    public async Task Explicit_numeric_looking_names_are_preserved()
    {
        // Arrange
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"integer","enum":[1e0],"default":1.0,"x-enumNames":["1.0"]}}}""");

        // Act
        var output = new TypeScriptGenerator(schema).GenerateFile("Container");

        // Assert
        Assert.Contains("_1_0 = 1", output);
        Assert.Contains("ContainerValue._1_0", output);
        TypeScriptCompiler.AssertCompile(output);
    }

    private sealed class PrefixEnumNameGenerator : IEnumNameGenerator
    {
        public List<(string? Name, object? Value)> Inputs { get; } = [];

        public string Generate(int index, string? name, object? value, JsonSchema schema)
        {
            Inputs.Add((name, value));
            return "Prefix" + name;
        }
    }

    private static void AssertInitializedValue(string source, string expected)
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "node",
                WorkingDirectory = Path.GetFullPath("../../../../src/NJsonSchema.CodeGeneration.TypeScript.Tests"),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        process.StartInfo.Arguments = "-e \"const ts=require('typescript');eval(ts.transpile(require('fs').readFileSync(0,'utf8'),{module:ts.ModuleKind.CommonJS}));\"";
        process.Start();
        process.StandardInput.Write(source + "\nif(new Container().value !== " + expected + ") throw new Error('Incorrect initialized enum value');");
        process.StandardInput.Close();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, error);
    }

}
