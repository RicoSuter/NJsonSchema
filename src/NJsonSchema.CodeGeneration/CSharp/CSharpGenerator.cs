//-----------------------------------------------------------------------
// <copyright file="CSharpGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

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
            var classes = GenerateType(string.Empty) + "\n\n" + _resolver.GenerateTypes();

            var template = LoadTemplate("File");
            template.Add("namespace", Settings.Namespace);
            template.Add("classes", classes);
            return template.Render();
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

                template.Add("hasDescription", !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description));
                template.Add("description", RemoveLineBreaks(_schema.Description));

                return template.Render();
            }
            else
            {
                var properties = _schema.Properties.Values.Select(property => new
                {
                    Name = property.Name,

                    HasDescription = !string.IsNullOrEmpty(property.Description), 
                    Description = RemoveLineBreaks(property.Description), 

                    PropertyName = ConvertToUpperStartIdentifier(property.Name),
                    FieldName = ConvertToLowerStartIdentifier(property.Name),
                    Required = property.IsRequired && Settings.RequiredPropertiesMustBeDefined ? "Required.Always" : "Required.Default",
                    Type = _resolver.Resolve(property, property.IsRequired, property.Name)
                }).ToList();

                var template = LoadTemplate("Class");
                template.Add("namespace", Settings.Namespace);
                template.Add("class", _schema.TypeName);

                template.Add("hasDescription", !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description));
                template.Add("description", RemoveLineBreaks(_schema.Description));

                template.Add("inheritance", _schema.AllOf.Count == 1 ? _resolver.Resolve(_schema.AllOf.First(), true, string.Empty) + ", " : string.Empty);
                template.Add("properties", properties);
                return template.Render();
            }
        }
    }
}
