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
            var jsonSchemaAttribute = type.GetTypeInfo().GetCustomAttribute<JsonSchemaAttribute>();
            if (!string.IsNullOrEmpty(jsonSchemaAttribute?.Name))
                return jsonSchemaAttribute.Name;

            //var jsonObjectAttribute = type.GetTypeInfo().GetCustomAttribute<JsonObjectAttribute>();
            //if (!string.IsNullOrEmpty(jsonObjectAttribute.Title) && Regex.IsMatch(jsonObjectAttribute.Title, "^[a-zA-Z0-9_]*$"))
            //    return jsonObjectAttribute.Title;

            return ReflectionExtensions.GetSafeTypeName(type);
        }
    }
}