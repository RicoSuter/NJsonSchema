//-----------------------------------------------------------------------
// <copyright file="TypeScriptDefaultValueGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Converts the default value to a TypeScript identifier.</summary>
    public class TypeScriptDefaultValueGenerator : DefaultValueGenerator
    {
        /// <summary>Initializes a new instance of the <see cref="TypeScriptDefaultValueGenerator"/> class.</summary>
        /// <param name="typeResolver">The type resolver.</param>
        public TypeScriptDefaultValueGenerator(ITypeResolver typeResolver) : base(typeResolver)
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
                    if (schema.Type.HasFlag(JsonObjectType.Array))
                        return "[]";

                    if (schema.IsDictionary)
                        return "{}";

                    if (schema.Type.HasFlag(JsonObjectType.Object))
                        return "new " + targetType + "()";
                }
            }
            return value;
        }
    }
}