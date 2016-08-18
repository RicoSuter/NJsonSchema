//-----------------------------------------------------------------------
// <copyright file="CodeGeneratorSettingsBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The code generator settings base.</summary>
    public class CodeGeneratorSettingsBase
    {
        /// <summary>Initializes a new instance of the <see cref="CodeGeneratorSettingsBase"/> class.</summary>
        public CodeGeneratorSettingsBase()
        {
            GenerateDefaultValues = true;
        }

        /// <summary>Gets or sets the property nullability handling.</summary>
        public NullHandling NullHandling { get; set; } = NullHandling.JsonSchema;

        /// <summary>Gets or sets a value indicating whether to generate default values for properties (when JSON Schema default is set, default: true).</summary>
        public bool GenerateDefaultValues { get; set; }

        /// <summary>Gets or sets the property name generator.</summary>
        [JsonIgnore]
        public IPropertyNameGenerator PropertyNameGenerator { get; set; }

        /// <summary>Gets or sets the type name generator.</summary>
        [JsonIgnore]
        public ITypeNameGenerator TypeNameGenerator { get; set; }

        /// <summary>Gets or sets the template factory.</summary>
        [JsonIgnore]
        public ITemplateFactory TemplateFactory { get; set; } = new DefaultTemplateFactory();
    }
}