//-----------------------------------------------------------------------
// <copyright file="CSharpClassGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using NJsonSchema.CodeGeneration.Models;
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
            : this(schema, settings, new TypeScriptTypeResolver(settings))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TypeScriptGenerator"/> class.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="settings">The generator settings.</param>
        /// <param name="resolver">The resolver.</param>
        public TypeScriptGenerator(JsonSchema4 schema, TypeScriptGeneratorSettings settings, TypeScriptTypeResolver resolver)
        {
            _schema = schema;
            _resolver = resolver;
            Settings = settings;
        }

        /// <summary>Gets the generator settings.</summary>
        public TypeScriptGeneratorSettings Settings { get; set; }

        /// <summary>Gets the language.</summary>
        protected override string Language => "TypeScript";

        /// <summary>Generates the file.</summary>
        /// <returns>The file contents.</returns>
        public override string GenerateFile()
        {
            var output =
                GenerateType(_resolver.GenerateTypeName()).Code + "\n\n" +
                _resolver.GenerateTypes() + "\n\n" +
                Settings.TransformedExtensionCode + "\n\n";

            return ConversionUtilities.TrimWhiteSpaces(output);
        }

        /// <summary>Generates the type.</summary>
        /// <param name="fallbackTypeName">The fallback type name.</param>
        /// <returns>The code.</returns>
        public override TypeGeneratorResult GenerateType(string fallbackTypeName)
        {
            var typeName = !string.IsNullOrEmpty(_schema.TypeName) ? _schema.TypeName : fallbackTypeName;

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
                var properties = _schema.Properties.Values.Select(property => new PropertyModel(property, typeName, _resolver, Settings)).ToList();
                var hasInheritance = _schema.AllOf.Count == 1;

                var template = Settings.CreateTemplate(typeName);
                template.Initialize(new // TODO: Create model class
                {
                    Class = typeName,

                    HasDescription = !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description),
                    Description = ConversionUtilities.RemoveLineBreaks(_schema.Description),

                    HasInheritance = hasInheritance,
                    Inheritance = hasInheritance ? " extends " + _resolver.Resolve(_schema.AllOf.First(), true, string.Empty) : string.Empty,
                    Properties = properties
                });

                return new TypeGeneratorResult
                {
                    TypeName = typeName,
                    Code = template.Render()
                };
            }
        }
    }
}
