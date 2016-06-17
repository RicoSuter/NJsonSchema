//-----------------------------------------------------------------------
// <copyright file="DefaultValueGeneratorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration
{
    /// <summary>Converts the default value to a language specific identifier.</summary>
    public abstract class DefaultValueGeneratorBase
    {
        /// <summary>Gets the default value code.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>The code.</returns>
        public virtual string GetDefaultValue(JsonSchema4 schema)
        {
            if (schema.Default == null)
                return null;

            if (schema.Type.HasFlag(JsonObjectType.String))
                return "\"" + schema.Default + "\"";
            else if (schema.Type.HasFlag(JsonObjectType.Boolean))
                return schema.Default.ToString().ToLower();
            else if (schema.Type.HasFlag(JsonObjectType.Integer) ||
                     schema.Type.HasFlag(JsonObjectType.Number) ||
                     schema.Type.HasFlag(JsonObjectType.Integer))
                return schema.Default.ToString();
            return null;
        }
    }
}