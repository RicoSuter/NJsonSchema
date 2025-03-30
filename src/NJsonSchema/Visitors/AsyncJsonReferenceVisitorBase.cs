﻿//-----------------------------------------------------------------------
// <copyright file="JsonReferenceVisitorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.ObjectModel;
using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema.References;

namespace NJsonSchema.Visitors
{
    /// <summary>Visitor to transform an object with <see cref="JsonSchema"/> objects.</summary>
    public abstract class AsyncJsonReferenceVisitorBase
    {
        private readonly IContractResolver _contractResolver;

        /// <summary>Initializes a new instance of the <see cref="AsyncJsonReferenceVisitorBase"/> class. </summary>
        protected AsyncJsonReferenceVisitorBase()
            : this(new DefaultContractResolver())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="AsyncJsonReferenceVisitorBase"/> class. </summary>
        /// <param name="contractResolver">The contract resolver.</param>
        protected AsyncJsonReferenceVisitorBase(IContractResolver contractResolver)
        {
            _contractResolver = contractResolver;
        }

        /// <summary>Processes an object.</summary>
        /// <param name="obj">The object to process.</param>
        /// <param name="cancellationToken">Cancellation token instance</param>
        /// <returns>The task.</returns>
        public virtual async Task VisitAsync(object obj, CancellationToken cancellationToken)
        {
            await VisitAsync(obj, "#", null, new HashSet<object>(), o => throw new NotSupportedException("Cannot replace the root."), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Called when a <see cref="IJsonReference"/> is visited.</summary>
        /// <param name="reference">The visited schema.</param>
        /// <param name="path">The path.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The task.</returns>
        protected abstract Task<IJsonReference> VisitJsonReferenceAsync(IJsonReference reference, string path, string? typeNameHint, CancellationToken cancellationToken);

        /// <summary>Processes an object.</summary>
        /// <param name="obj">The object to process.</param>
        /// <param name="path">The path</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <param name="checkedObjects">The checked objects.</param>
        /// <param name="replacer">The replacer.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The task.</returns>
        protected virtual async Task VisitAsync(object obj, string path, string? typeNameHint, ISet<object> checkedObjects, Action<object> replacer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (obj == null || checkedObjects.Contains(obj))
            {
                return;
            }

            checkedObjects.Add(obj);

            if (obj is IJsonReference reference)
            {
                var newReference = await VisitJsonReferenceAsync(reference, path, typeNameHint, cancellationToken).ConfigureAwait(false);
                if (newReference != reference)
                {
                    replacer(newReference);
                    return;
                }
            }

            if (obj is JsonSchema schema)
            {
                // Do not follow as the root object might be different than _rootObject, fixes https://github.com/RicoSuter/NJsonSchema/issues/588
                // i.e. we should only visit the objects which might be references but not resolve
                // because usually the resolved object is touched in another path (not via reference)
                //if (schema.Reference != null)
                //{
                //    await VisitAsync(schema.Reference, path, null, checkedObjects, o => schema.Reference = (JsonSchema)o).ConfigureAwait(false);
                //}

                if (schema.AdditionalItemsSchema != null)
                {
                    await VisitAsync(schema.AdditionalItemsSchema, path + "/additionalItems", null, checkedObjects, o => schema.AdditionalItemsSchema = (JsonSchema)o, cancellationToken).ConfigureAwait(false);
                }

                if (schema.AdditionalPropertiesSchema != null)
                {
                    await VisitAsync(schema.AdditionalPropertiesSchema, path + "/additionalProperties", null, checkedObjects, o => schema.AdditionalPropertiesSchema = (JsonSchema)o, cancellationToken).ConfigureAwait(false);
                }

                if (schema.Item != null)
                {
                    await VisitAsync(schema.Item, path + "/items", null, checkedObjects, o => schema.Item = (JsonSchema)o, cancellationToken).ConfigureAwait(false);
                }

                var items = schema._items;
                for (var i = 0; i < items.Count; i++)
                {
                    var index = i;
                    await VisitAsync(items[i], path + "/items[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(items, index, (JsonSchema)o), cancellationToken).ConfigureAwait(false);
                }

                var allOf = schema._allOf;
                for (var i = 0; i < allOf.Count; i++)
                {
                    var index = i;
                    await VisitAsync(allOf[i], path + "/allOf[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(allOf, index, (JsonSchema)o), cancellationToken).ConfigureAwait(false);
                }

                var anyOf = schema._anyOf;
                for (var i = 0; i < anyOf.Count; i++)
                {
                    var index = i;
                    await VisitAsync(anyOf[i], path + "/anyOf[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(anyOf, index, (JsonSchema)o), cancellationToken).ConfigureAwait(false);
                }

                var oneOf = schema._oneOf;
                for (var i = 0; i < oneOf.Count; i++)
                {
                    var index = i;
                    await VisitAsync(oneOf[i], path + "/oneOf[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(oneOf, index, (JsonSchema)o), cancellationToken).ConfigureAwait(false);
                }

                if (schema.Not != null)
                {
                    await VisitAsync(schema.Not, path + "/not", null, checkedObjects, o => schema.Not = (JsonSchema)o, cancellationToken).ConfigureAwait(false);
                }

                if (schema.DictionaryKey != null)
                {
                    await VisitAsync(schema.DictionaryKey, path + "/x-dictionaryKey", null, checkedObjects, o => schema.DictionaryKey = (JsonSchema)o, cancellationToken).ConfigureAwait(false);
                }

                if (schema.DiscriminatorRaw != null)
                {
                    await VisitAsync(schema.DiscriminatorRaw, path + "/discriminator", null, checkedObjects, o => schema.DiscriminatorRaw = o, cancellationToken).ConfigureAwait(false);
                }

                foreach (var p in schema.Properties.ToArray())
                {
                    await VisitAsync(p.Value, path + "/properties/" + p.Key, p.Key, checkedObjects, o => schema.Properties[p.Key] = (JsonSchemaProperty)o, cancellationToken).ConfigureAwait(false);
                }

                foreach (var p in schema.PatternProperties.ToArray())
                {
                    await VisitAsync(p.Value, path + "/patternProperties/" + p.Key, null, checkedObjects, o => schema.PatternProperties[p.Key] = (JsonSchemaProperty)o, cancellationToken).ConfigureAwait(false);
                }

                foreach (var p in schema.Definitions.ToArray())
                {
                    await VisitAsync(p.Value, path + "/definitions/" + p.Key, p.Key, checkedObjects, o =>
                    {
                        if (o != null)
                        {
                            schema.Definitions[p.Key] = (JsonSchema)o;
                        }
                        else
                        {
                            schema.Definitions.Remove(p.Key);
                        }
                    }, cancellationToken).ConfigureAwait(false);
                }
            }

            if (obj is not string && obj is not JToken && obj.GetType() != typeof(JsonSchema)) // Reflection fallback
            {
                if (_contractResolver.ResolveContract(obj.GetType()) is JsonObjectContract contract)
                {
                    foreach (var property in contract.Properties.Where(p =>
                    {
                        bool isJsonSchemaProperty = obj is JsonSchema && p.UnderlyingName != null && JsonSchema.JsonSchemaPropertiesCache.Contains(p.UnderlyingName);
                        return !isJsonSchemaProperty && !p.Ignored &&
                                p.ShouldSerialize?.Invoke(obj) != false;
                    }))
                    {
                        var value = property.ValueProvider?.GetValue(obj);
                        if (value != null)
                        {
                            await VisitAsync(value, path + "/" + property.PropertyName, property.PropertyName, checkedObjects, o => property.ValueProvider?.SetValue(obj, o), cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                else if (obj is IDictionary dictionary)
                {
                    foreach (var key in dictionary.Keys.OfType<object>().ToArray())
                    {
                        var value = dictionary[key];
                        if (value != null)
                        {
                            await VisitAsync(value, path + "/" + key, key.ToString(), checkedObjects, o =>
                            {
                                if (o != null)
                                {
                                    dictionary[key] = (JsonSchema)o;
                                }
                                else
                                {
                                    dictionary.Remove(key);
                                }
                            }, cancellationToken).ConfigureAwait(false);
                        }
                    }

                    // Custom dictionary type with additional properties (OpenApiPathItem)
                    var contextualType = obj.GetType().ToContextualType();
                    if (contextualType.IsAttributeDefined<JsonConverterAttribute>(true))
                    {
                        foreach (var property in contextualType.Type.GetContextualProperties()
                            .Where(p => p.MemberInfo.DeclaringType == contextualType.Type &&
                                        !p.IsAttributeDefined<JsonIgnoreAttribute>(true)))
                        {
                            var value = property.GetValue(obj);
                            if (value != null)
                            {
                                await VisitAsync(value, path + "/" + property.Name, property.Name, checkedObjects, o => property.SetValue(obj, o), cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                }
                else if (obj is IList list)
                {
                    var items = list.OfType<object>().ToArray();
                    for (var i = 0; i < items.Length; i++)
                    {
                        var index = i;
                        await VisitAsync(items[i], path + "[" + i + "]", null, checkedObjects, o => ReplaceOrDelete(list, index, o), cancellationToken).ConfigureAwait(false);
                    }
                }
                else if (obj is IEnumerable enumerable)
                {
                    var items = enumerable.OfType<object>().ToArray();
                    for (var i = 0; i < items.Length; i++)
                    {
                        await VisitAsync(items[i], path + "[" + i + "]", null, checkedObjects, o => throw new NotSupportedException("Cannot replace enumerable item."), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        private static void ReplaceOrDelete<T>(ObservableCollection<T> collection, int index, T obj)
        {
            collection.RemoveAt(index);
            if (obj != null)
            {
                collection.Insert(index, obj);
            }
        }

        private static void ReplaceOrDelete(IList collection, int index, object obj)
        {
            collection.RemoveAt(index);
            if (obj != null)
            {
                collection.Insert(index, obj);
            }
        }
    }
}
