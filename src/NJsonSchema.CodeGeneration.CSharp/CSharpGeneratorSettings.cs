//-----------------------------------------------------------------------
// <copyright file="CSharpGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Reflection;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>The generator settings.</summary>
    public class CSharpGeneratorSettings : CodeGeneratorSettingsBase
    {
        /// <summary>Initializes a new instance of the <see cref="CSharpGeneratorSettings"/> class.</summary>
        public CSharpGeneratorSettings()
        {
            Namespace = "MyNamespace";

            DateType = "System.DateTime";
            DateTimeType = "System.DateTime";
            TimeType = "System.TimeSpan";
            TimeSpanType = "System.TimeSpan";

            ArrayType = "System.Collections.ObjectModel.ObservableCollection";
            DictionaryType = "System.Collections.Generic.Dictionary";

            ArrayBaseType = "System.Collections.ObjectModel.ObservableCollection";
            DictionaryBaseType = "System.Collections.Generic.Dictionary";

            RequiredPropertiesMustBeDefined = true;
            GenerateDataAnnotations = true;
            ClassStyle = CSharpClassStyle.Inpc;
            TypeAccessModifier = "public";
            PropertySetterAccessModifier = string.Empty;
            GenerateJsonMethods = true;
            EnforceFlagEnums = false;

            ValueGenerator = new CSharpValueGenerator(this);
            PropertyNameGenerator = new CSharpPropertyNameGenerator();
            TemplateFactory = new DefaultTemplateFactory(this, new Assembly[]
            {
                typeof(CSharpGeneratorSettings).GetTypeInfo().Assembly
            });
        }

        /// <summary>Gets or sets the .NET namespace of the generated types (default: MyNamespace).</summary>
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

        /// <summary>Gets or sets the generic array .NET type which is used for ArrayType instances (default: empty = use ArrayType).</summary>
        public string ArrayInstanceType { get; set; }

        /// <summary>Gets or sets the generic dictionary .NET type which is used for DictionaryType instances (default: empty = use DictionaryType).</summary>
        public string DictionaryInstanceType { get; set; }

        /// <summary>Gets or sets the generic array .NET type which is used as base class (default: 'ObservableCollection').</summary>
        public string ArrayBaseType { get; set; }

        /// <summary>Gets or sets the generic dictionary .NET type which is used as base class (default: 'Dictionary').</summary>
        public string DictionaryBaseType { get; set; }

        /// <summary>Gets or sets the CSharp class style (default: 'Poco').</summary>
        public CSharpClassStyle ClassStyle { get; set; }

        /// <summary>Gets or sets the access modifier of generated classes and interfaces (default: 'public').</summary>
        public string TypeAccessModifier { get; set; }

        /// <summary>Gets the access modifier of property setters (default: '').</summary>
        public string PropertySetterAccessModifier { get; set; }

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

        /// <summary>Gets or sets a value indicating whether to render ToJson() and FromJson() methods (default: true).</summary>
        public bool GenerateJsonMethods { get; set; }

        /// <summary>Gets or sets a value indicating whether enums should be always generated as bit flags (default: false).</summary>
        public bool EnforceFlagEnums { get; set; }
    }
}
