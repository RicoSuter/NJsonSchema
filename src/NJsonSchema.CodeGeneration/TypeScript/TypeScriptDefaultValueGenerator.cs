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
        private readonly NullHandling _nullHandling;

        /// <summary>Initializes a new instance of the <see cref="TypeScriptDefaultValueGenerator"/> class.</summary>
        /// <param name="typeResolver">The type resolver.</param>
        /// <param name="nullHandling">The null handling.</param>
        public TypeScriptDefaultValueGenerator(ITypeResolver typeResolver, NullHandling nullHandling) : base(typeResolver)
        {
            _nullHandling = nullHandling;
        }

        /// <summary>Gets the default value code.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="targetType">The type of the target.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The code.</returns>
        public override string GetDefaultValue(JsonSchema4 schema, string targetType, string typeNameHint)
        {
            var value = base.GetDefaultValue(schema, targetType, typeNameHint);
            if (value == null)
            {
                schema = schema.ActualSchema;
                if (schema != null && schema.IsNullable(_nullHandling) == false)
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