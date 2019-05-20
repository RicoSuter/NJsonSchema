//-----------------------------------------------------------------------
// <copyright file="DefaultSchemaNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;
using Namotion.Reflection;
using Newtonsoft.Json;
using NJsonSchema.Annotations;

namespace NJsonSchema.Generation
{
    /// <summary>The default schema name generator implementation.</summary>
    public class DefaultSchemaNameGenerator : ISchemaNameGenerator
    {
        /// <summary>Generates the name of the JSON Schema.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The new name.</returns>
        public virtual string Generate(Type type)
        {
            var cachedType = type.ToCachedType();

            var jsonSchemaAttribute = cachedType.GetTypeAttribute<JsonSchemaAttribute>();
            if (!string.IsNullOrEmpty(jsonSchemaAttribute?.Name))
            {
                return jsonSchemaAttribute.Name;
            }

            //var jsonObjectAttribute = cachedType.GetTypeAttribute<JsonObjectAttribute>();
            //if (!string.IsNullOrEmpty(jsonObjectAttribute.Title) && Regex.IsMatch(jsonObjectAttribute.Title, "^[a-zA-Z0-9_]*$"))
            //{
            //    return jsonObjectAttribute.Title;
            //}

            return type.GetDisplayName();
        }
    }
}