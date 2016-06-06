//-----------------------------------------------------------------------
// <copyright file="CodeGeneratorSettingsBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The code generator settings base.</summary>
    public class CodeGeneratorSettingsBase
    {
        /// <summary>Gets or sets the property nullability handling.</summary>
        public PropertyNullHandling PropertyNullHandling { get; set; } = PropertyNullHandling.OneOf;

        /// <summary>Gets or sets the property name generator.</summary>
        public IPropertyNameGenerator PropertyNameGenerator { get; set; }

        /// <summary>Gets or sets the type name generator.</summary>
        public ITypeNameGenerator TypeNameGenerator { get; set; }
    }
}