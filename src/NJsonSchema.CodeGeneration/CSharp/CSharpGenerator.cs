//-----------------------------------------------------------------------
// <copyright file="CSharpGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

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
        protected override string Language
        {
            get { return "CSharp"; }
        }

        /// <summary>Generates the file.</summary>
        /// <returns>The file contents.</returns>
        public override string GenerateFile()
        {
            var classes = GenerateType(_resolver.GenerateTypeName()).Code + "\n\n" + _resolver.GenerateTypes();

            var template = LoadTemplate("File");
            template.Add("namespace", Settings.Namespace);
            template.Add("classes", classes);
            return template.Render();
        }

        /// <summary>Generates the type.</summary>
        /// <param name="fallbackTypeName">The fallback type name when TypeName is not available on schema.</param>
        /// <returns>The code.</returns>
        public override TypeGeneratorResult GenerateType(string fallbackTypeName)
        {
            var typeName = !string.IsNullOrEmpty(_schema.TypeName) ? _schema.TypeName : fallbackTypeName;

            if (_schema.IsEnumeration)
            {
                var template = LoadTemplate("Enum");
                template.Add("name", typeName);
                template.Add("enums", GetEnumeration());

                template.Add("hasDescription", !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description));
                template.Add("description", RemoveLineBreaks(_schema.Description));

                return new TypeGeneratorResult
                {
                    TypeName = typeName,
                    Code = template.Render()
                };
            }
            else
            {
                var properties = _schema.Properties.Values.Select(property => new
                {
                    Name = property.Name,

                    HasDescription = !string.IsNullOrEmpty(property.Description),
                    Description = RemoveLineBreaks(property.Description),

                    PropertyName = ConvertToUpperCamelCase(property.Name),
                    FieldName = ConvertToLowerCamelCase(property.Name),

                    Required = property.IsRequired && Settings.RequiredPropertiesMustBeDefined ? "Required.Always" : "Required.Default",
                    IsStringEnum = property.ActualPropertySchema.IsEnumeration && property.ActualPropertySchema.Type == JsonObjectType.String,

                    Type = _resolver.Resolve(property.ActualPropertySchema, property.IsNullable, property.Name)
                }).ToList();

                var template = LoadTemplate("Class");
                template.Add("namespace", Settings.Namespace);
                template.Add("class", typeName);

                template.Add("hasDescription", !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description));
                template.Add("description", RemoveLineBreaks(_schema.Description));
                template.Add("inpc", Settings.ClassStyle == CSharpClassStyle.Inpc);

                var hasInheritance = _schema.AllOf.Count == 1;
                template.Add("hasInheritance", hasInheritance);
                template.Add("inheritance", hasInheritance ? ": " + _resolver.Resolve(_schema.AllOf.First(), false, string.Empty) +
                    (Settings.ClassStyle == CSharpClassStyle.Inpc ? ", INotifyPropertyChanged" : "") :
                    (Settings.ClassStyle == CSharpClassStyle.Inpc ? ": INotifyPropertyChanged" : ""));
                template.Add("properties", properties);

                return new TypeGeneratorResult
                {
                    TypeName = typeName,
                    Code = template.Render()
                };
            }
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
                    Value = _schema.Type == JsonObjectType.Integer ? value.ToString() : i.ToString(),
                    Name = ConvertToUpperCamelCase(name)
                });
            }
            return entries;
        }
    }
}
