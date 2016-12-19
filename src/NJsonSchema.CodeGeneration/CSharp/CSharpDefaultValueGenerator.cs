//-----------------------------------------------------------------------
// <copyright file="CSharpDefaultValueGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using NJsonSchema.CodeGeneration.TypeScript;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Converts the default value to a TypeScript identifier.</summary>
    public class CSharpDefaultValueGenerator : DefaultValueGenerator
    {
        /// <summary>Initializes a new instance of the <see cref="TypeScriptDefaultValueGenerator"/> class.</summary>
        /// <param name="typeResolver">The type resolver.</param>
        public CSharpDefaultValueGenerator(ITypeResolver typeResolver) : base(typeResolver)
        {
        }

        /// <summary>Gets the default value code.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="allowsNull"></param>
        /// <param name="targetType">The type of the target.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The code.</returns>
        public override string GetDefaultValue(JsonSchema4 schema, bool allowsNull, string targetType, string typeNameHint)
        {
            var value = base.GetDefaultValue(schema, allowsNull, targetType, typeNameHint);
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
    }
}