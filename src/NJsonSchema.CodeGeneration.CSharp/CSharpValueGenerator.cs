//-----------------------------------------------------------------------
// <copyright file="CSharpValueGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Converts the default value to a TypeScript identifier.</summary>
    public class CSharpValueGenerator : ValueGeneratorBase
    {
        private readonly CSharpGeneratorSettings _settings;

        /// <summary>Initializes a new instance of the <see cref="CSharpValueGenerator" /> class.</summary>
        /// <param name="typeResolver">The type resolver.</param>
        /// <param name="settings">The settings.</param>
        public CSharpValueGenerator(TypeResolverBase typeResolver, CSharpGeneratorSettings settings)
            : base(typeResolver, settings.EnumNameGenerator)
        {
            _settings = settings;
        }

        /// <summary>Gets the default value code.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="allowsNull">Specifies whether the default value assignment also allows null.</param>
        /// <param name="targetType">The type of the target.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <param name="useSchemaDefault">if set to <c>true</c> uses the default value from the schema if available.</param>
        /// <returns>The code.</returns>
        public override string GetDefaultValue(JsonSchema4 schema, bool allowsNull, string targetType, string typeNameHint, bool useSchemaDefault)
        {
            var value = base.GetDefaultValue(schema, allowsNull, targetType, typeNameHint, useSchemaDefault);
            if (value == null)
            {
                schema = schema.ActualSchema;
                if (schema != null && allowsNull == false)
                {
                    if (schema.Type.HasFlag(JsonObjectType.Array) ||
                        schema.Type.HasFlag(JsonObjectType.Object))
                        return "new " + targetType + "()";
                }
            }

            return value;
        }

        /// <summary>Converts the default value to a C# number literal. </summary>
        /// <param name="type">The JSON type.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="format">Optional schema format</param>
        /// <returns>The C# number literal.</returns>
        public override string GetNumericValue(JsonObjectType type, object value, string format)
        {
            var valueString = ConvertNumberToString(value);
            if (valueString != null)
            {
                switch (format)
                {
                    case JsonFormatStrings.Byte:
                        return "(byte)" + valueString;
                    case JsonFormatStrings.Integer:
                        return valueString;
                    case JsonFormatStrings.Long:
                        return valueString + "L";
                    case JsonFormatStrings.Double:
                        return valueString + "D";
                    case JsonFormatStrings.Float:
                        return valueString + "F";
                    case JsonFormatStrings.Decimal:
                        return valueString + "M";
                    default:
                        return type.HasFlag(JsonObjectType.Integer) ? valueString : valueString + "D";
                }
            }

            return null;
        }

        /// <summary>Gets the enum default value.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="actualSchema">The actual schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The enum default value.</returns>
        protected override string GetEnumDefaultValue(JsonSchema4 schema, JsonSchema4 actualSchema, string typeNameHint)
        {
            return _settings.Namespace + "." + base.GetEnumDefaultValue(schema, actualSchema, typeNameHint);
        }
    }
}