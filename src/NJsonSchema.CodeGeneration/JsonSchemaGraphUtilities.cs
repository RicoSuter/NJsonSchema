//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGraphUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>JSON Schema graph utilities.</summary>
    public static class JsonSchemaGraphUtilities
    {
        /// <summary>Gets the derived schemas.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        /// <param name="typeResolver">The type resolver.</param>
        /// <returns></returns>
        public static IDictionary<string, JsonSchema4> GetDerivedSchemas(this JsonSchema4 schema, object rootObject, ITypeResolver typeResolver)
        {
            return FindAllSchemas(rootObject, typeResolver)
                .Where(p => p.Value.Inherits(schema))
                .ToDictionary(p => p.Key, p => p.Value);
        }

        /// <summary>Finds all schema object in the given object.</summary>
        /// <param name="root">The root object.</param>
        /// <param name="typeResolver">The type resolver.</param>
        /// <returns>The schemas.</returns>
        public static IDictionary<string, JsonSchema4> FindAllSchemas(object root, ITypeResolver typeResolver)
        {
            var schemas = new Dictionary<string, JsonSchema4>();
            FindAllSchemas(root, new HashSet<object>(), schemas, typeResolver, null);
            return schemas;
        }

        private static void FindAllSchemas(object obj, HashSet<object> checkedObjects, Dictionary<string, JsonSchema4> schemas, ITypeResolver typeResolver, string typeNameHint)
        {
            if (obj == null || obj is string || checkedObjects.Contains(obj))
                return;

            var schema = obj as JsonSchema4;
            if (schema != null)
            {
                schema = schema.ActualSchema;

                if (schema.Type.HasFlag(JsonObjectType.Object) && schemas.Values.All(s => s != schema))
                {
                    var typeName = typeResolver.GetOrGenerateTypeName(schema, typeNameHint);
                    schemas.Add(typeName, schema);
                }
            }

            checkedObjects.Add(obj);

            if (obj is IDictionary)
            {
                foreach (var key in ((IDictionary)obj).Keys)
                    FindAllSchemas(((IDictionary)obj)[key], checkedObjects, schemas, typeResolver, key as string);
            }
            else if (obj is IEnumerable)
            {
                foreach (var item in (IEnumerable)obj)
                    FindAllSchemas(item, checkedObjects, schemas, typeResolver, null);
            }
            else
            {
                foreach (var member in ReflectionCache.GetPropertiesAndFields(obj.GetType()).Where(p => p.CustomAttributes.JsonIgnoreAttribute == null))
                {
                    var value = member.GetValue(obj);
                    if (value != null)
                        FindAllSchemas(value, checkedObjects, schemas, typeResolver, member.MemberInfo.Name);
                }
            }
        }
    }
}