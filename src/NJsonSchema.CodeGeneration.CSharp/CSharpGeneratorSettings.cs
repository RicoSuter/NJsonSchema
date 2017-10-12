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
            DateType = "System.DateTime";
            DateTimeType = "System.DateTime";
            TimeType = "System.TimeSpan";
            TimeSpanType = "System.TimeSpan";

            ArrayType = "System.Collections.ObjectModel.ObservableCollection";
            DictionaryType = "System.Collections.Generic.Dictionary";

            RequiredPropertiesMustBeDefined = true;
            GenerateDataAnnotations = true;
            ClassStyle = CSharpClassStyle.Inpc;
            TypeAccessModifier = "public";

            PropertyNameGenerator = new CSharpPropertyNameGenerator();
            TemplateFactory = new DefaultTemplateFactory();
        }

        /// <summary>Gets or sets the .NET namespace of the generated types.</summary>
        public string Namespace { get; set; }

        /// <summary>Gets or sets a value indicating whether a required property must be defined in JSON 
        /// (sets Required.Always when the property is required) (default: true).</summary>
        public bool RequiredPropertiesMustBeDefined { get; set; }

        /// <summary>Gets or sets a value indicating whether to generated data annotation attributes (default: true).</summary>
        public bool GenerateDataAnnotations { get; set; }

        /// <summary>Gets or sets the date .NET type (default: 'DateTime').</summary>
        public string DateType { get; set; }

        /// <summary>Gets or sets the date time .NET type (default: 'DateTime').</summary>
        public string DateTimeType { get; set; }

        /// <summary>Gets or sets the time .NET type (default: 'TimeSpan').</summary>
        public string TimeType { get; set; }

        /// <summary>Gets or sets the time span .NET type (default: 'TimeSpan').</summary>
        public string TimeSpanType { get; set; }

        /// <summary>Gets or sets the generic array .NET type (default: 'ObservableCollection').</summary>
        public string ArrayType { get; set; }

        /// <summary>Gets or sets the generic dictionary .NET type (default: 'Dictionary').</summary>
        public string DictionaryType { get; set; }

        /// <summary>Gets or sets the CSharp class style (default: 'Poco').</summary>
        public CSharpClassStyle ClassStyle { get; set; }

        /// <summary>Gets or sets the access modifier of generated classes and interfaces (default: 'public').</summary>
        public string TypeAccessModifier { get; set; }

        /// <summary>Gets or sets the custom Json.NET converters (class names) which are registered for serialization and deserialization.</summary>
        public string[] JsonConverters { get; set; }

        /// <summary>Gets or sets a value indicating whether to remove the setter for non-nullable array properties (default: false).</summary>
        public bool GenerateImmutableArrayProperties { get; set; }

        /// <summary>Gets or sets a value indicating whether to remove the setter for non-nullable dictionary properties (default: false).</summary>
        public bool GenerateImmutableDictionaryProperties { get; set; }

        /// <summary>Gets or sets a value indicating whether to use preserve references handling (All) in the JSON serializer (default: false).</summary>
        public bool HandleReferences { get; set; }

        /// <summary>Gets or sets the name of a static method which is called to transform the JsonSerializerSettings used in the generated ToJson()/FromJson() methods (default: null).</summary>
        public string JsonSerializerSettingsTransformationMethod { get; set; }
    }
}
