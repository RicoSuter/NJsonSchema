//-----------------------------------------------------------------------
// <copyright file="JsonSchemaVisitor.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NJsonSchema.Infrastructure;
using NJsonSchema.References;

namespace NJsonSchema
{
    /// <summary>Visitor to transform an object with <see cref="JsonSchema4"/> objects.</summary>
    public abstract class JsonSchemaVisitor
    {
        /// <summary>Processes an object.</summary>
        /// <param name="obj">The object to process.</param>
        /// <returns>The task.</returns>
        public async Task VisitAsync(object obj)
        {
            await VisitAsync(obj, "#", null, new HashSet<object>());
        }

        /// <summary>Processes an object.</summary>
        /// <param name="obj">The object to process.</param>
        /// <param name="path">The path</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <param name="checkedObjects">The checked objects.</param>
        /// <returns>The task.</returns>
        protected virtual async Task VisitAsync(object obj, string path, string typeNameHint, ISet<object> checkedObjects)
        {
            if (checkedObjects.Contains(obj))
                return;
            checkedObjects.Add(obj);

            if (obj is IJsonReference reference)
            {
                await VisitJsonReferenceAsync(reference, path);
            }
            if (obj is JsonSchema4 schema)
            {
                await VisitSchemaAsync(schema, path, typeNameHint);

                if (schema.AdditionalItemsSchema != null)
                    await VisitAsync(schema.AdditionalItemsSchema, path + "/additionalItems", null, checkedObjects);

                if (schema.AdditionalPropertiesSchema != null)
                    await VisitAsync(schema.AdditionalPropertiesSchema, path + "/additionalProperties", null, checkedObjects);

                if (schema.Item != null)
                    await VisitAsync(schema.Item, path + "/items", null, checkedObjects);

                for (var i = 0; i < schema.Items.Count; i++)
                    await VisitAsync(schema.Items.ElementAt(i), path + "/items[" + i + "]", null, checkedObjects);

                for (var i = 0; i < schema.AllOf.Count; i++)
                    await VisitAsync(schema.AllOf.ElementAt(i), path + "/allOf[" + i + "]", null, checkedObjects);

                for (var i = 0; i < schema.AnyOf.Count; i++)
                    await VisitAsync(schema.AnyOf.ElementAt(i), path + "/anyOf[" + i + "]", null, checkedObjects);

                for (var i = 0; i < schema.OneOf.Count; i++)
                    await VisitAsync(schema.OneOf.ElementAt(i), path + "/oneOf[" + i + "]", null, checkedObjects);

                if (schema.Not != null)
                    await VisitAsync(schema.Not, path + "/not", null, checkedObjects);

                foreach (var p in schema.Properties)
                    await VisitAsync(p.Value, path + "/properties/" + p.Key, p.Key, checkedObjects);

                foreach (var p in schema.PatternProperties)
                    await VisitAsync(p.Value, path + "/patternProperties/" + p.Key, null, checkedObjects);

                foreach (var p in schema.Definitions)
                    await VisitAsync(p.Value, path + "/definitions/" + p.Key, p.Key, checkedObjects);
            }
            else if (obj != null && !(obj is string) && !checkedObjects.Contains(obj))
            {
                // Reflection fallback

                if (obj is IDictionary dictionary)
                {
                    foreach (var key in dictionary.Keys)
                        await VisitAsync(dictionary[key], path + "/" + key, key.ToString(), checkedObjects);
                }
                else if (obj is IEnumerable enumerable)
                {
                    var i = 0;
                    foreach (var item in enumerable)
                    {
                        await VisitAsync(item, path + "[" + i + "]", null, checkedObjects);
                        i++;
                    }
                }
                else
                {
                    foreach (var member in ReflectionCache.GetPropertiesAndFields(obj.GetType()))
                    {
                        var value = member.GetValue(obj);
                        if (value != null)
                            await VisitAsync(value, path + "/" + member.GetName(), member.GetName(), checkedObjects);
                    }
                }
            }
        }

        /// <summary>Called when a <see cref="JsonSchema4"/> is visited.</summary>
        /// <param name="schema">The visited schema.</param>
        /// <param name="path">The path.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The task.</returns>
#pragma warning disable 1998
        protected virtual async Task VisitSchemaAsync(JsonSchema4 schema, string path, string typeNameHint)
#pragma warning restore 1998
        {
            // must be empty
        }

        /// <summary>Called when a <see cref="IJsonReference"/> is visited.</summary>
        /// <param name="reference">The visited schema.</param>
        /// <param name="path">The path.</param>
        /// <returns>The task.</returns>
#pragma warning disable 1998
        protected virtual async Task VisitJsonReferenceAsync(IJsonReference reference, string path)
#pragma warning restore 1998
        {
            // must be empty
        }
    }
}
