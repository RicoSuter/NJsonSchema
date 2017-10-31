//-----------------------------------------------------------------------
// <copyright file="CSharpClassGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using NJsonSchema.CodeGeneration.TypeScript.Models;
using System.Linq;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>The TypeScript interface and enum code generator. </summary>
    public class TypeScriptGenerator : GeneratorBase
    {
        private readonly TypeScriptTypeResolver _resolver;

        /// <summary>Initializes a new instance of the <see cref="TypeScriptGenerator"/> class.</summary>
        /// <param name="schema">The schema.</param>
        public TypeScriptGenerator(JsonSchema4 schema)
            : this(schema, new TypeScriptGeneratorSettings())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TypeScriptGenerator"/> class.</summary>
        /// <param name="rootObject">The root object to search for all JSON Schemas.</param>
        /// <param name="settings">The generator settings.</param>
        public TypeScriptGenerator(object rootObject, TypeScriptGeneratorSettings settings)
            : this(rootObject, settings, new TypeScriptTypeResolver(settings))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TypeScriptGenerator" /> class.</summary>
        /// <param name="rootObject">The root object to search for all JSON Schemas.</param>
        /// <param name="settings">The generator settings.</param>
        /// <param name="resolver">The resolver.</param>
        public TypeScriptGenerator(object rootObject, TypeScriptGeneratorSettings settings, TypeScriptTypeResolver resolver)
            : base(rootObject, resolver, settings)
        {
            _resolver = resolver;
            Settings = settings;
        }

        /// <summary>Gets the generator settings.</summary>
        public TypeScriptGeneratorSettings Settings { get; }

        /// <summary>Generates all types from the resolver with extension code from the settings.</summary>
        /// <returns>The code.</returns>
        public override CodeArtifactCollection GenerateTypes()
        {
            return GenerateTypes(new TypeScriptExtensionCode(Settings.ExtensionCode, Settings.ExtendedClasses));
        }

        /// <summary>Generates all types from the resolver with the given extension code.</summary>
        /// <returns>The code.</returns>
        public CodeArtifactCollection GenerateTypes(ExtensionCode extensionCode)
        {
            var collection = base.GenerateTypes();
            var artifacts = collection.Artifacts.ToList();

            foreach (var artifact in collection.Artifacts)
            {
                if (extensionCode?.ExtensionClasses.ContainsKey(artifact.TypeName) == true)
                {
                    var classCode = artifact.Code;

                    var index = classCode.IndexOf("constructor(", StringComparison.Ordinal);
                    if (index != -1)
                        artifact.Code = classCode.Insert(index, extensionCode.GetExtensionClassBody(artifact.TypeName).Trim() + "\n\n    ");
                    else
                    {
                        index = classCode.IndexOf("class", StringComparison.Ordinal);
                        index = classCode.IndexOf("{", index, StringComparison.Ordinal) + 1;

                        artifact.Code = classCode.Insert(index, "\n    " + extensionCode.GetExtensionClassBody(artifact.TypeName).Trim() + "\n");
                    }
                }
            }

            if (artifacts.Any(r => r.Code.Contains("formatDate(")))
            {
                var template = Settings.TemplateFactory.CreateTemplate("TypeScript", "File.FormatDate", null);
                artifacts.Add(new CodeArtifact("formatDate", CodeArtifactType.Function, CodeArtifactLanguage.CSharp, template));
            }

            if (Settings.HandleReferences)
            {
                var template = Settings.TemplateFactory.CreateTemplate("TypeScript", "File.ReferenceHandling", null);
                artifacts.Add(new CodeArtifact("jsonParse", CodeArtifactType.Function, CodeArtifactLanguage.CSharp, template));
            }

            return new CodeArtifactCollection(artifacts, extensionCode);
        }

        /// <summary>Generates the file.</summary>
        /// <returns>The file contents.</returns>
        protected override string GenerateFile(CodeArtifactCollection artifactCollection)
        {
            var model = new FileTemplateModel(Settings)
            {
                Types = artifactCollection.Concatenate(),
                ExtensionCode = (TypeScriptExtensionCode)artifactCollection.ExtensionCode
            };

            var template = Settings.TemplateFactory.CreateTemplate("TypeScript", "File", model);
            return ConversionUtilities.TrimWhiteSpaces(template.Render());
        }

        /// <summary>Generates the type.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The fallback type name.</param>
        /// <returns>The code.</returns>
        protected override CodeArtifact GenerateType(JsonSchema4 schema, string typeNameHint)
        {
            var typeName = _resolver.GetOrGenerateTypeName(schema, typeNameHint);

            if (schema.IsEnumeration)
            {
                var model = new EnumTemplateModel(typeName, schema, Settings);
                var template = Settings.TemplateFactory.CreateTemplate("TypeScript", "Enum", model);
                return new CodeArtifact(typeName, CodeArtifactType.Enum, CodeArtifactLanguage.TypeScript, template);
            }
            else
            {
                var model = new ClassTemplateModel(typeName, typeNameHint, Settings, _resolver, schema, RootObject);
                var template = Settings.CreateTemplate(typeName, model);

                var type = Settings.TypeStyle == TypeScriptTypeStyle.Interface
                    ? CodeArtifactType.Interface
                    : CodeArtifactType.Class;

                return new CodeArtifact(typeName, model.BaseClass, type, CodeArtifactLanguage.TypeScript, template);
            }
        }
    }
}
