//-----------------------------------------------------------------------
// <copyright file="CSharpClassGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

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
            return GenerateType(_resolver.GenerateTypeName()).Code + "\n\n" + _resolver.GenerateTypes();
        }

        /// <summary>Generates the type.</summary>
        /// <param name="fallbackTypeName">The fallback type name.</param>
        /// <returns>The code.</returns>
        public override TypeGeneratorResult GenerateType(string fallbackTypeName)
        {
            var typeName = !string.IsNullOrEmpty(_schema.TypeName) ? _schema.TypeName : fallbackTypeName;

            if (_schema.IsEnumeration)
            {
                var template = LoadTemplate("Enum");

                if (_schema.Type == JsonObjectType.Integer)
                    typeName = typeName + "AsInteger";

                template.Add("name", typeName);
                template.Add("enums", GetEnumeration());

                template.Add("hasDescription", !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description));
                template.Add("description", ConversionUtilities.RemoveWhiteSpaces(_schema.Description));

                return new TypeGeneratorResult
                {
                    TypeName = typeName,
                    Code = template.Render()
                };
            }
            else
            {
                var properties = _schema.Properties.Values.Select(property =>
                {
                    var propertyName = ConversionUtilities.ConvertToLowerCamelCase(property.Name).Replace("-", "_");
                    return new
                    {
                        Name = property.Name,
                        InterfaceName = property.Name.Contains("-") ? '\"' + property.Name + '\"' : property.Name,
                        PropertyName = propertyName,

                        Type = _resolver.Resolve(property.ActualPropertySchema, property.IsNullable, property.Name),

                        DataConversionCode = Settings.TypeStyle == TypeScriptTypeStyle.Interface ? string.Empty : GenerateDataConversion(
                            Settings.TypeStyle == TypeScriptTypeStyle.Class ? "this." + propertyName : propertyName,
                            "data[\"" + property.Name + "\"]",
                            property.ActualPropertySchema,
                            property.IsNullable,
                            property.Name),

                        Description = property.Description,
                        HasDescription = !string.IsNullOrEmpty(property.Description),

                        IsReadOnly = property.IsReadOnly && Settings.GenerateReadOnlyKeywords,
                        IsOptional = !property.IsRequired
                    };
                }).ToList();

                var template = LoadTemplate(Settings.TypeStyle.ToString());
                template.Add("class", typeName);

                template.Add("hasDescription", !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description));
                template.Add("description", ConversionUtilities.RemoveWhiteSpaces(_schema.Description));

                var hasInheritance = _schema.AllOf.Count == 1;
                template.Add("hasInheritance", hasInheritance);
                template.Add("inheritance", hasInheritance ? " extends " + _resolver.Resolve(_schema.AllOf.First(), true, string.Empty) : string.Empty);
                template.Add("properties", properties);

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

            var template = LoadTemplate("DataConversion");
            template.Add("variable", variable);
            template.Add("value", value);
            template.Add("property", new
            {
                Type = _resolver.Resolve(schema, isPropertyNullable, typeNameHint),

                IsNewableObject = IsNewableObject(schema),
                IsDate = schema.Format == JsonFormatStrings.DateTime,

                IsDictionary = schema.IsDictionary,
                DictionaryValueType = TryResolve(schema.AdditionalPropertiesSchema, typeNameHint),
                IsDictionaryValueNewableObject = schema.AdditionalPropertiesSchema != null && IsNewableObject(schema.AdditionalPropertiesSchema),
                IsDictionaryValueDate = schema.AdditionalPropertiesSchema?.Format == JsonFormatStrings.DateTime,

                IsArray = schema.Type.HasFlag(JsonObjectType.Array),
                ArrayItemType = TryResolve(schema.Item, typeNameHint),
                IsArrayItemNewableObject = schema.Item != null && IsNewableObject(schema.Item),
                IsArrayItemDate = schema.Item?.Format == JsonFormatStrings.DateTime
            });

            var output = template.Render();
            return output.Trim('\n', '\r');
        }

        private string TryResolve(JsonSchema4 schema, string typeNameHint)
        {
            return schema != null ? _resolver.Resolve(schema, false, typeNameHint) : string.Empty;
        }

        private static bool IsNewableObject(JsonSchema4 schema)
        {
            schema = schema.ActualSchema;
            return schema.Type.HasFlag(JsonObjectType.Object) && !schema.IsAnyType && !schema.IsDictionary;
        }

        private List<EnumerationEntry> GetEnumeration()
        {
            var entries = new List<EnumerationEntry>();
            for (int i = 0; i < _schema.Enumeration.Count; i++)
            {
                var value = _schema.Enumeration.ElementAt(i);
                var name = _schema.EnumerationNames.Count > i ?
                    _schema.EnumerationNames.ElementAt(i) :
                    _schema.Type == JsonObjectType.Integer ? "Value" + value : value.ToString();

                entries.Add(new EnumerationEntry
                {
                    Value = _schema.Type == JsonObjectType.Integer ? value.ToString() : "<any>\"" + value + "\"",
                    Name = ConversionUtilities.ConvertToUpperCamelCase(name)
                });
            }
            return entries;
        }
    }
}
