using System;
using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration.Generators
{
    public class CSharpClassGenerator : GeneratorBase
    {
        private readonly JsonSchema4 _schema;
        private readonly Dictionary<string, CSharpClassGenerator> _types;

        public CSharpClassGenerator(JsonSchema4 schema)
            : this(schema, new Dictionary<string, CSharpClassGenerator>())
        {
        }

        private CSharpClassGenerator(JsonSchema4 schema, Dictionary<string, CSharpClassGenerator> types)
        {
            _schema = schema;
            _types = types;
        }

        public string Namespace { get; set; }

        public string GenerateFile()
        {
            var classes = GenerateClasses();

            var template = LoadTemplate("CSharp", "File");
            template.Add("namespace", Namespace);
            template.Add("classes", classes);
            return template.Render();
        }

        public string GenerateClasses()
        {
            var classes = GenerateMainClass();
            foreach (var type in _types.Values)
                classes += "\n\n" + type.GenerateMainClass();
            return classes;
        }

        private string GenerateMainClass()
        {
            var properties = _schema.Properties.Values.Select(p => new
            {
                Name = p.Name,
                PropertyName = ConvertToUpperStart(p.Name),
                FieldName = ConvertToLowerStart(p.Name),
                Required = p.IsRequired ? "Required.Always" : "Required.Default",
                Type = GetType(p, p.IsRequired)
            });

            var template = LoadTemplate("CSharp", "Class");
            template.Add("namespace", Namespace);
            template.Add("class", _schema.Title);
            template.Add("properties", properties);
            return template.Render();
        }

        protected string GetType(JsonSchema4 schema, bool isRequired)
        {
            var type = schema.Type;
            if (type.HasFlag(JsonObjectType.Array))
            {
                var property = (JsonProperty)schema;
                if (property.Item != null)
                    return string.Format("ObservableCollection<{0}>", GetType(property.Item, true));
                else
                    throw new NotImplementedException("Items not supported");
            }
            else
            {
                if (type.HasFlag(JsonObjectType.Number))
                    return isRequired ? "decimal" : "decimal?";

                if (type.HasFlag(JsonObjectType.Integer))
                    return isRequired ? "long" : "long?";

                if (type.HasFlag(JsonObjectType.Boolean))
                    return isRequired ? "bool" : "bool?";

                if (type.HasFlag(JsonObjectType.String))
                {
                    if (schema.Format == JsonFormatStrings.DateTime)
                        return isRequired ? "DateTime" : "DateTime?";
                    else
                        return "string";
                }

                if (type.HasFlag(JsonObjectType.Object))
                {
                    if (!string.IsNullOrEmpty(schema.Title))
                    {
                        if (!_types.ContainsKey(schema.Title))
                        {
                            var generator = new CSharpClassGenerator(schema, _types);
                            generator.Namespace = Namespace;
                            _types[schema.Title] = generator;
                        }

                        return schema.Title;
                    }
                    else
                        return "object";
                }

                throw new NotImplementedException("Type not supported");
            }
        }
    }
}
