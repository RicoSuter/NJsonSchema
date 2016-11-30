//-----------------------------------------------------------------------
// <copyright file="TypeGeneratorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration
{
    /// <summary>Generates code for a type.</summary>
    public abstract class TypeGeneratorBase : GeneratorBase
    {
        /// <summary>Generates the type.</summary>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The code.</returns>
        public abstract TypeGeneratorResult GenerateType(string typeNameHint);

        /// <summary>Initializes a new instance of the <see cref="TypeGeneratorBase"/> class.</summary>
        /// <param name="schema">The schema.</param>
        protected TypeGeneratorBase(JsonSchema4 schema) : base(schema)
        {
        }
    }
}