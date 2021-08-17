//-----------------------------------------------------------------------
// <copyright file="CSharpPropertyNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Generates the property name for a given CSharp <see cref="JsonSchemaProperty"/>.</summary>
    public class CSharpPropertyNameGenerator : IPropertyNameGenerator
    {
        private readonly Func<string, string> _propertyNameGenerator;

        /// <summary>Initializes a new instance of the <see cref="CSharpPropertyNameGenerator" /> class.</summary>
        /// <param name="settings">The settings.</param>
        public CSharpPropertyNameGenerator(CSharpGeneratorSettings settings)
        {
            _propertyNameGenerator = GetPropertyNameGenerator(settings);
        }

        private static Func<string, string> GetPropertyNameGenerator(CSharpGeneratorSettings settings)
        {
            switch (settings.PropertyNamingStyle)
            {
                case CSharpNamingStyle.FlatCase:
                    return ConversionUtilities.ConvertNameToFlatCase;

                case CSharpNamingStyle.UpperFlatCase:
                    return ConversionUtilities.ConvertNameToUpperFlatCase;

                case CSharpNamingStyle.CamelCase:
                    return ConversionUtilities.ConvertNameToCamelCase;

                case CSharpNamingStyle.PascalCase:
                    return ConversionUtilities.ConvertNameToPascalCase;

                case CSharpNamingStyle.SnakeCase:
                    return ConversionUtilities.ConvertNameToSnakeCase;

                case CSharpNamingStyle.PascalSnakeCase:
                    return ConversionUtilities.ConvertNameToPascalSnakeCase;

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>Generates the property name.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The new name.</returns>
        public virtual string Generate(JsonSchemaProperty property)
            => _propertyNameGenerator(property.Name);
    }
}
