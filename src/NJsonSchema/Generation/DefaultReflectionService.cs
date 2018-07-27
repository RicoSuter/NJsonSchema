//-----------------------------------------------------------------------
// <copyright file="DefaultReflectionService.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Annotations;
using NJsonSchema.Infrastructure;
using System.Reflection;
using Newtonsoft.Json.Converters;

namespace NJsonSchema.Generation
{
    /// <summary>The default reflection service implementation.</summary>
    public class DefaultReflectionService : IReflectionService
    {
        /// <summary>Creates a <see cref="JsonTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="type">The type. </param>
        /// <param name="parentAttributes">The parent's attributes (i.e. parameter or property attributes).</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonTypeDescription"/>. </returns>
        public virtual JsonTypeDescription GetDescription(Type type, IEnumerable<Attribute> parentAttributes, JsonSchemaGeneratorSettings settings)
        {
            var isNullable = IsNullable(type, parentAttributes, settings);

            var jsonSchemaTypeAttribute = type.GetTypeInfo().GetCustomAttribute<JsonSchemaTypeAttribute>() ??
                                          parentAttributes?.OfType<JsonSchemaTypeAttribute>().SingleOrDefault();

            if (jsonSchemaTypeAttribute != null)
            {
                type = jsonSchemaTypeAttribute.Type;

                if (jsonSchemaTypeAttribute.IsNullableRaw.HasValue)
                    isNullable = jsonSchemaTypeAttribute.IsNullableRaw.Value;
            }
            
            var jsonSchemaAttribute = type.GetTypeInfo().GetCustomAttribute<JsonSchemaAttribute>() ??
                                      parentAttributes?.OfType<JsonSchemaAttribute>().SingleOrDefault();

            if (jsonSchemaAttribute != null)
            {
                var classType = jsonSchemaAttribute.Type != JsonObjectType.None ? jsonSchemaAttribute.Type : JsonObjectType.Object;
                var format = !string.IsNullOrEmpty(jsonSchemaAttribute.Format) ? jsonSchemaAttribute.Format : null;
                return JsonTypeDescription.Create(type, classType, isNullable, format);
            }

            if (type.GetTypeInfo().IsEnum)
            {
                var isStringEnum = IsStringEnum(type, parentAttributes, settings);
                return JsonTypeDescription.CreateForEnumeration(type,
                    isStringEnum ? JsonObjectType.String : JsonObjectType.Integer, false);
            }

            if (type == typeof(short) ||
                type == typeof(uint) ||
                type == typeof(ushort))
                return JsonTypeDescription.Create(type, JsonObjectType.Integer, false, null);

            if (type == typeof(int))
                return JsonTypeDescription.Create(type, JsonObjectType.Integer, false, JsonFormatStrings.Integer);

            if (type == typeof(long) ||
                type == typeof(ulong))
                return JsonTypeDescription.Create(type, JsonObjectType.Integer, false, JsonFormatStrings.Long);

            if (type == typeof(double) ||
                type == typeof(float))
                return JsonTypeDescription.Create(type, JsonObjectType.Number, false, JsonFormatStrings.Double);

            if (type == typeof(decimal))
                return JsonTypeDescription.Create(type, JsonObjectType.Number, false, JsonFormatStrings.Decimal);

            if (type == typeof(bool))
                return JsonTypeDescription.Create(type, JsonObjectType.Boolean, false, null);

            if (type == typeof(string) || type == typeof(Type))
                return JsonTypeDescription.Create(type, JsonObjectType.String, isNullable, null);

            if (type == typeof(char))
                return JsonTypeDescription.Create(type, JsonObjectType.String, false, null);

            if (type == typeof(Guid))
                return JsonTypeDescription.Create(type, JsonObjectType.String, false, JsonFormatStrings.Guid);

            if (type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type.FullName == "NodaTime.OffsetDateTime" ||
                type.FullName == "NodaTime.LocalDateTime" ||
                type.FullName == "NodaTime.ZonedDateTime")
                return JsonTypeDescription.Create(type, JsonObjectType.String, false, JsonFormatStrings.DateTime);

            if (type == typeof(TimeSpan) ||
                type.FullName == "NodaTime.Duration")
                return JsonTypeDescription.Create(type, JsonObjectType.String, false, JsonFormatStrings.TimeSpan);

            if (type.FullName == "NodaTime.LocalDate")
                return JsonTypeDescription.Create(type, JsonObjectType.String, false, JsonFormatStrings.Date);

            if (type.FullName == "NodaTime.LocalTime")
                return JsonTypeDescription.Create(type, JsonObjectType.String, false, JsonFormatStrings.Time);

            if (type == typeof(Uri))
                return JsonTypeDescription.Create(type, JsonObjectType.String, isNullable, JsonFormatStrings.Uri);

            if (type == typeof(byte))
                return JsonTypeDescription.Create(type, JsonObjectType.Integer, false, JsonFormatStrings.Byte);

            if (type == typeof(byte[]))
                return JsonTypeDescription.Create(type, JsonObjectType.String, isNullable, JsonFormatStrings.Byte);

            if (type == typeof(JArray))
                return JsonTypeDescription.Create(type, JsonObjectType.Array, isNullable, null);

            if (type == typeof(JObject) ||
                type == typeof(JToken) ||
                type.FullName == "System.Dynamic.ExpandoObject" ||
                type == typeof(object))
                return JsonTypeDescription.Create(type, JsonObjectType.None, isNullable, null);

            if (IsFileType(type, parentAttributes))
                return JsonTypeDescription.Create(type, JsonObjectType.File, isNullable, null);

            var contract = settings.ResolveContract(type);
            if (IsDictionaryType(type, parentAttributes) && contract is JsonDictionaryContract)
                return JsonTypeDescription.CreateForDictionary(type, JsonObjectType.Object, isNullable);

            if (IsArrayType(type, parentAttributes) && contract is JsonArrayContract)
                return JsonTypeDescription.Create(type, JsonObjectType.Array, isNullable, null);

            if (type.Name == "Nullable`1")
            {
#if !LEGACY
                // Remove JsonSchemaTypeAttributes to avoid stack overflows
                var typeDescription = GetDescription(type.GenericTypeArguments[0], parentAttributes?.Where(a => !(a is JsonSchemaTypeAttribute)), settings);
#else
                var typeDescription = GetDescription(type.GetGenericArguments()[0], parentAttributes?.Where(a => !(a is JsonSchemaTypeAttribute)), settings);
#endif
                typeDescription.IsNullable = true;
                return typeDescription;
            }

            if (contract is JsonStringContract)
                return JsonTypeDescription.Create(type, JsonObjectType.String, isNullable, null);

            return JsonTypeDescription.Create(type, JsonObjectType.Object, isNullable, null);
        }

