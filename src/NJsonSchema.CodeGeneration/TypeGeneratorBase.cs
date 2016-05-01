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
        /// <returns>The code.</returns>
        public TypeGeneratorResult GenerateType()
        {
            return GenerateType(null);
        }

        /// <summary>Generates the type.</summary>
        /// <param name="fallbackTypeName">The type name hint.</param>
        /// <returns>The code.</returns>
        public abstract TypeGeneratorResult GenerateType(string fallbackTypeName);
    }
}