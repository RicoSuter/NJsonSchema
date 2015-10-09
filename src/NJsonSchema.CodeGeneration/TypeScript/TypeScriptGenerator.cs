//-----------------------------------------------------------------------
// <copyright file="CSharpClassGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

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
            if (_schema.Type == JsonObjectType.String && _schema.Enumeration.Count > 0)
            {
                var template = LoadTemplate("Enum");
                template.Add("name", !string.IsNullOrEmpty(_schema.TypeName) ? _schema.TypeName : typeNameHint);
                template.Add("enums", _schema.Enumeration);
                return template.Render();
            }
            else
            {
                var properties = _schema.Properties.Values.Select(property => new
                {
                    Name = property.Name,
                    Type = _resolver.Resolve(property, property.IsRequired, property.Name),
                    IsOptional = !property.IsRequired
                }).ToList();

                var template = LoadTemplate("Interface");
                template.Add("class", _schema.TypeName);
                template.Add("inheritance", _schema.AllOf.Count == 1 ? " extends " + _resolver.Resolve(_schema.AllOf.First(), true, string.Empty) : string.Empty);
                template.Add("properties", properties);
                return template.Render();
            }
        }
    }
}
