using System;

namespace NJsonSchema.Validation
{
    /// <summary>Class to configure the behavior of <see cref="JsonSchemaValidator"/>. </summary>
    public class JsonSchemaValidatorOptions
    {
        /// <summary>Whether to ignore casing for object properties.</summary>
        public bool IgnorePropertyNameCase { get; set; }
    }
}
