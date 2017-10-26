//-----------------------------------------------------------------------
// <copyright file="LiquidProxyHash.cs" company="NJsonSchema">
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
    internal class LiquidProxyHash : Hash
    {
        private readonly IDictionary<string, PropertyInfo> _properties;

        public LiquidProxyHash(object obj)
        {
            Object = obj;
            _properties = obj?.GetType().GetRuntimeProperties()
                .ToDictionary(p => p.Name, p => p) ?? 
                    new Dictionary<string, PropertyInfo>();
        }

        public object Object { get; }

        public override bool Contains(object key)
        {
            return _properties.ContainsKey(key.ToString()) || base.Contains(key);
        }

        protected override object GetValue(string key)
        {
            if (_properties.ContainsKey(key))
            {
                var value = _properties[key].GetValue(Object);
                if (IsObject(value))
                {
                    if (value is IDictionary dictionary)
                    {
                        var list = new List<Hash>();
                        foreach (var k in dictionary.Keys)
                        {
                            var v = dictionary[k];
                            var pair = new Hash();
                            pair["Key"] = k;
                            pair["Value"] = IsObject(v) ? new LiquidProxyHash(v) : v;
                            list.Add(pair);
                        }
                        return list;
                    }
                    else if (value is IEnumerable enumerable)
                    {
                        if (enumerable.OfType<object>().Any(i => !IsObject(i)))
                        {
                            var list = new List<object>();
                            foreach (var item in enumerable)
                                list.Add(item);
                            return list;
                        }
                        else
                        {
                            var list = new List<LiquidProxyHash>();
                            foreach (var item in enumerable)
                                list.Add(new LiquidProxyHash(item));
                            return list;
                        }
                    }
                    else
                        return new LiquidProxyHash(value);
                }
                return value;
            }
            else
                return base.GetValue(key);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is LiquidProxyHash hash && hash.Object != Object)
                return false;

            return base.Equals(obj);
        }

        private static bool IsObject(object value)
        {
            return value != null && value.GetType().GetTypeInfo().IsClass && !(value is string);
        }
    }
}