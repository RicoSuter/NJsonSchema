namespace NJsonSchema.CodeGeneration.TypeScript.Tests;

public static class VerifyHelper
{
    /// <summary>
    /// Helper to run verify tests with sane defaults.
    /// </summary>
    public static SettingsTask Verify(string output)
    {
        return Verifier
            .Verify(output)
            .ScrubLinesContaining(StringComparison.OrdinalIgnoreCase, "Generated using the NSwag toolchain")
            .UseDirectory("Snapshots");
    }
}