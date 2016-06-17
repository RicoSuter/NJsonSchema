//-----------------------------------------------------------------------
// <copyright file="DefaultValueGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>Converts the default value to a language specific identifier.</summary>
    public class DefaultValueGenerator
    {
        private readonly CodeGeneratorSettingsBase _settings;

        /// <summary>Initializes a new instance of the <see cref="DefaultValueGenerator" /> class.</summary>
        /// <param name="settings">The settings.</param>
        public DefaultValueGenerator(CodeGeneratorSettingsBase settings)
        {
            _settings = settings;
        }

        /// <summary>Gets the default value code.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>The code.</returns>
        public virtual string GetDefaultValue(JsonSchema4 schema)
        {
            if (schema.Default == null)
                return null;

            var actualSchema = schema is JsonProperty ? ((JsonProperty)schema).ActualPropertySchema : schema.ActualSchema;
            if (actualSchema.IsEnumeration)
            {
                var typeName = actualSchema.GetTypeName(_settings.TypeNameGenerator);
                var enumName = schema.Default is string ?
                    schema.Default.ToString() :
                    actualSchema.EnumerationNames[actualSchema.Enumeration.ToList().IndexOf(schema.Default)];

                return typeName + "." + ConversionUtilities.ConvertToUpperCamelCase(enumName);
            }

            if (schema.Type.HasFlag(JsonObjectType.String))
                return "\"" + schema.Default + "\"";
            else if (schema.Type.HasFlag(JsonObjectType.Boolean))
                return schema.Default.ToString().ToLower();
            else if (schema.Type.HasFlag(JsonObjectType.Integer) ||
                     schema.Type.HasFlag(JsonObjectType.Number) ||
                     schema.Type.HasFlag(JsonObjectType.Integer))
                return schema.Default.ToString();
            return null;
        }
    }
}