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
    public class CSharpGeneratorSettings : CodeGeneratorSettingsBase
    {
        /// <summary>Initializes a new instance of the <see cref="CSharpGeneratorSettings"/> class.</summary>
        public CSharpGeneratorSettings()
        {
            DateTimeType = "DateTime";
            ArrayType = "ObservableCollection";
            DictionaryType = "Dictionary"; 
            RequiredPropertiesMustBeDefined = true; 
            ClassStyle = CSharpClassStyle.Inpc;
        }

        /// <summary>Gets or sets the .NET namespace of the generated types.</summary>
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

        /// <summary>Gets or sets the CSharp class style (default: 'Poco').</summary>
        public CSharpClassStyle ClassStyle { get; set; }

        /// <summary>Gets or sets the custom Json.NET converters (class names) which are registered for serialization and deserialization.</summary>
        public string[] JsonConverters { get; set; }
    }
}
