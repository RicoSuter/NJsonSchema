namespace NJsonSchema.SourceGenerators.CSharp
{
    /// <summary>
    /// Contains contstants with configuration keys used for Source Generation.
    /// </summary>
    public static class GeneratorConfigurationKeys
    {
        private const string _prefix = "NJsonSchema_";

        /// <summary>
        /// Indicating whether optional schema properties (not required) are generated as nullable properties.
        /// </summary>
        public const string GenerateOptionalPropertiesAsNullable = _prefix + "GenerateOptionalPropertiesAsNullable";

        /// <summary>
        /// .NET namespace of the generated types.
        /// </summary>
        public const string Namespace = _prefix + "Namespace";

        /// <summary>
        /// C# class name.
        /// </summary>
        public const string TypeNameHint = _prefix + "TypeNameHint";

        /// <summary>
        /// Name of the file containing generated classes.
        /// </summary>
        public const string FileName = _prefix + "FileName";
    }
}
