//-----------------------------------------------------------------------
// <copyright file="CSharpGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>The generator settings.</summary>
    public class TypeScriptGeneratorSettings
    {
        /// <summary>Initializes a new instance of the <see cref="TypeScriptGeneratorSettings"/> class.</summary>
        public TypeScriptGeneratorSettings()
        {
            GenerateReadOnlyKeywords = true;
            TypeStyle = TypeScriptTypeStyle.Interface;
        }

        /// <summary>Gets or sets a value indicating whether to generate the readonly keywords (only available in TS 2.0+, default: true).</summary>
        public bool GenerateReadOnlyKeywords { get; set; }

        /// <summary>Gets or sets the type style (default: Interface).</summary>
        public TypeScriptTypeStyle TypeStyle { get; set; }
    }
}