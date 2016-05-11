using NJsonSchema.CodeGeneration.Models;

namespace NJsonSchema.CodeGeneration.TypeScript.Models
{
    internal class PropertyModel : PropertyModelBase
    {
        public PropertyModel(JsonProperty property, TypeScriptTypeResolver resolver, TypeScriptGeneratorSettings settings, TypeScriptGenerator generator) 
            : base(property)
        {
            var propertyName = ConversionUtilities.ConvertToLowerCamelCase(property.Name).Replace("-", "_");

            Name = property.Name;
            InterfaceName = property.Name.Contains("-") ? '\"' + property.Name + '\"' : property.Name;
            PropertyName = propertyName;
            Type = resolver.Resolve(property.ActualPropertySchema, property.IsNullable, property.Name);
            DataConversionCode = settings.TypeStyle == TypeScriptTypeStyle.Interface ? string.Empty : generator.GenerateDataConversion(
                settings.TypeStyle == TypeScriptTypeStyle.Class ? "this." + propertyName : propertyName,
                "data[\"" + property.Name + "\"]",
                property.ActualPropertySchema,
                property.IsNullable,
                property.Name);
            Description = property.Description;
            HasDescription = !string.IsNullOrEmpty(property.Description);
            IsArray = property.ActualPropertySchema.Type.HasFlag(JsonObjectType.Array);
            ArrayItemType = resolver.TryResolve(property.ActualPropertySchema.Item, property.Name);
            IsReadOnly = property.IsReadOnly && settings.GenerateReadOnlyKeywords;
            IsOptional = !property.IsRequired;
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