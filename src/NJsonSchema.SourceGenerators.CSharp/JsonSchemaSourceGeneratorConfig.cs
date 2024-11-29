namespace NJsonSchema.SourceGenerators.CSharp
{
    /// <summary>
    /// Configuration for source generator.
    /// </summary>
    /// <param name="GenerateOptionalPropertiesAsNullable">Value indicating whether optional schema properties (not required) are generated as nullable properties (default: false).</param>
    /// <param name="Namespace">.NET namespace of the generated types (default: MyNamespace).</param>
    /// <param name="TypeNameHint">C# class name.</param>
    /// <param name="FileName">Name of the file containing generated classes.</param>
    public record JsonSchemaSourceGeneratorConfig(
        bool? GenerateOptionalPropertiesAsNullable,
        string? Namespace,
        string? TypeNameHint,
        string? FileName)
    {
    }
}
