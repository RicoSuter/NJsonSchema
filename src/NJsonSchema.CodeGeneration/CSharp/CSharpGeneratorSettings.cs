//-----------------------------------------------------------------------
// <copyright file="CSharpGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>The generator settings.</summary>
    public class CSharpGeneratorSettings
    {
        /// <summary>Initializes a new instance of the <see cref="CSharpGeneratorSettings"/> class.</summary>
        public CSharpGeneratorSettings()
        {
            RequiredPropertiesMustBeDefined = true; 
        }

        /// <summary>Gets or sets the namespace.</summary>
        public string Namespace { get; set; }

        /// <summary>Gets or sets a value indicating whether a required property must be defined in JSON 
        /// (sets Required.Always when the property is required) (default: true).</summary>
        public bool RequiredPropertiesMustBeDefined { get; set; }
    }
}