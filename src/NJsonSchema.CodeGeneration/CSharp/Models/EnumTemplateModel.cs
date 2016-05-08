using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration.CSharp.Models
{
    internal class EnumTemplateModel
    {
        public EnumTemplateModel(string typeName, JsonSchema4 schema)
        {
            Name = typeName;
            Enums = GetEnumeration(schema);

            HasDescription = !(schema is JsonProperty) && !string.IsNullOrEmpty(schema.Description);
            Description = ConversionUtilities.RemoveWhiteSpaces(schema.Description);
        }

        public string Name { get; }

        public List<EnumerationEntry> Enums { get; }

        public bool HasDescription { get; }

        public string Description { get; }

        private List<EnumerationEntry> GetEnumeration(JsonSchema4 schema)
        {
            var entries = new List<EnumerationEntry>();
            for (int i = 0; i < schema.Enumeration.Count; i++)
            {
                var value = schema.Enumeration.ElementAt(i);
                var name = schema.EnumerationNames.Count > i ?
                    schema.EnumerationNames.ElementAt(i) :
                    schema.Type == JsonObjectType.Integer ? "Value" + value : value.ToString();

                entries.Add(new EnumerationEntry
                {
                    Value = schema.Type == JsonObjectType.Integer ? value.ToString() : i.ToString(),
                    Name = ConversionUtilities.ConvertToUpperCamelCase(name)
                });
            }
            return entries;
        }
    }
}