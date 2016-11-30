//-----------------------------------------------------------------------
// <copyright file="GeneratorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The base class of the code generators</summary>
    public abstract class GeneratorBase
    {
        private readonly JsonSchema4 _schema;

        /// <summary>Initializes a new instance of the <see cref="GeneratorBase"/> class.</summary>
        /// <param name="schema">The schema.</param>
        protected GeneratorBase(JsonSchema4 schema)
        {
            _schema = schema; 
        }

        /// <summary>Generates the the whole file containing all needed types.</summary>
        /// <returns>The code</returns>
        public string GenerateFile()
        {
            return GenerateFile(Regex.IsMatch(_schema.Title, "^[a-zA-Z0-9_]*$") ? _schema.Title : null);
        }

        /// <summary>Generates the the whole file containing all needed types.</summary>
        /// <param name="rootTypeNameHint">The root type name hint.</param>
        /// <returns>The code</returns>
        public abstract string GenerateFile(string rootTypeNameHint);
    }
}