using System.Runtime.CompilerServices;

namespace NJsonSchema.CodeGeneration.Tests;

public static class VerifyHelper
{
    /// <summary>
    /// Helper to run verify tests with sane defaults.
    /// </summary>
    public static SettingsTask Verify(string output, [CallerFilePath] string sourceFile = "")
    {
        return Verifier
            .Verify(output, sourceFile: sourceFile)
            .ScrubLinesContaining(
                StringComparison.OrdinalIgnoreCase,
                "Generated using the NSwag toolchain",
                "Generated using the NJsonSchema",
                "[System.CodeDom.Compiler.GeneratedCode(\"NJsonSchema\"")
            .UseDirectory("Snapshots")
            .AutoVerify(includeBuildServer: false);
    }
}