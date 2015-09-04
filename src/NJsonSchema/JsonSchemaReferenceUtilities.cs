//-----------------------------------------------------------------------
// <copyright file="JsonSchemaReferenceUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace NJsonSchema
{
    /// <summary>Provides utilities to resolve and set JSON schema references.</summary>
    public static class JsonSchemaReferenceUtilities
    {
        /// <summary>Resolves the all TypeReferencePaths and TypeReferences in any <see cref="JsonSchema4"/> in any child of the root.</summary>
        /// <param name="root">The root.</param>
        public static void UpdateAllTypeReferences(object root)
        {
            UpdateTypeReferences(root, root, new List<object>());
            UpdateTypeReferencePaths(root, root, new List<object>());
        }

        /// <summary>Converts JSON references ($ref) to property references.</summary>
        /// <param name="data">The data.</param>
        /// <returns>The data.</returns>
        public static string ConvertJsonReferences(string data)
        {
            return data.Replace("$ref", "schemaReferencePath");
        }

        /// <summary>Converts property references to JSON references ($ref).</summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static string ConvertPropertyReferences(string data)
        {
            return data.Replace("schemaReferencePath", "$ref");
        }

        private static void UpdateTypeReferencePaths(object root, object obj, List<object> checkedObjects)
        {
            if (obj == null || obj is string)
                return;

            var schema = obj as JsonSchema4;
            if (schema != null)
            {
                if (schema.SchemaReference != null)
                    schema.SchemaReferencePath = JsonPathUtilities.GetJsonPath(root, schema.SchemaReference);
            }

            if (obj is IDictionary)
            {
                foreach (var item in ((IDictionary)obj).Values)
                    UpdateTypeReferencePaths(root, item, checkedObjects);
            }
            else if (obj is IEnumerable)
            {
                foreach (var item in (IEnumerable)obj)
                    UpdateTypeReferencePaths(root, item, checkedObjects);
            }
            else
            {
                foreach (var property in obj.GetType().GetRuntimeProperties().Where(p => p.CanRead && p.GetCustomAttribute<JsonIgnoreAttribute>() == null))
                {
                    var value = property.GetValue(obj);
                    if (value != null)
                    {
                        if (!checkedObjects.Contains(value))
                        {
                            checkedObjects.Add(value);

                            UpdateTypeReferencePaths(root, value, checkedObjects);
                        }
                    }
                }
            }
        }

        private static void UpdateTypeReferences(object root, object obj, List<object> checkedObjects)
        {
            if (obj == null || obj is string)
                return;

            var schema = obj as JsonSchema4;
            if (schema != null && schema.SchemaReferencePath != null)
                schema.SchemaReference = JsonPathUtilities.GetObjectFromJsonPath(root, schema.SchemaReferencePath);
            
            if (obj is IDictionary)
            {
                foreach (var item in ((IDictionary)obj).Values)
                    UpdateTypeReferences(root, item, checkedObjects);
            }
            else if(obj is IEnumerable)
            {
                foreach (var item in (IEnumerable)obj)
                    UpdateTypeReferences(root, item, checkedObjects);
            }
            else
            {
                foreach (var property in obj.GetType().GetRuntimeProperties().Where(p => p.CanRead && p.GetCustomAttribute<JsonIgnoreAttribute>() == null))
                {
                    var value = property.GetValue(obj);
                    if (value != null)
                    {
                        if (!checkedObjects.Contains(value))
                        {
                            checkedObjects.Add(value);
                            UpdateTypeReferences(root, value, checkedObjects);
                        }
                    }
                }
            }
        }
    }
}