//-----------------------------------------------------------------------
// <copyright file="JsonReferenceVisitorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Infrastructure;
using NJsonSchema.References;

namespace NJsonSchema.Visitors
{
    /// <summary>Visitor to transform an object with <see cref="JsonSchema4"/> objects.</summary>
    public abstract class JsonReferenceVisitorBase
    {
        private readonly IContractResolver _contractResolver;
        private readonly string[] _jsonSchemaProperties = typeof(JsonSchema4).GetRuntimeProperties().Select(p => p.Name).ToArray();

        /// <summary>Initializes a new instance of the <see cref="JsonReferenceVisitorBase"/> class. </summary>
        protected JsonReferenceVisitorBase()
            : this(new DefaultContractResolver())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JsonReferenceVisitorBase"/> class. </summary>
        /// <param name="contractResolver">The contract resolver.</param>
        protected JsonReferenceVisitorBase(IContractResolver contractResolver)
        {
            _contractResolver = contractResolver;
        }

        /// <summary>Processes an object.</summary>
        /// <param name="obj">The object to process.</param>
        /// <returns>The task.</returns>
        public virtual async Task VisitAsync(object obj)
        {
            await VisitAsync(obj, "#", null, new HashSet<object>(), o => throw new NotSupportedException("Cannot replace the root.")).ConfigureAwait(false);
        }

        /// <summary>Called when a <see cref="IJsonReference"/> is visited.</summary>
        /// <param name="reference">The visited schema.</param>
        /// <param name="path">The path.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The task.</returns>
        protected abstract Task<IJsonReference> VisitJsonReferenceAsync(IJsonReference reference, string path, string typeNameHint);

