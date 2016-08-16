//-----------------------------------------------------------------------
// <copyright file="ReflectionExtensions.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;

namespace NJsonSchema.Infrastructure
{
    internal static class ReflectionExtensions
    {
        public static string GetTypeName(Type type)
        {
#if !LEGACY
            if (type.IsConstructedGenericType)
                return type.Name.Split('`').First() + "Of" + GetTypeName(type.GenericTypeArguments[0]);
#else
            if (type.IsGenericType)
                return type.Name.Split('`').First() + "Of" + GetTypeName(type.GetGenericArguments()[0]);
#endif

            return type.Name;
        }

        /// <summary>Gets the generic type arguments of a type.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The type arguments.</returns>
        public static Type[] GetGenericTypeArguments(Type type)
        {
#if !LEGACY

            var genericTypeArguments = type.GenericTypeArguments;
            while (type != null && type != typeof(object) && genericTypeArguments.Length == 0)
            {
                type = type.GetTypeInfo().BaseType;
                if (type != null)
                    genericTypeArguments = type.GenericTypeArguments;
            }
            return genericTypeArguments;

#else

            var genericTypeArguments = type.GetGenericArguments();
            while (type != null && type != typeof(object) && genericTypeArguments.Length == 0)
            {
                type = type.GetTypeInfo().BaseType;
                if (type != null)
                    genericTypeArguments = type.GetGenericArguments();
            }
            return genericTypeArguments;

#endif
        }

#if LEGACY

        public static MethodInfo GetRuntimeMethod(this Type type, string name, Type[] types)
        {
            return type.GetMethod(name, types);
        }

        public static PropertyInfo GetRuntimeProperty(this Type type, string name)
        {
            return type.GetProperty(name);
        }

        public static FieldInfo GetDeclaredField(this Type type, string name)
        {
            return type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }

        public static PropertyInfo[] GetRuntimeProperties(this Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }

        public static Attribute[] GetCustomAttributes(this FieldInfo fieldInfo, bool inherit = true)
        {
            return fieldInfo.GetCustomAttributes(inherit).OfType<Attribute>().ToArray();
        }

        public static Attribute[] GetCustomAttributes(this Type type, bool inherit = true)
        {
            return type.GetCustomAttributes(inherit).OfType<Attribute>().ToArray();
        }

        public static Attribute[] GetCustomAttributes(this PropertyInfo propertyInfo, bool inherit = true)
        {
            return propertyInfo.GetCustomAttributes(inherit).OfType<Attribute>().ToArray();
        }

        public static T[] GetCustomAttributes<T>(this Type type, bool inherit = true)
            where T : Attribute
        {
            return type.GetCustomAttributes(inherit).OfType<T>().ToArray();
        }

        public static T[] GetCustomAttributes<T>(this PropertyInfo propertyInfo, bool inherit = true)
            where T : Attribute
        {
            return propertyInfo.GetCustomAttributes(inherit).OfType<T>().ToArray();
        }

        public static T GetCustomAttribute<T>(this Type type)
            where T : Attribute
        {
            return type.GetCustomAttributes().OfType<T>().FirstOrDefault();
        }

        public static T GetCustomAttribute<T>(this PropertyInfo propertyInfo)
            where T : Attribute
        {
            return propertyInfo.GetCustomAttributes().OfType<T>().FirstOrDefault();
        }

        public static object GetValue(this PropertyInfo propertyInfo, object obj)
        {
            return propertyInfo.GetValue(obj, null);
        }

        public static void SetValue(this PropertyInfo propertyInfo, object obj, object value)
        {
            propertyInfo.SetValue(obj, value, null);
        }

#endif
    }
}