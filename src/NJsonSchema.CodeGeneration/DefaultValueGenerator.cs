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
    public abstract class DefaultValueGenerator
    {
        private readonly ITypeResolver _typeResolver;
        private readonly IEnumNameGenerator _enumNameGenerator;

        /// <summary>Initializes a new instance of the <see cref="DefaultValueGenerator" /> class.</summary>
        /// <param name="typeResolver">The type typeResolver.</param>
        /// <param name="enumNameGenerator">The enum name generator.</param>
        protected DefaultValueGenerator(ITypeResolver typeResolver, IEnumNameGenerator enumNameGenerator)
        {
            _typeResolver = typeResolver;
            _enumNameGenerator = enumNameGenerator;
        }

        /// <summary>Gets the default value code.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="allowsNull">Specifies whether the default value assignment also allows null.</param>
        /// <param name="targetType">The type of the target.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <param name="useSchemaDefault">if set to <c>true</c> uses the default value from the schema if available.</param>
        /// <returns>The code.</returns>
        public virtual string GetDefaultValue(JsonSchema4 schema, bool allowsNull, string targetType, string typeNameHint, bool useSchemaDefault)
        {
            if (schema.Default == null || !useSchemaDefault)
                return null;

            var actualSchema = schema is JsonProperty ? ((JsonProperty)schema).ActualPropertySchema : schema.ActualSchema;
            if (actualSchema.IsEnumeration && !actualSchema.Type.HasFlag(JsonObjectType.Object) && actualSchema.Type != JsonObjectType.None)
                return GetEnumDefaultValue(schema, actualSchema, typeNameHint);

            if (schema.Type.HasFlag(JsonObjectType.String))
                return "\"" + ConversionUtilities.ConvertToStringLiteral(schema.Default.ToString()) + "\"";
            if (schema.Type.HasFlag(JsonObjectType.Boolean))
                return schema.Default.ToString().ToLowerInvariant();
            if (schema.Type.HasFlag(JsonObjectType.Integer) ||
                schema.Type.HasFlag(JsonObjectType.Number))
                return ConvertNumericValue(schema.Default);

            return null;
        }

        /// <summary>Converts the default value to a number literal. </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The number literal.</returns>
        protected abstract string ConvertNumericValue(object value);

        /// <summary>Gets the enum default value.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="actualSchema">The actual schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The enum default value.</returns>
        protected virtual string GetEnumDefaultValue(JsonSchema4 schema, JsonSchema4 actualSchema, string typeNameHint)
        {
            var typeName = _typeResolver.Resolve(actualSchema, false, typeNameHint);

            var index = actualSchema.Enumeration.ToList().IndexOf(schema.Default);
            var enumName = index >= 0 && actualSchema.EnumerationNames?.Count > index
                ? actualSchema.EnumerationNames.ElementAt(index)
                : schema.Default.ToString();

            return typeName + "." + _enumNameGenerator.Generate(index, enumName, schema.Default, actualSchema);
        }
    }
}