        /// <summary>Processes an object.</summary>
        /// <param name="obj">The object to process.</param>
        /// <param name="path">The path</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <param name="checkedObjects">The checked objects.</param>
        /// <param name="replacer">The replacer.</param>
        /// <returns>The task.</returns>
        protected virtual async Task VisitAsync(object obj, string path, string typeNameHint, ISet<object> checkedObjects, Action<object> replacer)
        {
            if (obj == null || checkedObjects.Contains(obj))
                return;
            checkedObjects.Add(obj);

            if (obj is IJsonReference reference)
            {
                var newReference = await VisitJsonReferenceAsync(reference, path, typeNameHint).ConfigureAwait(false);
                if (newReference != reference)
                {
                    replacer(newReference);
                    return;
                }
            }

            if (obj is JsonSchema4 schema)
            {
                if (schema.Reference != null)
                    await VisitAsync(schema.Reference, path, null, checkedObjects, o => schema.Reference = (JsonSchema4)o).ConfigureAwait(false);

                if (schema.AdditionalItemsSchema != null)
                    await VisitAsync(schema.AdditionalItemsSchema, path + "/additionalItems", null, checkedObjects, o => schema.AdditionalItemsSchema = (JsonSchema4)o).ConfigureAwait(false);

                if (schema.AdditionalPropertiesSchema != null)
                    await VisitAsync(schema.AdditionalPropertiesSchema, path + "/additionalProperties", null, checkedObjects, o => schema.AdditionalPropertiesSchema = (JsonSchema4)o).ConfigureAwait(false);

                if (schema.Item != null)
                    await VisitAsync(schema.Item, path + "/items", null, checkedObjects, o => schema.Item = (JsonSchema4)o).ConfigureAwait(false);

                for (var i = 0; i < schema.Items.Count; i++)
                {
                    var index = i;
                    await VisitAsync(schema.Items.ElementAt(i), path + "/items[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(schema.Items, index, (JsonSchema4)o)).ConfigureAwait(false);
                }

                for (var i = 0; i < schema.AllOf.Count; i++)
                {
                    var index = i;
                    await VisitAsync(schema.AllOf.ElementAt(i), path + "/allOf[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(schema.AllOf, index, (JsonSchema4)o)).ConfigureAwait(false);
                }

                for (var i = 0; i < schema.AnyOf.Count; i++)
                {
                    var index = i;
                    await VisitAsync(schema.AnyOf.ElementAt(i), path + "/anyOf[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(schema.AnyOf, index, (JsonSchema4)o)).ConfigureAwait(false);
                }

                for (var i = 0; i < schema.OneOf.Count; i++)
                {
                    var index = i;
                    await VisitAsync(schema.OneOf.ElementAt(i), path + "/oneOf[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(schema.OneOf, index, (JsonSchema4)o)).ConfigureAwait(false);
                }

                if (schema.Not != null)
                    await VisitAsync(schema.Not, path + "/not", null, checkedObjects, o => schema.Not = (JsonSchema4)o).ConfigureAwait(false);

                foreach (var p in schema.Properties.ToArray())
                    await VisitAsync(p.Value, path + "/properties/" + p.Key, p.Key, checkedObjects, o => schema.Properties[p.Key] = (JsonProperty)o).ConfigureAwait(false);

                foreach (var p in schema.PatternProperties.ToArray())
                    await VisitAsync(p.Value, path + "/patternProperties/" + p.Key, null, checkedObjects, o => schema.PatternProperties[p.Key] = (JsonProperty)o).ConfigureAwait(false);

                foreach (var p in schema.Definitions.ToArray())
                {
                    await VisitAsync(p.Value, path + "/definitions/" + p.Key, p.Key, checkedObjects, o =>
                    {
                        if (o != null)
                            schema.Definitions[p.Key] = (JsonSchema4)o;
                        else
                            schema.Definitions.Remove(p.Key);
                    }).ConfigureAwait(false);
                }
            }

            if (!(obj is string) && !(obj is JToken))
            {
                // Reflection fallback
                if (obj is IDictionary dictionary)
                {
                    await VisitPropertiesAsync(obj, path, checkedObjects).ConfigureAwait(false);
                    foreach (var key in dictionary.Keys.OfType<object>().ToArray())
                    {
                        await VisitAsync(dictionary[key], path + "/" + key, key.ToString(), checkedObjects, o =>
                        {
                            if (o != null)
                                dictionary[key] = (JsonSchema4)o;
                            else
                                dictionary.Remove(key);
                        }).ConfigureAwait(false);
                    }
                }
                else if (obj is IList list)
                {
                    var items = list.OfType<object>().ToArray();
                    for (var i = 0; i < items.Length; i++)
                    {
                        var index = i;
                        await VisitAsync(items[i], path + "[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(list, index, o)).ConfigureAwait(false);
                    }
                }
                else if (obj is IEnumerable enumerable)
                {
                    var items = enumerable.OfType<object>().ToArray();
                    for (var i = 0; i < items.Length; i++)
                        await VisitAsync(items[i], path + "[" + i + "]", null, checkedObjects, o => throw new NotSupportedException("Cannot replace enumerable item.")).ConfigureAwait(false);
                }
                else
                {
                    await VisitPropertiesAsync(obj, path, checkedObjects).ConfigureAwait(false);
                }
            }
        }

        private async Task VisitPropertiesAsync(object obj, string path, ISet<object> checkedObjects)
        {
            if (_contractResolver.ResolveContract(obj.GetType()) is JsonObjectContract contract)
            {
                foreach (var property in contract.Properties.Where(p => !p.Ignored && p.ShouldSerialize?.Invoke(obj) != false))
                {
                    var value = property.ValueProvider.GetValue(obj);
                    if (value != null)
                        await VisitAsync(value, path + "/" + property.PropertyName, property.PropertyName, checkedObjects, o => property.ValueProvider.SetValue(obj, o)).ConfigureAwait(false);
                }
            }
            else
            {
                foreach (var member in ReflectionCache.GetPropertiesAndFields(obj.GetType()).Where(p =>
                    p.MemberInfo is PropertyInfo &&
                    (!(obj is JsonSchema4) || !_jsonSchemaProperties.Contains(p.MemberInfo.Name)) &&
                    (!(obj is IDictionary) || (p.MemberInfo.DeclaringType == obj.GetType())) && // only check additional properties of dictionary
                    p.CanRead &&
                    p.IsIndexer == false &&
                    p.CustomAttributes.JsonIgnoreAttribute == null))
                {
                    var value = member.GetValue(obj);
                    if (value != null)
                        await VisitAsync(value, path + "/" + member.GetName(), member.GetName(), checkedObjects, o => member.SetValue(obj, o)).ConfigureAwait(false);
                }
            }
        }

        private void ReplaceOrDelete<T>(ICollection<T> collection, int index, T obj)
        {
            ((Collection<T>)collection).RemoveAt(index);
            if (obj != null)
                ((Collection<T>)collection).Insert(index, obj);
        }

        private void ReplaceOrDelete(IList collection, int index, object obj)
        {
            collection.RemoveAt(index);
            if (obj != null)
                collection.Insert(index, obj);
        }
    }
}
