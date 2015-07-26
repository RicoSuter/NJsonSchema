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
    /// <summary>The TypeScript interface code generator. </summary>
    public class TypeScriptInterfaceGenerator : GeneratorBase
    {
        private readonly JsonSchema4 _schema;
        private readonly TypeScriptTypeResolver _resolver;

        /// <summary>Initializes a new instance of the <see cref="TypeScriptInterfaceGenerator"/> class.</summary>
        /// <param name="schema">The schema.</param>
        public TypeScriptInterfaceGenerator(JsonSchema4 schema)
            : this(schema, new TypeScriptTypeResolver())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TypeScriptInterfaceGenerator"/> class.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="resolver">The resolver.</param>
        public TypeScriptInterfaceGenerator(JsonSchema4 schema, TypeScriptTypeResolver resolver)
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
        public string GenerateFile()
        {
            return GenerateInterfaces();
        }

        /// <summary>Generates the interfaces.</summary>
        /// <returns>The interfaces.</returns>
        public string GenerateInterfaces()
        {
            var classes = GenerateInterface();
            foreach (var type in _resolver.Types)
                classes += "\n\n" + type.GenerateInterface();
            return classes;
        }

        private string GenerateInterface()
        {
            var properties = _schema.Properties.Values.Select(property => new
            {
                Name = property.Name,
                Type = _resolver.Resolve(property),
                IsOptional = !property.IsRequired
            }).ToList();

            var template = LoadTemplate("Interface");
            template.Add("class", _schema.TypeName);
            template.Add("properties", properties);
            return template.Render();
        }
    }
}
