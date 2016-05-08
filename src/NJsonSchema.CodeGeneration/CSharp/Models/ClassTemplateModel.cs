using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration.CSharp.Models
{
    internal class ClassTemplateModel
    {
        public ClassTemplateModel(string typeName, CSharpGeneratorSettings settings, CSharpTypeResolver resolver, JsonSchema4 schema, List<PropertyModel> properties)
        {
            Class = typeName;
            Namespace = settings.Namespace;

            HasDescription = !(schema is JsonProperty) && !string.IsNullOrEmpty(schema.Description);
            Description = ConversionUtilities.RemoveWhiteSpaces(schema.Description);

            Inpc = settings.ClassStyle == CSharpClassStyle.Inpc;

            var hasInheritance = schema.AllOf.Count == 1;
            HasInheritance = hasInheritance;
            Inheritance = GenerateInheritanceCode(settings, resolver, schema, hasInheritance);

            Properties = properties;
        }

        public string Namespace { get; set; }

        public string Class { get; set; }

        public bool HasDescription { get; }

        public string Description { get; }

        public bool Inpc { get; set; }

        public string Inheritance { get; set; }

        public bool HasInheritance { get; set; }

        public List<PropertyModel> Properties { get; set; }

        private static string GenerateInheritanceCode(CSharpGeneratorSettings settings, CSharpTypeResolver resolver, JsonSchema4 schema, bool hasInheritance)
        {
            if (hasInheritance)
                return ": " + resolver.Resolve(schema.AllOf.First(), false, string.Empty) + 
                    (settings.ClassStyle == CSharpClassStyle.Inpc ? ", INotifyPropertyChanged" : "");
            else
                return settings.ClassStyle == CSharpClassStyle.Inpc ? ": INotifyPropertyChanged" : "";
        }
    }
}