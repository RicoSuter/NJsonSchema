//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Reflection;
using NJsonSchema.Annotations;
using NJsonSchema.Generation.TypeMappers;
using Namotion.Reflection;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace NJsonSchema.Generation
{
    /// <summary>The JSON Schema generator settings.</summary>
    public abstract class JsonSchemaGeneratorSettings : IXmlDocsSettings
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchemaGeneratorSettings"/> class.</summary>
        public JsonSchemaGeneratorSettings(IReflectionService reflectionService)
        {
            ReflectionService = reflectionService;

            DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.Null;
            DefaultDictionaryValueReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;

            SchemaType = SchemaType.JsonSchema;
            GenerateAbstractSchemas = true;
            GenerateExamples = true;

            TypeNameGenerator = new DefaultTypeNameGenerator();
            SchemaNameGenerator = new DefaultSchemaNameGenerator();

            ExcludedTypeNames = [];

            UseXmlDocumentation = true;
            ResolveExternalXmlDocumentation = true;
            XmlDocumentationFormatting = XmlDocsFormattingMode.None;
        }

        /// <summary>Gets or sets the default reference type null handling when no nullability information is available (default: Null).</summary>
        public ReferenceTypeNullHandling DefaultReferenceTypeNullHandling { get; set; }

        /// <summary>Gets or sets the default reference type null handling of dictionary value types when no nullability information is available (default: NotNull).</summary>
        public ReferenceTypeNullHandling DefaultDictionaryValueReferenceTypeNullHandling { get; set; }

        /// <summary>Gets or sets a value indicating whether to generate abstract properties (i.e. interface and abstract properties. Properties may defined multiple times in a inheritance hierarchy, default: false).</summary>
        public bool GenerateAbstractProperties { get; set; }

        /// <summary>Gets or sets a value indicating whether to flatten the inheritance hierarchy instead of using allOf to describe inheritance (default: false).</summary>
        public bool FlattenInheritanceHierarchy { get; set; }

        /// <summary>Gets or sets a value indicating whether to generate the x-abstract flag on schemas (default: true).</summary>
        public bool GenerateAbstractSchemas { get; set; }

        /// <summary>Gets or sets a value indicating whether to generate schemas for types in <see cref="KnownTypeAttribute"/> attributes (default: true).</summary>
        public bool GenerateKnownTypes { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether to generate xmlObject representation for definitions (default: false).</summary>
        public bool GenerateXmlObjects { get; set; }

        /// <summary>Gets or sets a value indicating whether to ignore properties with the <see cref="T:ObsoleteAttribute"/>.</summary>
        public bool IgnoreObsoleteProperties { get; set; }

        /// <summary>Gets or sets a value indicating whether to use $ref references even if additional properties are
        /// defined on the object (otherwise allOf/oneOf with $ref is used, default: false).</summary>
        public bool AllowReferencesWithProperties { get; set; }

        /// <summary>Gets or sets a value indicating whether to generate a description with number to enum name mappings (for integer enums only, default: false).</summary>
        public bool GenerateEnumMappingDescription { get; set; }

        /// <summary>Will set `additionalProperties` on all added <see cref="JsonSchema">schema definitions and references</see>(default: false).</summary>
        public bool AlwaysAllowAdditionalObjectProperties { get; set; }

        /// <summary>Gets or sets a value indicating whether to generate the example property of the schemas based on the &lt;example&gt; xml docs entry as JSON (requires <see cref="UseXmlDocumentation"/> to be true, default: true).</summary>
        public bool GenerateExamples { get; set; }

        /// <summary>Gets or sets the schema type to generate (default: JsonSchema).</summary>
        public SchemaType SchemaType { get; set; }

        /// <summary>Gets or sets the excluded type names (same as <see cref="JsonSchemaIgnoreAttribute"/>).</summary>
        public string[] ExcludedTypeNames { get; set; }

        /// <summary>Gets or sets a value indicating whether to read XML Docs (default: true).</summary>
        public bool UseXmlDocumentation { get; set; }

        /// <summary>Gets or sets a value indicating whether tho resolve the XML Docs from the NuGet cache or .NET SDK directory (default: true).</summary>
        public bool ResolveExternalXmlDocumentation { get; set; }

        /// <summary>Gets or sets the XML Docs formatting (default: None).</summary>
        public XmlDocsFormattingMode XmlDocumentationFormatting { get; set; }

        /// <summary>Gets or sets the type name generator.</summary>
        [JsonIgnore]
        public ITypeNameGenerator TypeNameGenerator { get; set; }

        /// <summary>Gets or sets the schema name generator.</summary>
        [JsonIgnore]
        public ISchemaNameGenerator SchemaNameGenerator { get; set; }

        /// <summary>Gets or sets the reflection service.</summary>
        [JsonIgnore]
        public IReflectionService ReflectionService { get; set; }

        /// <summary>Gets or sets the type mappings.</summary>
        [JsonIgnore]
        public ICollection<ITypeMapper> TypeMappers { get; set; } = [];

        /// <summary>Gets or sets the schema processors.</summary>
        [JsonIgnore]
        public ICollection<ISchemaProcessor> SchemaProcessors { get; } = [];

        /// <summary>Gets or sets a value indicating whether to generate x-nullable properties (Swagger 2 only).</summary>
        public bool GenerateCustomNullableProperties { get; set; }

        /// <summary>Gets the actual computed <see cref="GenerateAbstractSchemas"/> setting based on the global setting and the JsonSchemaAbstractAttribute attribute.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The result.</returns>
        public bool GetActualGenerateAbstractSchema(Type type)
        {
            var attribute = type.GetCustomAttributes(false)
                .FirstAssignableToTypeNameOrDefault("JsonSchemaAbstractAttribute", TypeNameStyle.Name);

            return (GenerateAbstractSchemas && attribute == null) || attribute?.TryGetPropertyValue("IsAbstract", true) == true;
        }

        /// <summary>Gets the actual computed <see cref="FlattenInheritanceHierarchy"/> setting based on the global setting and the JsonSchemaFlattenAttribute attribute.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The result.</returns>
        public bool GetActualFlattenInheritanceHierarchy(Type type)
        {
            var attribute = type.GetCustomAttributes(false)
                .FirstAssignableToTypeNameOrDefault("JsonSchemaFlattenAttribute", TypeNameStyle.Name);

            return (FlattenInheritanceHierarchy && attribute == null) || attribute?.TryGetPropertyValue("Flatten", true) == true;
        }
    }
}