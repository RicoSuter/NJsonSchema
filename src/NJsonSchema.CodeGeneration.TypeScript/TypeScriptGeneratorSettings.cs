//-----------------------------------------------------------------------
// <copyright file="CSharpGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>The generator settings.</summary>
    public class TypeScriptGeneratorSettings : CodeGeneratorSettingsBase
    {
        /// <summary>Initializes a new instance of the <see cref="TypeScriptGeneratorSettings"/> class.</summary>
        public TypeScriptGeneratorSettings()
        {
            ModuleName = "";
            Namespace = "";

            NullValue = TypeScriptNullValue.Undefined;
            TypeStyle = TypeScriptTypeStyle.Class;
            DateTimeType = TypeScriptDateTimeType.Date;
            EnumStyle = TypeScriptEnumStyle.Enum;
            UseLeafType = false;
            ExtensionCode = string.Empty;
            TypeScriptVersion = 4.3m;
            GenerateConstructorInterface = true;
            ConvertConstructorInterfaceData = false;
            ExportTypes = true;

            ValueGenerator = new TypeScriptValueGenerator(this);
            PropertyNameGenerator = new TypeScriptPropertyNameGenerator();
            TemplateFactory = new DefaultTemplateFactory(this, [
                typeof(TypeScriptGeneratorSettings).Assembly
            ]);

            ClassTypes = [];
            ExtendedClasses = [];

            InlineNamedDictionaries = false;
            GenerateTypeCheckFunctions = false;
        }

        /// <summary>Gets or sets the target TypeScript version (minimum, default: 4.3).</summary>
        public decimal TypeScriptVersion { get; set; }

        /// <summary>Gets or sets a value indicating whether to mark optional properties with ? (default: false).</summary>
        public bool MarkOptionalProperties { get; set; }

        /// <summary>Gets or sets the type style (default: Class).</summary>
        public TypeScriptTypeStyle TypeStyle { get; set; }

        /// <summary>Gets or sets the date time type (default: 'Date').</summary>
        public TypeScriptDateTimeType DateTimeType { get; set; }

        /// <summary>
        /// Whether to use UTC (default) or local time zone when deserializing dates 'yyyy-MM-dd' (default: 'false').
        /// Only applicable if <see cref="DateTimeType"/> is <see cref="TypeScriptDateTimeType.Date"/>.
        /// Other DateTimeTypes use local timezone by default.
        /// </summary>
        public bool ConvertDateToLocalTimezone { get; set; }

        /// <summary>Gets or sets the enum style (default: Enum).</summary>
        public TypeScriptEnumStyle EnumStyle { get; set; }

        /// <summary>Generate leaf types for an object with discriminator (default: false).</summary>
        public bool UseLeafType { get; set; }

        /// <summary>Gets or sets the TypeScript module name (default: '', no module).</summary>
        public string ModuleName { get; set; }

        /// <summary>Gets or sets the TypeScript namespace (default: '', no namespace).</summary>
        public string Namespace { get; set; }

        /// <summary>Gets or sets the list of extended classes (the classes must be implemented in the <see cref="ExtensionCode"/>).</summary>
        public string[] ExtendedClasses { get; set; }

        /// <summary>Gets or sets the extension code to append to the generated code.</summary>
        public string ExtensionCode { get; set; }

        /// <summary>Gets or sets the type names which always generate plain TypeScript classes.</summary>
        public string[] ClassTypes { get; set; }

        /// <summary>Gets or sets the TypeScript null value.</summary>
        public TypeScriptNullValue NullValue { get; set; }

        /// <summary>Gets or sets a value indicating whether to handle JSON references (supports $ref, $id, $values, default: false).</summary>
        public bool HandleReferences { get; set; }

        /// <summary>Gets or sets a value indicating whether a clone() method should be generated in the DTO classes.</summary>
        public bool GenerateCloneMethod { get; set; }

        /// <summary>Gets or sets a value indicating whether to generate an class interface which is used in the constructor to initialize the class (default: true).</summary>
        public bool GenerateConstructorInterface { get; set; }

        /// <summary>Gets or sets a value indicating whether POJO objects in the constructor data are converted to DTO instances (GenerateConstructorInterface must be enabled, default: false).</summary>
        public bool ConvertConstructorInterfaceData { get; set; }

        /// <summary>Gets or sets a value indicating whether the export keyword should be added to all classes and enums (default: true).</summary>
        public bool ExportTypes { get; set; }

        /// <summary>Gets or sets a value indicating whether named/referenced dictionaries should be inlined or generated as class with an indexer.</summary>
        public bool InlineNamedDictionaries { get; set; }

        /// <summary>Gets a value indicating whether to generate type check functions (for type style interface only, default: false).</summary>
        public bool GenerateTypeCheckFunctions { get; set; }

        internal ITemplate CreateTemplate(string typeName, object model)
        {
            if (ClassTypes != null && ClassTypes.Contains(typeName))
            {
                return TemplateFactory.CreateTemplate("TypeScript", "Class", model);
            }

            return TemplateFactory.CreateTemplate("TypeScript", TypeStyle.ToString(), model);
        }

        /// <summary>Gets the type style of the given type name.</summary>
        /// <param name="typeName">The type name.</param>
        /// <returns>The type style.</returns>
        public TypeScriptTypeStyle GetTypeStyle(string typeName)
        {
            if (ClassTypes != null && ClassTypes.Contains(typeName))
            {
                return TypeScriptTypeStyle.Class;
            }

            return TypeStyle;
        }
    }
}