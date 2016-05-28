//-----------------------------------------------------------------------
// <copyright file="PropertyModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using NJsonSchema.CodeGeneration.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Models
{
    internal class PropertyModel : PropertyModelBase
    {
        internal PropertyModel(JsonProperty property, CSharpTypeResolver resolver, CSharpGeneratorSettings settings) : base(property)
        {
            Name = property.Name;
            HasDescription = !string.IsNullOrEmpty(property.Description);
            Description = property.Description;
            PropertyName = ConversionUtilities.ConvertToUpperCamelCase(property.Name);
            FieldName = ConversionUtilities.ConvertToLowerCamelCase(property.Name);
            Required = property.IsRequired && settings.RequiredPropertiesMustBeDefined ? "Required.Always" : "Required.Default";
            IsStringEnum = property.ActualPropertySchema.IsEnumeration && property.ActualPropertySchema.Type == JsonObjectType.String;
            Type = resolver.Resolve(property.ActualPropertySchema, property.IsNullable, property.Name);
        }

        public string Name { get; }

        public bool HasDescription { get; }

        public string Description { get; }

        public string PropertyName { get; }

        public string FieldName { get; }

        public string Required { get; }

        public bool IsStringEnum { get; }

        public string Type { get; }
    }
}