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
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    /// <summary>Provides utilities to resolve and set JSON schema references.</summary>
    public static class JsonSchemaReferenceUtilities
    {
        /// <summary>Updates all <see cref="JsonSchema4.SchemaReference"/> properties from the 
        /// available <see cref="JsonSchema4.SchemaReferencePath"/> properties.</summary>
        /// <param name="root">The root.</param>
        public static void UpdateSchemaReferences(object root)
        {
            UpdateSchemaReferences(root, root, new HashSet<object>());
        }

        /// <summary>Updates the <see cref="JsonSchema4.SchemaReferencePath" /> properties
        /// from the available <see cref="JsonSchema4.SchemaReference" /> properties.</summary>
        /// <param name="root">The root.</param>
        /// <param name="schemaDefinitionAppender">The schema definition appender.</param>
        public static void UpdateSchemaReferencePaths(object root, ISchemaDefinitionAppender schemaDefinitionAppender)
        {
            UpdateSchemaReferencePaths(root, root, new HashSet<object>(), schemaDefinitionAppender);
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

        private static void UpdateSchemaReferencePaths(object root, object obj, HashSet<object> checkedObjects, ISchemaDefinitionAppender schemaDefinitionAppender)
        {
            if (obj == null || obj is string)
                return;

            var schema = obj as JsonSchema4;
            if (schema != null && schema.SchemaReference != null)
                schema.SchemaReferencePath = JsonPathUtilities.GetJsonPath(root, schema.SchemaReference.ActualSchema, schemaDefinitionAppender);

            if (obj is IDictionary)
            {
                foreach (var item in ((IDictionary)obj).Values.OfType<object>().ToList())
                    UpdateSchemaReferencePaths(root, item, checkedObjects, schemaDefinitionAppender);
            }
            else if (obj is IEnumerable)
            {
                foreach (var item in (IEnumerable)obj)
                    UpdateSchemaReferencePaths(root, item, checkedObjects, schemaDefinitionAppender);
            }
            else
            {
                foreach (var property in ReflectionCache.GetProperties(obj.GetType()).Where(p => p.CanRead && ReflectionCache.GetCustomAttributes(p).JsonIgnoreAttribute == null))
                {
                    var value = property.GetValue(obj);
                    if (value != null)
                    {
                        if (!checkedObjects.Contains(value))
                        {
                            checkedObjects.Add(value);
                            UpdateSchemaReferencePaths(root, value, checkedObjects, schemaDefinitionAppender);
                        }
                    }
                }
            }
        }

        private static void UpdateSchemaReferences(object root, object obj, HashSet<object> checkedObjects)
        {
            if (obj == null || obj is string)
                return;

            var schema = obj as JsonSchema4;
            if (schema != null && schema.SchemaReferencePath != null)
                schema.SchemaReference = JsonPathUtilities.GetObjectFromJsonPath(root, schema.SchemaReferencePath);

            if (obj is IDictionary)
            {
                foreach (var item in ((IDictionary)obj).Values)
                    UpdateSchemaReferences(root, item, checkedObjects);
            }
            else if(obj is IEnumerable)
            {
                foreach (var item in (IEnumerable)obj)
                    UpdateSchemaReferences(root, item, checkedObjects);
            }
            else
            {
                foreach (var property in ReflectionCache.GetProperties(obj.GetType()).Where(p => p.CanRead && ReflectionCache.GetCustomAttributes(p).JsonIgnoreAttribute == null))
                {
                    var value = property.GetValue(obj);
                    if (value != null)
                    {
                        if (!checkedObjects.Contains(value))
                        {
                            checkedObjects.Add(value);
                            UpdateSchemaReferences(root, value, checkedObjects);
                        }
                    }
                }
            }
        }
    }
}