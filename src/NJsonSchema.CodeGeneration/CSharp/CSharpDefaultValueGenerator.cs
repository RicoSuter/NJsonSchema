//-----------------------------------------------------------------------
// <copyright file="CSharpDefaultValueGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Converts the default value to a language specific identifier.</summary>
    public class CSharpDefaultValueGenerator : DefaultValueGeneratorBase
    {
        private readonly CSharpGeneratorSettings _settings;

        /// <summary>Initializes a new instance of the <see cref="CSharpDefaultValueGenerator" /> class.</summary>
        /// <param name="settings">The settings.</param>
        public CSharpDefaultValueGenerator(CSharpGeneratorSettings settings)
        {
            _settings = settings;
        }

        /// <summary>Gets the default value code.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>The code.</returns>
        public override string GetDefaultValue(JsonSchema4 schema)
        {
            if (schema.Default != null)
            {
                var actualSchema = schema is JsonProperty ? ((JsonProperty)schema).ActualPropertySchema : schema.ActualSchema;
                if (actualSchema.IsEnumeration)
                {
                    var typeName = actualSchema.GetTypeName(_settings.TypeNameGenerator);
                    var enumName = schema.Default is string ? 
                        schema.Default.ToString() :
                        actualSchema.EnumerationNames[actualSchema.Enumeration.ToList().IndexOf(schema.Default)];

                    return typeName + "." + ConversionUtilities.ConvertToUpperCamelCase(enumName);
                }
            }

            return base.GetDefaultValue(schema);
        }
    }
}