namespace NJsonSchema.Generation
{
    /// <summary> Settings for generating sample json data.</summary>
    public class SampleJsonDataGeneratorSettings
    {
        /// <summary>Gets or sets a value indicating whether to generate optional properties (default: true).</summary>
        public bool GenerateOptionalProperties { get; set; } = true;

        /// <summary>Gets or sets a value indicating the max level of recursion the generator is allowed to perform (default: 3)</summary>
        public int MaxRecursionLevel { get; set; } = 3;
    }
}
