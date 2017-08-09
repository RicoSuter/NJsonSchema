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
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    /// <summary>Provides utilities to resolve and set JSON schema references.</summary>
    public static class JsonSchemaReferenceUtilities
    {
        /// <summary>Updates all <see cref="JsonSchema4.SchemaReference"/> properties from the 
        /// available <see cref="JsonSchema4.SchemaReferencePath"/> properties.</summary>
        /// <param name="referenceResolver">The JSON document resolver.</param>
        /// <param name="rootObject">The root object.</param>
        public static async Task UpdateSchemaReferencesAsync(object rootObject, JsonReferenceResolver referenceResolver)
        {
            await UpdateSchemaReferencesAsync(rootObject, rootObject, new HashSet<object>(), referenceResolver).ConfigureAwait(false);
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

        /// <summary>Updates the <see cref="JsonSchema4.SchemaReferencePath" /> properties
        /// from the available <see cref="JsonSchema4.SchemaReference" /> properties.</summary>
        /// <param name="rootObject">The root object.</param>
        public static void UpdateSchemaReferencePaths(object rootObject)
        {
            var schemaReferences = new Dictionary<JsonSchema4, JsonSchema4>();
            UpdateSchemaReferencePaths(rootObject, new HashSet<object>(), schemaReferences);

            var searchedSchemas = schemaReferences.Select(p => p.Value).Distinct();
            var result = JsonPathUtilities.GetJsonPaths(rootObject, searchedSchemas);

            foreach (var p in schemaReferences)
                p.Key.SchemaReferencePath = result[p.Value];
        }

        private static void UpdateSchemaReferencePaths(object obj, HashSet<object> checkedObjects, Dictionary<JsonSchema4, JsonSchema4> schemaReferences)
        {
            if (obj == null || obj is string)
                return;

            var schema = obj as JsonSchema4;
            if (schema != null && schema.SchemaReference != null)
            {
                if (schema.SchemaReference.DocumentPath == null)
                    schemaReferences[schema] = schema.SchemaReference.ActualSchema;
                else
                {
                    // TODO: Improve performance here (like the rest)
                    var externalReference = schema.SchemaReference;
                    var externalReferenceRoot = externalReference.FindRootParent();
                    schema.SchemaReferencePath = externalReference.DocumentPath + JsonPathUtilities.GetJsonPath(externalReferenceRoot, externalReference);
                }
            }

            if (obj is IDictionary)
            {
                foreach (var item in ((IDictionary)obj).Values.OfType<object>().ToList())
                    UpdateSchemaReferencePaths(item, checkedObjects, schemaReferences);
            }
            else if (obj is IEnumerable)
            {
                foreach (var item in ((IEnumerable)obj).OfType<object>().ToArray())
                    UpdateSchemaReferencePaths(item, checkedObjects, schemaReferences);
            }

            if (!(obj is JToken))
            {
                foreach (var member in ReflectionCache.GetPropertiesAndFields(obj.GetType()).Where(p =>
                    p.CanRead && p.IsIndexer == false && p.MemberInfo is PropertyInfo &&
                    p.CustomAttributes.JsonIgnoreAttribute == null))
                {
                    var value = member.GetValue(obj);
                    if (value != null)
                    {
                        if (!checkedObjects.Contains(value))
                        {
                            checkedObjects.Add(value);
                            UpdateSchemaReferencePaths(value, checkedObjects, schemaReferences);
                        }
                    }
                }
            }
        }

        private static async Task UpdateSchemaReferencesAsync(object rootObject, object obj, HashSet<object> checkedObjects, JsonReferenceResolver jsonReferenceResolver)
        {
            if (obj == null || obj is string)
                return;

            var schema = obj as JsonSchema4;
            if (schema != null && schema.SchemaReferencePath != null)
                schema.SchemaReference = await jsonReferenceResolver.ResolveReferenceAsync(rootObject, schema.SchemaReferencePath).ConfigureAwait(false);

            if (obj is IDictionary)
            {
                foreach (var item in ((IDictionary)obj).Values.OfType<object>().ToArray())
                    await UpdateSchemaReferencesAsync(rootObject, item, checkedObjects, jsonReferenceResolver).ConfigureAwait(false);
            }
            else if (obj is IEnumerable)
            {
                foreach (var item in ((IEnumerable)obj).OfType<object>().ToArray())
                    await UpdateSchemaReferencesAsync(rootObject, item, checkedObjects, jsonReferenceResolver).ConfigureAwait(false);
            }

            if (!(obj is JToken))
            {
                foreach (var property in ReflectionCache.GetPropertiesAndFields(obj.GetType()).Where(p =>
                    p.CanRead && p.IsIndexer == false && p.MemberInfo is PropertyInfo &&
                    p.CustomAttributes.JsonIgnoreAttribute == null))
                {
                    var value = property.GetValue(obj);
                    if (value != null)
                    {
                        if (!checkedObjects.Contains(value))
                        {
                            checkedObjects.Add(value);
                            await UpdateSchemaReferencesAsync(rootObject, value, checkedObjects, jsonReferenceResolver).ConfigureAwait(false);
                        }
                    }
                }
            }
        }
    }
}