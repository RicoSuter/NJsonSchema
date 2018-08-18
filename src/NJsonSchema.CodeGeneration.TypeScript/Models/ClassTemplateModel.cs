//-----------------------------------------------------------------------
// <copyright file="ClassTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NJsonSchema.CodeGeneration.Models;

namespace NJsonSchema.CodeGeneration.TypeScript.Models
{
    /// <summary>The TypeScript class template model.</summary>
    public class ClassTemplateModel : ClassTemplateModelBase
    {
        private readonly TypeScriptGeneratorSettings _settings;
        private readonly JsonSchema4 _schema;
        private readonly TypeScriptTypeResolver _resolver;

        /// <summary>Initializes a new instance of the <see cref="ClassTemplateModel" /> class.</summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="discriminatorName">The name to compare the discriminator against.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="resolver">The resolver.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        public ClassTemplateModel(string typeName, string discriminatorName,
            TypeScriptGeneratorSettings settings, TypeScriptTypeResolver resolver,
            JsonSchema4 schema, object rootObject)
            : base(resolver, schema, rootObject)
        {
            _settings = settings;
            _schema = schema;
            _resolver = resolver;

            ClassName = typeName;
            DiscriminatorName = discriminatorName;
        }

        /// <summary>Gets the class name.</summary>
        public override string ClassName { get; }

        /// <summary>Gets the name for the discriminator check.</summary>
        public string DiscriminatorName { get; }

        /// <summary>Gets a value indicating whether the class has a discriminator property.</summary>
        public bool HasDiscriminator => !string.IsNullOrEmpty(_schema.Discriminator);

        /// <summary>Gets a value indicating whether the class or an inherited class has a discriminator property.</summary>
        public bool HasBaseDiscriminator => _schema.BaseDiscriminator != null;

        /// <summary>Gets the class discriminator property name (may be defined in a inherited class).</summary>
        public string BaseDiscriminator => _schema.BaseDiscriminator?.PropertyName;

        /// <summary>Gets a value indicating whether the class has description.</summary>
        public bool HasDescription => !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description);

        /// <summary>Gets the description.</summary>
        public string Description => ConversionUtilities.RemoveLineBreaks(_schema.Description);

        /// <summary>Gets a value indicating whether this class has a parent class.</summary>
        public bool HasInheritance => InheritedSchema != null && !InheritedSchema.IsDictionary;

        /// <summary>Gets the inheritance code.</summary>
        public string Inheritance
        {
            get
            {
                if (HasInheritance)
                {
                    return " extends " + BaseClass + (GenerateConstructorInterface && _settings.TypeStyle == TypeScriptTypeStyle.Class ?
                        " implements I" + ClassName : string.Empty);
                }

                return GenerateConstructorInterface && _settings.TypeStyle == TypeScriptTypeStyle.Class ?
                    " implements I" + ClassName : string.Empty;
            }
        }

        /// <summary>Gets the constructor interface inheritance code.</summary>
        public string InterfaceInheritance => HasInheritance ? " extends I" + BaseClass : string.Empty;

        /// <summary>Gets the base class name.</summary>
        public string BaseClass => HasInheritance ? _resolver.Resolve(InheritedSchema, true, string.Empty) : null;

        /// <summary>Gets a value indicating whether the class inherits from dictionary.</summary>
        public bool HasIndexerProperty => _schema.InheritedSchema?.IsDictionary == true;

        /// <summary>Gets or sets a value indicating whether a clone() method should be generated in the DTO classes.</summary>
        public bool GenerateCloneMethod => _settings.GenerateCloneMethod;

        /// <summary>Gets or sets a value indicating whether to generate an class interface which is used in the constructor to initialize the class (default: true).</summary>
        public bool GenerateConstructorInterface => _settings.GenerateConstructorInterface;

        /// <summary>Gets or sets a value indicating whether POJO objects in the constructor data are converted to DTO instances (default: true).</summary>
        public bool ConvertConstructorInterfaceData => _settings.ConvertConstructorInterfaceData;

        /// <summary>Gets the null value.</summary>
        public string NullValue => _settings.NullValue.ToString().ToLowerInvariant();

        /// <summary>Gets the type of the indexer property value.</summary>
        public string IndexerPropertyValueType
        {
            get
            {
                var valueType = InheritedSchema?.AdditionalPropertiesSchema != null
                    ? _resolver.Resolve(InheritedSchema.AdditionalPropertiesSchema, true, string.Empty)
                    : "any";

                // TODO: Find solution to avoid using union with any
                return valueType != "any" ? valueType + " | any" : valueType;
            }
        }

        /// <summary>Gets a value indicating whether to handle JSON references.</summary>
        public bool HandleReferences => _settings.HandleReferences;

        /// <summary>Gets the property models.</summary>
        public List<PropertyModel> Properties => _schema.ActualProperties.Values
            .Where(v => v.IsInheritanceDiscriminator == false)
            .Select(property => new PropertyModel(this, property, ClassName, _resolver, _settings)).ToList();

        /// <summary>Gets a value indicating whether any property has a default value.</summary>
        public bool HasDefaultValues => Properties.Any(p => p.HasDefaultValue);

        /// <summary>Gets a value indicating whether </summary>
        public bool RequiresStrictPropertyInitialization => _settings.TypeScriptVersion >= 2.7m;

        /// <summary>Gets a value indicating whether the export keyword should be added to all classes.</summary>
        public bool ExportTypes => _settings.ExportTypes;

        /// <summary>Gets the inherited schema.</summary>
        private JsonSchema4 InheritedSchema => _schema.InheritedSchema?.ActualSchema;
    }
}