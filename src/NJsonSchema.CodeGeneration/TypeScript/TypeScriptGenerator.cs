//-----------------------------------------------------------------------
// <copyright file="CSharpClassGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;
using NJsonSchema.CodeGeneration.TypeScript.Models;
using NJsonSchema.CodeGeneration.TypeScript.Templates;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>The TypeScript interface and enum code generator. </summary>
    public class TypeScriptGenerator : TypeGeneratorBase
    {
        private readonly JsonSchema4 _schema;
        private readonly TypeScriptTypeResolver _resolver;

        /// <summary>Initializes a new instance of the <see cref="TypeScriptGenerator"/> class.</summary>
        /// <param name="schema">The schema.</param>
        public TypeScriptGenerator(JsonSchema4 schema)
            : this(schema, new TypeScriptGeneratorSettings())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TypeScriptGenerator"/> class.</summary>
        /// <param name="settings">The generator settings.</param>
        /// <param name="schema">The schema.</param>
        public TypeScriptGenerator(JsonSchema4 schema, TypeScriptGeneratorSettings settings)
            : this(schema, settings, new TypeScriptTypeResolver(settings, schema), null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TypeScriptGenerator" /> class.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="settings">The generator settings.</param>
        /// <param name="resolver">The resolver.</param>
        /// <param name="rootObject">The root object to search for all JSON Schemas.</param>
        public TypeScriptGenerator(JsonSchema4 schema, TypeScriptGeneratorSettings settings, TypeScriptTypeResolver resolver, object rootObject)
        {
            _schema = schema;
            _resolver = resolver;
            RootObject = rootObject ?? schema;
            Settings = settings;
        }

        /// <summary>Gets or sets the root object.</summary>
        public object RootObject { get; set; }

        /// <summary>Gets the generator settings.</summary>
        public TypeScriptGeneratorSettings Settings { get; set; }

        /// <summary>Gets the language.</summary>
        protected override string Language => "TypeScript";

        /// <summary>Generates the file.</summary>
        /// <returns>The file contents.</returns>
        public override string GenerateFile()
        {
            _resolver.Resolve(_schema, false, string.Empty); // register root type

            var template = new FileTemplate() as ITemplate;
            template.Initialize(new FileTemplateModel
            {
                Types = ConversionUtilities.TrimWhiteSpaces(_resolver.GenerateTypes(Settings.ProcessedExtensionCode)),

                HasModuleName = !string.IsNullOrEmpty(Settings.ModuleName),
                ModuleName = Settings.ModuleName,

                ExtensionCodeBefore = Settings.ProcessedExtensionCode.CodeBefore,
                ExtensionCodeAfter = Settings.ProcessedExtensionCode.CodeAfter
            });
            return ConversionUtilities.TrimWhiteSpaces(template.Render());
        }

        /// <summary>Generates the type.</summary>
        /// <param name="fallbackTypeName">The fallback type name.</param>
        /// <returns>The code.</returns>
        public override TypeGeneratorResult GenerateType(string fallbackTypeName)
        {
            var typeName = _schema.GetTypeName(Settings.TypeNameGenerator);

            if (string.IsNullOrEmpty(typeName))
                typeName = fallbackTypeName;

            if (_schema.IsEnumeration)
            {
                if (_schema.Type == JsonObjectType.Integer)
                    typeName = typeName + "AsInteger";

                var template = new EnumTemplate() as ITemplate;
                template.Initialize(new EnumTemplateModel(typeName, _schema));
                return new TypeGeneratorResult
                {
                    TypeName = typeName,
                    Code = template.Render()
                };
            }
            else
            {
                var properties = _schema.ActualProperties.Values.Select(property => new PropertyModel(property, typeName, _resolver, Settings)).ToList();
                var hasInheritance = _schema.InheritedSchemas.Count >= 1;
                var baseClass = hasInheritance ? _resolver.Resolve(_schema.InheritedSchemas.First(), true, string.Empty) : null;

                var derivedClassNames = _schema.GetDerivedSchemas(RootObject, _resolver)
                    .Where(s => s.Value.Inherits(_schema))
                    .Select(s => s.Key)
                    .ToList();

                var template = Settings.CreateTemplate(typeName);
                template.Initialize(new // TODO: Create model class
                {
                    Class = Settings.ExtendedClasses?.Contains(typeName) == true ? typeName + "Base" : typeName,
                    RealClass = typeName,
                    DerivedClassNames = derivedClassNames,
                    DerivedClassNamesWithPropertyCheck = derivedClassNames.Where(n => properties.All(p => p.PropertyName != n)),

                    HasDiscriminator = !string.IsNullOrEmpty(_schema.BaseDiscriminator),
                    Discriminator = _schema.BaseDiscriminator,
                    DiscriminatorProperty = GetDiscriminatorProperty(_schema),

                    HasDescription = !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description),
                    Description = ConversionUtilities.RemoveLineBreaks(_schema.Description),

                    HasInheritance = hasInheritance,
                    Inheritance = hasInheritance ? " extends " + baseClass : string.Empty,
                    Properties = properties
                });

                return new TypeGeneratorResult
                {
                    TypeName = typeName,
                    BaseTypeName = baseClass,
                    Code = template.Render()
                };
            }
        }

        private PropertyModel GetDiscriminatorProperty(JsonSchema4 schema)
        {
            var property = schema.ActualSchema.ActualProperties.FirstOrDefault(p => p.Value.IsInheritanceDiscriminator);
            if (property.Value != null)
                return new PropertyModel(property.Value, string.Empty, _resolver, Settings);

            foreach (var baseSchema in schema.InheritedSchemas)
            {
                var propertyModel = GetDiscriminatorProperty(baseSchema);
                if (propertyModel != null)
                    return propertyModel;
            }

            return null;
        }
    }
}
