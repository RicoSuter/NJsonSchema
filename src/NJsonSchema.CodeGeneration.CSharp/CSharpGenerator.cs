//-----------------------------------------------------------------------
// <copyright file="CSharpGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NJsonSchema.CodeGeneration.CSharp.Models;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>The CSharp code generator.</summary>
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
            : this(schema, settings, new CSharpTypeResolver(settings, schema), null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CSharpGenerator"/> class.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="settings">The generator settings.</param>
        /// <param name="resolver">The resolver.</param>
        /// <param name="rootObject">The root object to search for all JSON Schemas.</param>
        public CSharpGenerator(JsonSchema4 schema, CSharpGeneratorSettings settings, CSharpTypeResolver resolver, object rootObject) 
            : base(schema, rootObject)
        {
            _schema = schema;
            _resolver = resolver;
            Settings = settings;
        }

        /// <summary>Gets the generator settings.</summary>
        public CSharpGeneratorSettings Settings { get; }

        /// <summary>Generates the file.</summary>
        /// <param name="rootTypeNameHint">The root type name hint.</param>
        /// <returns>The file contents.</returns>
        public override string GenerateFile(string rootTypeNameHint)
        {
            _resolver.Resolve(_schema, false, rootTypeNameHint); // register root type

            var model = new FileTemplateModel
            {
                Namespace = Settings.Namespace ?? string.Empty,
                Classes = ConversionUtilities.TrimWhiteSpaces(_resolver.GenerateTypes().Concatenate())
            };

            var template = Settings.TemplateFactory.CreateTemplate("CSharp", "File", model);
            return ConversionUtilities.TrimWhiteSpaces(template.Render());
        }

        /// <summary>Generates the type.</summary>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The code.</returns>
        public override CodeArtifact GenerateType(string typeNameHint)
        {
            var typeName = _resolver.GetOrGenerateTypeName(_schema, typeNameHint);

            if (_schema.IsEnumeration)
                return GenerateEnum(typeName);
            else
                return GenerateClass(typeName);
        }

        private CodeArtifact GenerateClass(string typeName)
        {
            var model = new ClassTemplateModel(typeName, Settings, _resolver, _schema, RootObject);

            RenamePropertyWithSameNameAsClass(typeName, model.Properties);

            var template = Settings.TemplateFactory.CreateTemplate("CSharp", "Class", model);
            return new CodeArtifact
            {
                Type = CodeArtifactType.Class,
                Language = CodeArtifactLanguage.CSharp,

                TypeName = typeName,
                BaseTypeName = model.BaseClassName,

                Code = template.Render()
            };
        }

        private void RenamePropertyWithSameNameAsClass(string typeName, IEnumerable<PropertyModel> properties)
        {
            var propertyWithSameNameAsClass = properties.SingleOrDefault(p => p.PropertyName == typeName);
            if (propertyWithSameNameAsClass != null)
            {
                var number = 1;
                while (properties.Any(p => p.PropertyName == typeName + number))
                    number++;

                propertyWithSameNameAsClass.PropertyName = propertyWithSameNameAsClass.PropertyName + number;
            }
        }

        private CodeArtifact GenerateEnum(string typeName)
        {
            var model = new EnumTemplateModel(typeName, _schema, Settings);
            var template = Settings.TemplateFactory.CreateTemplate("CSharp", "Enum", model);
            return new CodeArtifact
            {
                Type = CodeArtifactType.Enum,
                Language = CodeArtifactLanguage.CSharp,

                TypeName = typeName,
                Code = template.Render()
            };
        }
    }
}
