//-----------------------------------------------------------------------
// <copyright file="JsonObjectTypeDescription.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NJsonSchema.Annotations;

namespace NJsonSchema
{
    /// <summary>Gets JSON information about a .NET type. </summary>
    public class JsonObjectTypeDescription
    {
        /// <summary>Creates a <see cref="JsonObjectTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="type">The type. </param>
        /// <param name="parentAttributes">The parent's attributes (i.e. parameter or property attributes).</param>
        /// <param name="defaultEnumHandling">The default enum handling.</param>
        /// <returns>The <see cref="JsonObjectTypeDescription"/>. </returns>
        public static JsonObjectTypeDescription FromType(Type type, IEnumerable<Attribute> parentAttributes, EnumHandling defaultEnumHandling)
        {
            if (type.GetTypeInfo().IsEnum)
            {
                var isStringEnum = 
                    HasStringEnumConverter(type.GetTypeInfo().GetCustomAttributes()) || 
                    (parentAttributes != null && HasStringEnumConverter(parentAttributes)) ||
                    defaultEnumHandling == EnumHandling.String;

                return new JsonObjectTypeDescription(isStringEnum ? JsonObjectType.String : JsonObjectType.Integer, true)
                {
                    IsEnum = true
                };
            }

            if (type == typeof(int) || type == typeof(short) || type == typeof(uint) || type == typeof(ushort))
                return new JsonObjectTypeDescription(JsonObjectType.Integer, true);

            if ((type == typeof(long)) ||(type == typeof(ulong)))
                return new JsonObjectTypeDescription(JsonObjectType.Integer, true, false, JsonFormatStrings.Long);

            if (type == typeof(double) || type == typeof(float))
                return new JsonObjectTypeDescription(JsonObjectType.Number, true, false, JsonFormatStrings.Double);

            if (type == typeof(decimal))
                return new JsonObjectTypeDescription(JsonObjectType.Number, true, false, JsonFormatStrings.Decimal);

            if (type == typeof(bool))
                return new JsonObjectTypeDescription(JsonObjectType.Boolean, true);

            if (type == typeof(string) || type == typeof(Type))
                return new JsonObjectTypeDescription(JsonObjectType.String, false);

            if (type == typeof(char))
                return new JsonObjectTypeDescription(JsonObjectType.String, true);

            if (type == typeof(Guid))
                return new JsonObjectTypeDescription(JsonObjectType.String, true, false, JsonFormatStrings.Guid);

            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                return new JsonObjectTypeDescription(JsonObjectType.String, true, false, JsonFormatStrings.DateTime);

            if (type == typeof(TimeSpan))
                return new JsonObjectTypeDescription(JsonObjectType.String, true, false, JsonFormatStrings.TimeSpan);

            if (type == typeof(Uri))
                return new JsonObjectTypeDescription(JsonObjectType.String, true, false, JsonFormatStrings.Uri);

            if (type == typeof(byte))
                return new JsonObjectTypeDescription(JsonObjectType.Integer, true, false, JsonFormatStrings.Byte);

            if (type == typeof(byte[]))
                return new JsonObjectTypeDescription(JsonObjectType.String, false, false, JsonFormatStrings.Byte);

            if (IsDictionaryType(type))
                return new JsonObjectTypeDescription(JsonObjectType.Object, false, true);

            if (IsArrayType(type))
                return new JsonObjectTypeDescription(JsonObjectType.Array, false);

            if (type.Name == "Nullable`1")
            {
                var typeDescription = FromType(type.GenericTypeArguments[0], parentAttributes, defaultEnumHandling);
                typeDescription.IsAlwaysRequired = false;
                return typeDescription;
            }

            var jsonSchemaAttribute = type.GetTypeInfo().GetCustomAttribute<JsonSchemaAttribute>();
            if (jsonSchemaAttribute != null)
            {
                var classType = jsonSchemaAttribute.Type != JsonObjectType.None ? jsonSchemaAttribute.Type : JsonObjectType.Object;
                var format = !string.IsNullOrEmpty(jsonSchemaAttribute.Format) ? jsonSchemaAttribute.Format : null; 
                return new JsonObjectTypeDescription(classType, false, false, format);
            }

            return new JsonObjectTypeDescription(JsonObjectType.Object, false);
        }

        private JsonObjectTypeDescription(JsonObjectType type, bool isAlwaysRequired, bool isDictionary = false, string format = null)
        {
            Type = type;
            IsAlwaysRequired = isAlwaysRequired;
            Format = format;
            IsDictionary = isDictionary;
        }

        private static bool HasStringEnumConverter(IEnumerable<Attribute> attributes)
        {
            if (attributes == null)
                return false;

            dynamic jsonConverterAttribute = attributes?.FirstOrDefault(a => a.GetType().Name == "JsonConverterAttribute");
            if (jsonConverterAttribute != null)
            {
                var converterType = (Type)jsonConverterAttribute.ConverterType;
                if (converterType.Name == "StringEnumConverter")
                    return true;
            }
            return false;
        }

        /// <summary>Gets the type. </summary>
        public JsonObjectType Type { get; private set; }

        /// <summary>Gets a value indicating whether the type must always required. </summary>
        public bool IsAlwaysRequired { get; private set; }

        /// <summary>Gets a value indicating whether the object is a generic dictionary.</summary>
        public bool IsDictionary { get; private set; }

        /// <summary>Gets a value indicating whether the type is an enum.</summary>
        public bool IsEnum { get; private set; }

        /// <summary>Gets the format string. </summary>
        public string Format { get; private set; }

        /// <summary>Gets a value indicating whether this is a complex type (i.e. object, dictionary or array).</summary>
        public bool IsComplexType => Type.HasFlag(JsonObjectType.Object) || Type.HasFlag(JsonObjectType.Array);

        private static bool IsArrayType(Type type)
        {
            if (IsDictionaryType(type))
                return false;

            return type.IsArray || (type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IEnumerable)) &&
                (type.GetTypeInfo().BaseType == null ||
                !type.GetTypeInfo().BaseType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IEnumerable))));
        }

        private static bool IsDictionaryType(Type type)
        {
            if (type.Name == "IDictionary`2" || type.Name == "IReadOnlyDictionary`2")
                return true; 

            return type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDictionary)) &&
                (type.GetTypeInfo().BaseType == null ||
                !type.GetTypeInfo().BaseType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDictionary)));
        }

        /// <summary>Applies the type and format to the given schema.</summary>
        /// <param name="schema">The JSON schema.</param>
        public void ApplyType(JsonSchema4 schema)
        {
            schema.Type = Type;
            schema.Format = Format;
        }
    }
}