        /// <summary>Checks whether a type is nullable.</summary>
        /// <param name="type">The type.</param>
        /// <param name="parentAttributes">The parent attributes (e.g. property or parameter attributes).</param>
        /// <param name="settings">The settings</param>
        /// <returns>true if the type can be null.</returns>
        public virtual bool IsNullable(Type type, IEnumerable<Attribute> parentAttributes, JsonSchemaGeneratorSettings settings)
        {
            var jsonPropertyAttribute = parentAttributes?.OfType<JsonPropertyAttribute>().SingleOrDefault();
            if (jsonPropertyAttribute != null && jsonPropertyAttribute.Required == Required.DisallowNull)
                return false;

            if (parentAttributes.TryGetIfAssignableTo("NotNullAttribute", TypeNameStyle.Name) != null)
                return false;

            if (parentAttributes.TryGetIfAssignableTo("CanBeNullAttribute", TypeNameStyle.Name) != null)
                return true;

            if (type.Name == "Nullable`1")
                return true;

            var isValueType = type != typeof(string) && type.GetTypeInfo().IsValueType;
            return isValueType == false && settings.DefaultReferenceTypeNullHandling == ReferenceTypeNullHandling.Null;
        }

        /// <summary>Checks whether the given type is a file type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="parentAttributes">The parent attributes.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsFileType(Type type, IEnumerable<Attribute> parentAttributes)
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

