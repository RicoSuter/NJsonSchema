//-----------------------------------------------------------------------
// <copyright file="TypeScriptDefaultValueGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Globalization;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Converts the default value to a TypeScript identifier.</summary>
    public class TypeScriptDefaultValueGenerator : DefaultValueGenerator
    {
        /// <summary>Initializes a new instance of the <see cref="TypeScriptDefaultValueGenerator"/> class.</summary>
        /// <param name="typeResolver">The type resolver.</param>
        public TypeScriptDefaultValueGenerator(ITypeResolver typeResolver, TypeScriptGeneratorSettings settings) 
            : base(typeResolver, settings.EnumNameGenerator)
        {
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
                    if (schema.IsArray)
                        return "[]";

                    if (schema.IsDictionary)
                        return "{}";

                    if (schema.Type.HasFlag(JsonObjectType.Object))
                        return "new " + targetType + "()";
                }
            }
            return value;
        }

        /// <summary>Converts the default value to a TypeScript number literal. </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="format">Optional schema format</param>
        /// <returns>The TypeScript number literal.</returns>
        protected override string ConvertNumericValue(object value, string format)
        {
            if (value is byte) return ((byte)value).ToString(CultureInfo.InvariantCulture);
            if (value is sbyte) return ((sbyte)value).ToString(CultureInfo.InvariantCulture);
            if (value is short) return ((short)value).ToString(CultureInfo.InvariantCulture);
            if (value is ushort) return ((ushort)value).ToString(CultureInfo.InvariantCulture);
            if (value is int) return ((int)value).ToString(CultureInfo.InvariantCulture);
            if (value is uint) return ((uint)value).ToString(CultureInfo.InvariantCulture);
            if (value is long) return ((long)value).ToString(CultureInfo.InvariantCulture);
            if (value is ulong) return ((ulong)value).ToString(CultureInfo.InvariantCulture);
            if (value is float) return ((float)value).ToString("r", CultureInfo.InvariantCulture);
            if (value is double) return ((double)value).ToString("r", CultureInfo.InvariantCulture);
            if (value is decimal) return ((decimal)value).ToString(CultureInfo.InvariantCulture);
            return null;
        }
    }
}