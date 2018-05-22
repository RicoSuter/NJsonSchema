//-----------------------------------------------------------------------
// <copyright file="MultiTypeValidationError.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Validation
{
    /// <summary>A multi type validation error.</summary>
    public class MultiTypeValidationError : ValidationError
    {
        /// <summary>Initializes a new instance of the <see cref="ValidationError"/> class. </summary>
        /// <param name="kind">The error kind. </param>
        /// <param name="property">The property name. </param>
        /// <param name="path">The property path. </param>
        /// <param name="errors">The error list. </param>
        /// <param name="token">The token that failed to validate. </param>
        /// <param name="schema">The schema that contains the validation rule.</param>
#if !LEGACY
        public MultiTypeValidationError(ValidationErrorKind kind, string property, string path, IReadOnlyDictionary<JsonObjectType, ICollection<ValidationError>> errors, JToken token, JsonSchema4 schema)
#else
        public MultiTypeValidationError(ValidationErrorKind kind, string property, string path, IDictionary<JsonObjectType, ICollection<ValidationError>> errors, JToken token, JsonSchema4 schema)
#endif
            : base(kind, property, path, token, schema)
        {
            Errors = errors;
        }

        /// <summary>Gets the errors for each validated type. </summary>
#if !LEGACY
        public IReadOnlyDictionary<JsonObjectType, ICollection<ValidationError>> Errors { get; private set; }
#else
        public IDictionary<JsonObjectType, ICollection<ValidationError>> Errors { get; private set; }
#endif

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            var output = string.Format("{0}: {1}\n", Kind, Path);
            foreach (var error in Errors)
            {
                output += "{" + error.Key + ":\n";
                foreach (var validationError in error.Value)
                {
                    output += string.Format("  {0}\n", validationError.ToString().Replace("\n", "\n  "));
                }
                output += "}\n";
            }
            return output;
        }
    }
}