        /// <summary>Checks whether the given type is an array type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="parentAttributes">The parent attributes.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsArrayType(Type type, IEnumerable<Attribute> parentAttributes)
        {
            if (IsDictionaryType(type, parentAttributes))
                return false;

            // TODO: Improve these checks
            if (type.Name == "ObservableCollection`1")
                return true;

            return type.IsArray || (type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IEnumerable)) &&
                (type.GetTypeInfo().BaseType == null ||
                !type.GetTypeInfo().BaseType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IEnumerable))));
        }

        /// <summary>Checks whether the given type is a dictionary type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="parentAttributes">The parent attributes.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsDictionaryType(Type type, IEnumerable<Attribute> parentAttributes)
        {
            if (type.Name == "IDictionary`2" || type.Name == "IReadOnlyDictionary`2")
                return true;

            return type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDictionary)) &&
                (type.GetTypeInfo().BaseType == null ||
                !type.GetTypeInfo().BaseType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDictionary)));
        }

#else

        /// <summary>Checks whether the given type is an array type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="parentAttributes">The parent attributes.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsArrayType(Type type, IEnumerable<Attribute> parentAttributes)
        {
            if (IsDictionaryType(type, parentAttributes))
                return false;

            // TODO: Improve these checks
            if (type.Name == "ObservableCollection`1")
                return true;

            return type.IsArray || (type.GetTypeInfo().GetInterfaces().Contains(typeof(IEnumerable)) &&
                                    (type.GetTypeInfo().BaseType == null ||
                                     !type.GetTypeInfo().BaseType.GetTypeInfo().GetInterfaces().Contains(typeof(IEnumerable))));
        }

        /// <summary>Checks whether the given type is a dictionary type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="parentAttributes">The parent attributes.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsDictionaryType(Type type, IEnumerable<Attribute> parentAttributes)
        {
            if (type.Name == "IDictionary`2" || type.Name == "IReadOnlyDictionary`2")
                return true;

            return type.GetTypeInfo().GetInterfaces().Contains(typeof(IDictionary)) &&
                   (type.GetTypeInfo().BaseType == null ||
                    !type.GetTypeInfo().BaseType.GetTypeInfo().GetInterfaces().Contains(typeof(IDictionary)));
        }

#endif

        private bool IsStringEnum(Type type, IEnumerable<Attribute> parentAttributes, JsonSchemaGeneratorSettings settings)
        {
            var hasGlobalStringEnumConverter = settings.ActualSerializerSettings.Converters.OfType<StringEnumConverter>().Any();
            var hasStringEnumConverterOnType = HasStringEnumConverter(type.GetTypeInfo().GetCustomAttributes());
            var hasStringEnumConverterOnProperty = parentAttributes != null && HasStringEnumConverter(parentAttributes);

            return hasGlobalStringEnumConverter || hasStringEnumConverterOnType || hasStringEnumConverterOnProperty;
        }

        private bool HasStringEnumConverter(IEnumerable<Attribute> attributes)
        {
            if (attributes == null)
                return false;

            dynamic jsonConverterAttribute = attributes?.FirstOrDefault(a => a.GetType().Name == "JsonConverterAttribute");
            if (jsonConverterAttribute != null)
            {
                var converterType = (Type)jsonConverterAttribute.ConverterType;
                return converterType.IsAssignableTo("StringEnumConverter", TypeNameStyle.Name);
            }
            return false;
        }
    }
}