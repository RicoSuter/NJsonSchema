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
        /// <param name="rootTypeNameHint">The root type name hint.</param>
        /// <returns>The file contents.</returns>
        public override string GenerateFile(string rootTypeNameHint)
        {
            _resolver.Resolve(_schema, false, rootTypeNameHint); // register root type

            var model = new FileTemplateModel
            {
                Namespace = Settings.Namespace ?? string.Empty,
                Classes = ConversionUtilities.TrimWhiteSpaces(_resolver.GenerateClasses())
            };

            var template = Settings.TemplateFactory.CreateTemplate("CSharp", "File", model);
            return ConversionUtilities.TrimWhiteSpaces(template.Render());
        }

        /// <summary>Generates the type.</summary>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The code.</returns>
        public override TypeGeneratorResult GenerateType(string typeNameHint)
        {
            var typeName = _resolver.GetOrGenerateTypeName(_schema, typeNameHint);

            if (_schema.IsEnumeration)
                return GenerateEnum(typeName);
            else
                return GenerateClass(typeName);
        }

        private TypeGeneratorResult GenerateClass(string typeName)
        {
            var model = new ClassTemplateModel(typeName, Settings, _resolver, _schema);

            RenamePropertyWithSameNameAsClass(typeName, model.Properties);

            var template = Settings.TemplateFactory.CreateTemplate("CSharp", "Class", model);
            return new TypeGeneratorResult
            {
                TypeName = typeName,
                BaseTypeName = model.BaseClass,
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

        private TypeGeneratorResult GenerateEnum(string typeName)
        {
            var model = new EnumTemplateModel(typeName, _schema, Settings);
            var template = Settings.TemplateFactory.CreateTemplate("CSharp", "Enum", model);
            return new TypeGeneratorResult
            {
                TypeName = typeName,
                Code = template.Render()
            };
        }
    }
}
