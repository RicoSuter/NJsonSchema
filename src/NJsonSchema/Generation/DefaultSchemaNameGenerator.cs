﻿//-----------------------------------------------------------------------
// <copyright file="DefaultSchemaNameGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
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

            var jsonSchemaAttribute = cachedType.GetAttribute<JsonSchemaAttribute>(true);

            if (!string.IsNullOrEmpty(jsonSchemaAttribute?.Name))
            {
                return jsonSchemaAttribute!.Name!;
            }

            var nType = type.ToCachedType();

            if (nType.Type.IsConstructedGenericType)
            {
                return GetName(nType).Split('`').First() + "Of" +
                       string.Join("And", nType.GenericArguments
                           .Select(a => Generate(a.OriginalType)));
            }

            return GetName(nType);
        }

        private static string GetName(CachedType cType)
        {
            return
                cType.Name == "Int16" ? GetNullableDisplayName(cType, "Short") :
                cType.Name == "Int32" ? GetNullableDisplayName(cType, "Integer") :
                cType.Name == "Int64" ? GetNullableDisplayName(cType, "Long") :
                GetNullableDisplayName(cType, cType.Name);
        }

        private static string GetNullableDisplayName(CachedType type, string actual)
        {
            return (type.IsNullableType ? "Nullable" : "") + actual;
        }
    }
}