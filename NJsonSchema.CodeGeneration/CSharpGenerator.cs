using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.StringTemplate;

namespace NJsonSchema.CodeGeneration
{
    public class CSharpGenerator : GeneratorBase
    {
        private readonly JsonSchema4 _schema;
        private readonly Dictionary<string, CSharpGenerator> _classes;

        public CSharpGenerator(JsonSchema4 schema) 
            : this(schema, new Dictionary<string, CSharpGenerator>())
        {
        }

        private CSharpGenerator(JsonSchema4 schema, Dictionary<string, CSharpGenerator> classes)
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
            var template = LoadTemplate("CSharp", "Class");
            template.Add("namespace", Namespace);
            template.Add("class", _schema.Title);
            template.Add("properties", _schema.Properties.Values.Select(p => new
            {
                Key = p.Key,
                Type = GetType(p, p.IsRequired)
            }));
            return template.Render();
        }

        private string GetType(JsonSchema4 schema, bool isRequired)
        {
            var type = schema.Type;
            if (type.HasFlag(JsonObjectType.Array))
            {
                var property = (JsonProperty) schema;
                if (property.Item != null)
                {
                    return GetType(property.Item, true) + "[]";
                }
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
                    return "string";

                if (type.HasFlag(JsonObjectType.Object))
                {
                    if (!string.IsNullOrEmpty(schema.Title))
                    {
                        if (!_classes.ContainsKey(schema.Title))
                        {
                            var generator = new CSharpGenerator(schema, _classes);
                            generator.Namespace = Namespace;
                            _classes[schema.Title] = generator;
                        }

                        return schema.Title;
                    }
                    else
                        return "object";
                }

                return "object";
            }
        }
    }
}
