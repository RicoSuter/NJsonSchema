using System;
using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration.Generators
{
    public class CSharpClassGenerator : GeneratorBase
    {
        private readonly JsonSchema4 _schema;
        private readonly Dictionary<string, CSharpClassGenerator> _classes;

        public CSharpClassGenerator(JsonSchema4 schema)
            : this(schema, new Dictionary<string, CSharpClassGenerator>())
        {
        }

        private CSharpClassGenerator(JsonSchema4 schema, Dictionary<string, CSharpClassGenerator> classes)
        {
            _schema = schema;
            _classes = classes;
        }

        public string Namespace { get; set; }

        public string Generate()
        {
            return GenerateFile();
        }

        private string GenerateFile()
        {
            var classes = GenerateClass();
            foreach (var childClass in _classes.Values)
                classes += "\n\n" + childClass.GenerateClass();

            var template = LoadTemplate("CSharp", "File");
            template.Add("namespace", Namespace);
            template.Add("classes", classes);
            return template.Render();
        }

        private string GenerateClass()
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

        private string GetType(JsonSchema4 schema, bool isRequired)
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
                        if (!_classes.ContainsKey(schema.Title))
                        {
                            var generator = new CSharpClassGenerator(schema, _classes);
                            generator.Namespace = Namespace;
                            _classes[schema.Title] = generator;
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
