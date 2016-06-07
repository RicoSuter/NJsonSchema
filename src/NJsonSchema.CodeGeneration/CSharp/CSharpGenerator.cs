//-----------------------------------------------------------------------
// <copyright file="CSharpGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;
using NJsonSchema.CodeGeneration.CSharp.Models;
using NJsonSchema.CodeGeneration.CSharp.Templates;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>The CSharp code generator. </summary>
    public class CSharpGenerator : TypeGeneratorBase
    {
        private readonly JsonSchema4 _schema;
        private readonly CSharpTypeResolver _resolver;

        /// <summary>Initializes a new instance of the <see cref="CSharpGenerator"/> class.</summary>
        /// <param name="schema">The schema.</param>
        public CSharpGenerator(JsonSchema4 schema)
            : this(schema, new CSharpGeneratorSettings())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CSharpGenerator"/> class.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="settings">The generator settings.</param>
        public CSharpGenerator(JsonSchema4 schema, CSharpGeneratorSettings settings)
            : this(schema, settings, new CSharpTypeResolver(settings))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CSharpGenerator"/> class.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="settings">The generator settings.</param>
        /// <param name="resolver">The resolver.</param>
        public CSharpGenerator(JsonSchema4 schema, CSharpGeneratorSettings settings, CSharpTypeResolver resolver)
        {
            _schema = schema;
            _resolver = resolver;
            Settings = settings;
        }

        /// <summary>Gets the generator settings.</summary>
        public CSharpGeneratorSettings Settings { get; private set; }

        /// <summary>Gets the language.</summary>
        protected override string Language => "CSharp";

        /// <summary>Generates the file.</summary>
        /// <returns>The file contents.</returns>
        public override string GenerateFile()
        {
            _resolver.Resolve(_schema, false, string.Empty); // register root type

            var template = new FileTemplate() as ITemplate;
            template.Initialize(new FileTemplateModel
            {
                Toolchain = JsonSchema4.ToolchainVersion, 
                Namespace = Settings.Namespace ?? string.Empty,
                Classes = ConversionUtilities.TrimWhiteSpaces(_resolver.GenerateTypes(null))
            });
            return ConversionUtilities.TrimWhiteSpaces(template.Render());
        }

        /// <summary>Generates the type.</summary>
        /// <param name="fallbackTypeName">The fallback type name when TypeName is not available on schema.</param>
        /// <returns>The code.</returns>
        public override TypeGeneratorResult GenerateType(string fallbackTypeName)
        {
            var typeName = _schema.GetTypeName(Settings.TypeNameGenerator);
            if (string.IsNullOrEmpty(typeName))
                typeName = fallbackTypeName;
            if (_schema.IsEnumeration)
                return GenerateEnum(typeName);
            else
                return GenerateClass(typeName);
        }

        private TypeGeneratorResult GenerateClass(string typeName)
        {

            var properties = _schema.Properties.Values
                .Select(property => new PropertyModel(property, _resolver, Settings))
                .ToList();

            if (_resolver.Settings.IsFlattenAllOf && _schema.AllOf.Count > 1)
            {
                var allOfProperties = _schema.AllOf
                    .SelectMany(s => s.ActualSchema.Properties.Values.Select(property => new PropertyModel(property, _resolver, Settings)))
                   .ToList();
                properties.AddRange(allOfProperties);
            }

            var model = new ClassTemplateModel(typeName, Settings, _resolver, _schema, properties); 

            var template = new ClassTemplate() as ITemplate;
            template.Initialize(model);
            return new TypeGeneratorResult
            {
                TypeName = typeName,
                BaseTypeName = model.BaseClass, 
                Code = template.Render()
            };
        }

        private TypeGeneratorResult GenerateEnum(string typeName)
        {
            var template = new EnumTemplate() as ITemplate;
            template.Initialize(new EnumTemplateModel(typeName, _schema));
            return new TypeGeneratorResult
            {
                TypeName = typeName,
                Code = template.Render()
            };
        }
    }
}
