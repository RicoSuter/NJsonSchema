//-----------------------------------------------------------------------
// <copyright file="CSharpGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;
using NJsonSchema.CodeGeneration.TypeScript.Templates;

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
            AdditionalCode = string.Empty;
        }

        /// <summary>Gets or sets a value indicating whether to generate the readonly keywords (only available in TS 2.0+, default: true).</summary>
        public bool GenerateReadOnlyKeywords { get; set; }

        /// <summary>Gets or sets the type style (experimental, default: Interface).</summary>
        public TypeScriptTypeStyle TypeStyle { get; set; }

        /// <summary>Gets or sets the class mappings (the classes must be implemented in the <see cref="AdditionalCode"/>).</summary>
        public TypeScriptClassMapping[] ClassMappings { get; set; }

        /// <summary>Gets or sets the additional code to append to the generated code.</summary>
        public string AdditionalCode { get; set; }

        /// <summary>Gets the transformed additional code.</summary>
        public string TransformedAdditionalCode
        {
            get
            {
                var additionalCode = Regex.Replace(AdditionalCode ?? "", "import generated = (.*?)\n", "", RegexOptions.Multiline);
                return additionalCode.Replace("generated.", "");
            }
        }

        internal ITemplate CreateTemplate()
        {
            if (TypeStyle == TypeScriptTypeStyle.Interface)
                return new InterfaceTemplate();

            if (TypeStyle == TypeScriptTypeStyle.Class)
                return new ClassTemplate();

            if (TypeStyle == TypeScriptTypeStyle.KnockoutClass)
                return new KnockoutClassTemplate();

            throw new NotImplementedException();
        }
    }
}