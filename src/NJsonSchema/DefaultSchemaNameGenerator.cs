//-----------------------------------------------------------------------
// <copyright file="DefaultSchemaNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using NJsonSchema.Annotations;
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    /// <summary>The default schema name generator implementation.</summary>
    public class DefaultSchemaNameGenerator : ISchemaNameGenerator
    {
        /// <summary>Generates the name of the JSON Schema.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The new name.</returns>
        public virtual string Generate(Type type)
        {
            var attribute = type.GetTypeInfo().GetCustomAttribute<JsonSchemaAttribute>();
            if (!string.IsNullOrEmpty(attribute?.Name))
                return attribute.Name; 

            return ReflectionExtensions.GetSafeTypeName(type);
        }
    }
}