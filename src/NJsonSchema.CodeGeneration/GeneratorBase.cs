//-----------------------------------------------------------------------
// <copyright file="GeneratorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The base class of the code generators</summary>
    public abstract class GeneratorBase
    {
        /// <summary>Generates the the whole file containing all needed types.</summary>
        /// <param name="rootTypeNameHint">The root type name hint.</param>
        /// <returns>The code</returns>
        public abstract string GenerateFile(string rootTypeNameHint);
    }
}