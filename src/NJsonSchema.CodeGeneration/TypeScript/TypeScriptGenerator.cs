//-----------------------------------------------------------------------
// <copyright file="CSharpClassGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using NJsonSchema.CodeGeneration.TypeScript.Models;

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
        public TypeScriptGeneratorSettings Settings { get; }

        /// <summary>Generates the file.</summary>
        /// <param name="rootTypeNameHint">The root type name hint.</param>
        /// <returns>The file contents.</returns>
        public override string GenerateFile(string rootTypeNameHint)
        {
            _resolver.Resolve(_schema, false, rootTypeNameHint); // register root type

            var model = new FileTemplateModel(Settings)
            {
                Types = ConversionUtilities.TrimWhiteSpaces(_resolver.GenerateTypes(Settings.ProcessedExtensionCode)),

                ExtensionCodeBefore = Settings.ProcessedExtensionCode.CodeBefore,
                ExtensionCodeAfter = Settings.ProcessedExtensionCode.CodeAfter
            };

            var template = Settings.TemplateFactory.CreateTemplate("TypeScript", "File", model);
            return ConversionUtilities.TrimWhiteSpaces(template.Render());
        }

        /// <summary>Generates the type.</summary>
        /// <param name="typeNameHint">The fallback type name.</param>
        /// <returns>The code.</returns>
        public override TypeGeneratorResult GenerateType(string typeNameHint)
        {
            var typeName = _resolver.GetOrGenerateTypeName(_schema, typeNameHint);

            if (_schema.IsEnumeration)
            {
                var model = new EnumTemplateModel(typeName, _schema, Settings);
                var template = Settings.TemplateFactory.CreateTemplate("TypeScript", "Enum", model);
                return new TypeGeneratorResult
                {
                    TypeName = typeName,
                    Code = template.Render()
                };
            }
            else
            {
                var model = new ClassTemplateModel(typeName, typeNameHint, Settings, _resolver, _schema, RootObject);
                var template = Settings.CreateTemplate(typeName, model);
                return new TypeGeneratorResult
                {
                    TypeName = typeName,
                    BaseTypeName = model.BaseClass,
                    Code = template.Render()
                };
            }
        }
    }
}
