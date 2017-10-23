//-----------------------------------------------------------------------
// <copyright file="LiquidHash.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotLiquid;

namespace NJsonSchema.CodeGeneration
{
    internal class LiquidHash
    {
        public static Hash FromObject(object obj)
        {
            return obj != null ? FromObject(obj, new Dictionary<object, Hash>()) : new Hash();
        }

        private static Hash FromObject(object obj, Dictionary<object, Hash> cache)
        {
            if (cache == null)
            {
                cache = new Dictionary<object, Hash>();
            }

            if (cache.ContainsKey(obj))
            {
                return cache[obj];
            }

            var hash = new Hash();
            foreach (var property in obj.GetType().GetRuntimeProperties().Where(p => p.CanRead && p.GetMethod.IsPublic))
            {
                var value = property.GetValue(obj, null);
                if (IsObject(value))
                {
                    if (value is IDictionary dictionary)
                    {
                        var list = new List<Hash>();
                        foreach (var key in dictionary.Keys)
                        {
                            var pair = new Hash();
                            pair["Key"] = key;
                            pair["Value"] = dictionary[key];
                            list.Add(pair);
                        }

                        hash[property.Name] = list;
                    }
                    else if (value is IEnumerable enumerable)
                    {
                        if (enumerable.OfType<object>().Any(i => !IsObject(i)))
                        {
                            hash[property.Name] = enumerable;
                        }
                        else
                        {
                            var list = new List<Hash>();
                            foreach (var item in enumerable)
                            {
                                list.Add(FromObject(item, cache));
                            }
                            hash[property.Name] = list;
                        }
                    }
                    else
                    {
                        hash[property.Name] = FromObject(value, cache);
                    }
                }
                else
                {
                    hash[property.Name] = value;
                }
            }

            cache[obj] = hash;
            return hash;
        }

        private static bool IsObject(object value)
        {
            return value != null && value.GetType().GetTypeInfo().IsClass && !(value is string);
        }
    }
}