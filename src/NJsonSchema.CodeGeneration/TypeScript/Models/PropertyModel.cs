using NJsonSchema.CodeGeneration.Models;

namespace NJsonSchema.CodeGeneration.TypeScript.Models
{
    internal class PropertyModel : PropertyModelBase
    {
        public PropertyModel(JsonProperty property, TypeScriptTypeResolver resolver, TypeScriptGeneratorSettings settings, TypeScriptGenerator generator, string parentTypeName) 
            : base(property)
        {
            var propertyName = ConversionUtilities.ConvertToLowerCamelCase(property.Name).Replace("-", "_");
            var typeName = resolver.Resolve(property.ActualPropertySchema, property.IsNullable, property.Name); 

            Name = property.Name;
            InterfaceName = property.Name.Contains("-") ? '\"' + property.Name + '\"' : property.Name;
            PropertyName = propertyName;
            Type = typeName;
            DataConversionCode = GenerateDataConversionCode(property, settings, generator, propertyName, parentTypeName);
            Description = property.Description;
            HasDescription = !string.IsNullOrEmpty(property.Description);
            IsArray = property.ActualPropertySchema.Type.HasFlag(JsonObjectType.Array);
            ArrayItemType = resolver.TryResolve(property.ActualPropertySchema.Item, property.Name);
            IsReadOnly = property.IsReadOnly && settings.GenerateReadOnlyKeywords;
            IsOptional = !property.IsRequired;
        }

        private static string GenerateDataConversionCode(JsonProperty property, TypeScriptGeneratorSettings settings, 
            TypeScriptGenerator generator, string propertyName, string parentTypeName)
        {
            var typeStyle = settings.GetTypeStyle(parentTypeName);
            return typeStyle == TypeScriptTypeStyle.Interface ? string.Empty : generator.GenerateDataConversion(
                typeStyle == TypeScriptTypeStyle.Class ? "this." + propertyName : propertyName + "_",
                "data[\"" + property.Name + "\"]",
                property.ActualPropertySchema,
                property.IsNullable,
                property.Name);
        }

        public string Name { get; }

        public string InterfaceName { get; }

        public string PropertyName { get; }

        public string Type { get; }

        public string DataConversionCode { get; }

        public string Description { get; }

        public bool HasDescription { get; }

        public bool IsArray { get; }

        public string ArrayItemType { get; }

        public bool IsReadOnly { get; }

        public bool IsOptional { get; }
    }
}