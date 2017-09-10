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
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Annotations;
using NJsonSchema.Generation.TypeMappers;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Generation
{
    /// <summary>Gets JSON information about a .NET type. </summary>
    public class JsonObjectTypeDescription
    {
        private readonly Type _type;

        /// <summary>Creates a <see cref="JsonObjectTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="type">The type. </param>
        /// <param name="parentAttributes">The parent's attributes (i.e. parameter or property attributes).</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonObjectTypeDescription"/>. </returns>
        public static JsonObjectTypeDescription FromType(
            Type type, IEnumerable<Attribute> parentAttributes, JsonSchemaGeneratorSettings settings)
        {
            var isStruct = type.Name != "Nullable`1" && type.GetTypeInfo().IsValueType && !type.GetTypeInfo().IsPrimitive;
            var allowsNull = isStruct == false;

            var jsonPropertyAttribute = parentAttributes?.OfType<JsonPropertyAttribute>().SingleOrDefault();
            if (jsonPropertyAttribute != null && jsonPropertyAttribute.Required == Required.DisallowNull)
                allowsNull = false;

            if (parentAttributes?.Any(a => a.GetType().Name == "NotNullAttribute") == true)
                allowsNull = false;

            if (type.GetTypeInfo().IsEnum)
            {
                var isStringEnum = IsStringEnum(type, parentAttributes, settings.DefaultEnumHandling);
                return new JsonObjectTypeDescription(type, isStringEnum ? JsonObjectType.String : JsonObjectType.Integer, false)
                {
                    IsEnum = true
                };
            }

            if (type == typeof(short) || type == typeof(uint) || type == typeof(ushort))
                return new JsonObjectTypeDescription(type, JsonObjectType.Integer, false);

            if (type == typeof(int))
                return new JsonObjectTypeDescription(type, JsonObjectType.Integer, false, false, JsonFormatStrings.Integer);

            if ((type == typeof(long)) || (type == typeof(ulong)))
                return new JsonObjectTypeDescription(type, JsonObjectType.Integer, false, false, JsonFormatStrings.Long);

            if (type == typeof(double) || type == typeof(float))
                return new JsonObjectTypeDescription(type, JsonObjectType.Number, false, false, JsonFormatStrings.Double);

            if (type == typeof(decimal))
                return new JsonObjectTypeDescription(type, JsonObjectType.Number, false, false, JsonFormatStrings.Decimal);

            if (type == typeof(bool))
                return new JsonObjectTypeDescription(type, JsonObjectType.Boolean, false);

            if (type == typeof(string) || type == typeof(Type))
                return new JsonObjectTypeDescription(type, JsonObjectType.String, allowsNull);

            if (type == typeof(char))
                return new JsonObjectTypeDescription(type, JsonObjectType.String, false);

            if (type == typeof(Guid))
                return new JsonObjectTypeDescription(type, JsonObjectType.String, false, false, JsonFormatStrings.Guid);

            if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type.FullName == "NodaTime.OffsetDateTime" || type.FullName == "NodaTime.ZonedDateTime")
                return new JsonObjectTypeDescription(type, JsonObjectType.String, false, false, JsonFormatStrings.DateTime);

            if (type == typeof(TimeSpan) || type.FullName == "NodaTime.Duration")
                return new JsonObjectTypeDescription(type, JsonObjectType.String, false, false, JsonFormatStrings.TimeSpan);

            if (type.FullName == "NodaTime.LocalDate")
                return new JsonObjectTypeDescription(type, JsonObjectType.String, false, false, JsonFormatStrings.Date);

            if (type == typeof(Uri))
                return new JsonObjectTypeDescription(type, JsonObjectType.String, allowsNull, false, JsonFormatStrings.Uri);

            if (type == typeof(byte))
                return new JsonObjectTypeDescription(type, JsonObjectType.Integer, false, false, JsonFormatStrings.Byte);

            if (type == typeof(byte[]))
                return new JsonObjectTypeDescription(type, JsonObjectType.String, allowsNull, false, JsonFormatStrings.Byte);

            if (type == typeof(JObject) || type == typeof(JToken) || type == typeof(object))
                return new JsonObjectTypeDescription(type, JsonObjectType.None, allowsNull);

            if (IsFileType(type, parentAttributes))
                return new JsonObjectTypeDescription(type, JsonObjectType.File, allowsNull);

            var contract = settings.ResolveContract(type);
            if (IsDictionaryType(type) && contract is JsonDictionaryContract)
                return new JsonObjectTypeDescription(type, JsonObjectType.Object, allowsNull, true);

            if (IsArrayType(type) && contract is JsonArrayContract)
                return new JsonObjectTypeDescription(type, JsonObjectType.Array, allowsNull);

            if (type.Name == "Nullable`1")
            {
#if !LEGACY
                var typeDescription = FromType(type.GenericTypeArguments[0], parentAttributes, settings);
#else
                var typeDescription = FromType(type.GetGenericArguments()[0], parentAttributes, settings);
#endif
                typeDescription.IsNullable = allowsNull;
                return typeDescription;
            }

            var jsonSchemaAttribute = type.GetTypeInfo().GetCustomAttribute<JsonSchemaAttribute>() ??
                parentAttributes?.OfType<JsonSchemaAttribute>().SingleOrDefault();
            if (jsonSchemaAttribute != null)
            {
                var classType = jsonSchemaAttribute.Type != JsonObjectType.None ? jsonSchemaAttribute.Type : JsonObjectType.Object;
                var format = !string.IsNullOrEmpty(jsonSchemaAttribute.Format) ? jsonSchemaAttribute.Format : null;
                return new JsonObjectTypeDescription(type, classType, allowsNull, false, format);
            }

            if (contract is JsonStringContract)
                return new JsonObjectTypeDescription(type, JsonObjectType.String, allowsNull);

            return new JsonObjectTypeDescription(type, JsonObjectType.Object, allowsNull);
        }

        /// <summary>Determines whether the an enum property serializes to a string.</summary>
        /// <param name="classType">Type of the class.</param>
        /// <param name="propertyAttributes">The property attributes.</param>
        /// <param name="defaultEnumHandling"></param>
        /// <returns>True or false</returns>
        public static bool IsStringEnum(Type classType, IEnumerable<Attribute> propertyAttributes, EnumHandling defaultEnumHandling)
        {
            if (defaultEnumHandling == EnumHandling.String)
                return true;

            return HasStringEnumConverter(classType.GetTypeInfo().GetCustomAttributes()) ||
                (propertyAttributes != null && HasStringEnumConverter(propertyAttributes));
        }

        private JsonObjectTypeDescription(Type type, JsonObjectType jsonType, bool isNullable, bool isDictionary = false, string format = null)
        {
            _type = type;
            Type = jsonType;
            IsNullable = isNullable;
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

        /// <summary>Gets a value indicating whether the object is a generic dictionary.</summary>
        public bool IsDictionary { get; private set; }

        /// <summary>Gets a value indicating whether the type is an enum.</summary>
        public bool IsEnum { get; private set; }

        /// <summary>Gets the format string. </summary>
        public string Format { get; private set; }

        /// <summary>Gets a value indicating whether this is a complex type (i.e. object, dictionary or array).</summary>
        public bool IsComplexType => IsDictionary || Type.HasFlag(JsonObjectType.Object) || Type.HasFlag(JsonObjectType.Array);

        /// <summary>Gets a value indicating whether the type is nullable.</summary>
        public bool IsNullable { get; private set; }

        /// <summary>Gets a value indicating whether this is an any type (e.g. object).</summary>
        public bool IsAny => Type == JsonObjectType.None;

        /// <summary>Specifices whether the type requires a reference.</summary>
        /// <param name="typeMappers">The type mappers.</param>
        /// <returns>true or false.</returns>
        public bool RequiresSchemaReference(IEnumerable<ITypeMapper> typeMappers)
        {
            var typeMapper = typeMappers.FirstOrDefault(m => m.MappedType == _type);
            if (typeMapper != null)
                return typeMapper.UseReference;

            return !IsDictionary && (Type.HasFlag(JsonObjectType.Object) || IsEnum);
        }

        private static bool IsFileType(Type type, IEnumerable<Attribute> parentAttributes)
        {
            // TODO: Move all file handling to NSwag. How?

            var parameterTypeName = type.Name;
            return parameterTypeName == "IFormFile" ||
                   type.IsAssignableTo("HttpPostedFile", TypeNameStyle.Name) ||
                   type.IsAssignableTo("HttpPostedFileBase", TypeNameStyle.Name) ||
#if !LEGACY
                   type.GetTypeInfo().ImplementedInterfaces.Any(i => i.Name == "IFormFile");
#else
                   type.GetTypeInfo().GetInterfaces().Any(i => i.Name == "IFormFile");
#endif
        }

#if !LEGACY

        private static bool IsArrayType(Type type)
        {
            if (IsDictionaryType(type))
                return false;

            // TODO: Improve these checks
            if (type.Name == "ObservableCollection`1")
                return true;

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

#else

        private static bool IsArrayType(Type type)
        {
            if (IsDictionaryType(type))
                return false;

            // TODO: Improve these checks
            if (type.Name == "ObservableCollection`1")
                return true;

            return type.IsArray || (type.GetTypeInfo().GetInterfaces().Contains(typeof(IEnumerable)) &&
                (type.GetTypeInfo().BaseType == null ||
                !type.GetTypeInfo().BaseType.GetTypeInfo().GetInterfaces().Contains(typeof(IEnumerable))));
        }

        private static bool IsDictionaryType(Type type)
        {
            if (type.Name == "IDictionary`2" || type.Name == "IReadOnlyDictionary`2")
                return true;

            return type.GetTypeInfo().GetInterfaces().Contains(typeof(IDictionary)) &&
                (type.GetTypeInfo().BaseType == null ||
                !type.GetTypeInfo().BaseType.GetTypeInfo().GetInterfaces().Contains(typeof(IDictionary)));
        }

#endif

        /// <summary>Applies the type and format to the given schema.</summary>
        /// <param name="schema">The JSON schema.</param>
        public void ApplyType(JsonSchema4 schema)
        {
            schema.Type = Type;
            schema.Format = Format;
        }
    }
}