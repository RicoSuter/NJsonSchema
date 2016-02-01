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
            DateTimeType = "DateTime";
            ArrayType = "ObservableCollection";
            DictionaryType = "Dictionary"; 
            RequiredPropertiesMustBeDefined = true; 
            Style = CSharpStyle.Inpc;
        }

        /// <summary>Gets or sets the namespace.</summary>
        public string Namespace { get; set; }

        /// <summary>Gets or sets a value indicating whether a required property must be defined in JSON 
        /// (sets Required.Always when the property is required) (default: true).</summary>
        public bool RequiredPropertiesMustBeDefined { get; set; }

        /// <summary>Gets or sets the date time .NET type (default: 'DateTime').</summary>
        public string DateTimeType { get; set; }

        /// <summary>Gets or sets the generic array .NET type (default: 'ObservableCollection').</summary>
        public string ArrayType { get; set; }

        /// <summary>Gets or sets the generic dictionary .NET type (default: 'Dictionary').</summary>
        public string DictionaryType { get; set; }

        /// <summary>Gets or sets the CSharp style.</summary>
        public CSharpStyle Style { get; set; }
    }
}