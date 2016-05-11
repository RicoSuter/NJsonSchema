//-----------------------------------------------------------------------
// <copyright file="CSharpClassGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
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
            var output = GenerateType(_resolver.GenerateTypeName()).Code + "\n\n" + _resolver.GenerateTypes();
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
                var properties = _schema.Properties.Values.Select(property => new PropertyModel(property, _resolver, Settings, this)).ToList();
                var hasInheritance = _schema.AllOf.Count == 1;

                var template = Settings.CreateTemplate();
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

        /// <summary>Generates the code to convert a data object to the target class instances.</summary>
        /// <param name="variable">The variable to assign the converted value to.</param>
        /// <param name="value">The variable containing the original value.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="isPropertyNullable">Value indicating whether the value is nullable.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The generated code.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null" />.</exception>
        public string GenerateDataConversion(string variable, string value, JsonSchema4 schema, bool isPropertyNullable, string typeNameHint)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            var template = new DataConversionTemplate() as ITemplate;
            template.Initialize(new // TODO: Create model class
            {
                Variable = variable,
                Value = value,

                Type = _resolver.Resolve(schema, isPropertyNullable, typeNameHint),

                IsNewableObject = IsNewableObject(schema),
                IsDate = schema.Format == JsonFormatStrings.DateTime,

                IsDictionary = schema.IsDictionary,
                DictionaryValueType = _resolver.TryResolve(schema.AdditionalPropertiesSchema, typeNameHint),
                IsDictionaryValueNewableObject = schema.AdditionalPropertiesSchema != null && IsNewableObject(schema.AdditionalPropertiesSchema),
                IsDictionaryValueDate = schema.AdditionalPropertiesSchema?.Format == JsonFormatStrings.DateTime,

                IsArray = schema.Type.HasFlag(JsonObjectType.Array),
                ArrayItemType = _resolver.TryResolve(schema.Item, typeNameHint),
                IsArrayItemNewableObject = schema.Item != null && IsNewableObject(schema.Item),
                IsArrayItemDate = schema.Item?.Format == JsonFormatStrings.DateTime
            });
            return template.Render();
        }

        private static bool IsNewableObject(JsonSchema4 schema)
        {
            schema = schema.ActualSchema;
            return schema.Type.HasFlag(JsonObjectType.Object) && !schema.IsAnyType && !schema.IsDictionary;
        }
    }
}
