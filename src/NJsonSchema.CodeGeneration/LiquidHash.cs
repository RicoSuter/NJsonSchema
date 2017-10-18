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
                cache = new Dictionary<object, Hash>();

            if (cache.ContainsKey(obj))
                return cache[obj];

            var hash = new Hash();
            foreach (var property in obj.GetType().GetRuntimeProperties().Where(p => p.CanRead))
            {
                var value = property.GetValue(obj, null);
                if (value is IEnumerable && !(value is string))
                {
                    var list = new List<Hash>();
                    foreach (var item in (IEnumerable)value)
                    {
                        list.Add(FromObject(item, cache));
                    }
                    hash[property.Name] = list;
                }
                else if (value != null && property.PropertyType.GetTypeInfo().IsClass && !(value is string))
                    hash[property.Name] = FromObject(value, cache);
                else
                    hash[property.Name] = value;
            }

            cache[obj] = hash;
            return hash;
        }
    }
}