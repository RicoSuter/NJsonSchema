//-----------------------------------------------------------------------
// <copyright file="CSharpClassGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

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
            : this(schema, new TypeScriptTypeResolver())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TypeScriptGenerator"/> class.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="resolver">The resolver.</param>
        public TypeScriptGenerator(JsonSchema4 schema, TypeScriptTypeResolver resolver)
        {
            _schema = schema;
            _resolver = resolver;
        }

        /// <summary>Gets the language.</summary>
        protected override string Language
        {
            get { return "TypeScript"; }
        }

        /// <summary>Generates the file.</summary>
        /// <returns>The file contents.</returns>
        public override string GenerateFile()
        {
            return GenerateType(string.Empty) + "\n\n" + _resolver.GenerateTypes();
        }

        /// <summary>Generates the type.</summary>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The code.</returns>
        public override string GenerateType(string typeNameHint)
        {
            var typeName = !string.IsNullOrEmpty(_schema.TypeName) ? _schema.TypeName : typeNameHint;

            if (_schema.IsEnumeration)
            {
                var template = LoadTemplate("Enum");

                if (_schema.Type == JsonObjectType.Integer)
                    typeName = typeName + "AsInteger";

                template.Add("name", typeName);
                template.Add("enums", GetEnumeration());

                template.Add("hasDescription", !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description));
                template.Add("description", RemoveLineBreaks(_schema.Description));

                return template.Render();
            }
            else
            {
                var properties = _schema.Properties.Values.Select(property => new
                {
                    Name = property.Name,
                    Type = _resolver.Resolve(property, property.IsRequired, property.Name),

                    HasDescription = !string.IsNullOrEmpty(property.Description),
                    Description = property.Description,

                    IsOptional = !property.IsRequired
                }).ToList();

                var template = LoadTemplate("Interface");
                template.Add("class", typeName);

                template.Add("hasDescription", !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description));
                template.Add("description", RemoveLineBreaks(_schema.Description));

                template.Add("inheritance", _schema.AllOf.Count == 1 ? " extends " + _resolver.Resolve(_schema.AllOf.First(), true, string.Empty) : string.Empty);
                template.Add("properties", properties);
                return template.Render();
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
                    Value = _schema.Type == JsonObjectType.Integer ? value.ToString() : "<any>\"" + value + "\"", 
                    Name = ConvertToUpperStartIdentifier(name)
                });
            }
            return entries;
        }
    }
}
