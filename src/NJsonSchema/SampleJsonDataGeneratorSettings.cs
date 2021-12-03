//-----------------------------------------------------------------------
// <copyright file="SampleJsonDataGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema
{
    /// <summary> Settings for generating sample json data.</summary>
    public class SampleJsonDataGeneratorSettings
    {
        /// <summary>Gets or sets a value indicating whether to generate optional properties (default: true).</summary>
        public bool GenerateOptionalProperties { get; set; } = true;
    }
}
