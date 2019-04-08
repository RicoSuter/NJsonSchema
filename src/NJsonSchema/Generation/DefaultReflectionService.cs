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
using System.Reflection;
using Newtonsoft.Json.Converters;
using Namotion.Reflection;

namespace NJsonSchema.Generation
{
    /// <summary>The default reflection service implementation.</summary>
    public class DefaultReflectionService : IReflectionService
    {
        /// <summary>Creates a <see cref="JsonTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="typeWithContext">The type. </param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonTypeDescription"/>. </returns>
        public virtual JsonTypeDescription GetDescription(TypeWithContext typeWithContext, JsonSchemaGeneratorSettings settings)
        {
            var type = typeWithContext.OriginalType;
            var isNullable = IsNullable(typeWithContext, settings);

            var jsonSchemaTypeAttribute = typeWithContext.GetTypeAttribute<JsonSchemaTypeAttribute>() ??
                                          typeWithContext.GetContextAttribute<JsonSchemaTypeAttribute>();

            if (jsonSchemaTypeAttribute != null)
            {
                type = jsonSchemaTypeAttribute.Type;

                if (jsonSchemaTypeAttribute.IsNullableRaw.HasValue)
                    isNullable = jsonSchemaTypeAttribute.IsNullableRaw.Value;
            }

            var jsonSchemaAttribute = typeWithContext.GetTypeAttribute<JsonSchemaAttribute>() ??
                                      typeWithContext.GetContextAttribute<JsonSchemaAttribute>();

            if (jsonSchemaAttribute != null)
            {
                var classType = jsonSchemaAttribute.Type != JsonObjectType.None ? jsonSchemaAttribute.Type : JsonObjectType.Object;
                var format = !string.IsNullOrEmpty(jsonSchemaAttribute.Format) ? jsonSchemaAttribute.Format : null;
                return JsonTypeDescription.Create(type, classType, isNullable, format);
            }

            if (type.GetTypeInfo().IsEnum)
            {
                var isStringEnum = IsStringEnum(typeWithContext, settings);
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
                type.FullName == "NodaTime.ZonedDateTime" ||
                type.FullName == "NodaTime.Instant")
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

            if (type.IsAssignableTo(nameof(JArray), TypeNameStyle.Name))
                return JsonTypeDescription.Create(type, JsonObjectType.Array, isNullable, null);

            if (type.IsAssignableTo(nameof(JToken), TypeNameStyle.Name) ||
                type.FullName == "System.Dynamic.ExpandoObject" ||
                type == typeof(object))
            {
                return JsonTypeDescription.Create(type, JsonObjectType.None, isNullable, null);
            }

            if (IsBinary(typeWithContext))
            {
                if (settings.SchemaType == SchemaType.Swagger2)
                {
                    return JsonTypeDescription.Create(type, JsonObjectType.File, isNullable, null);
                }
                else
                {
                    return JsonTypeDescription.Create(type, JsonObjectType.String, isNullable, JsonFormatStrings.Binary);
                }
            }

            var contract = settings.ResolveContract(type);
            if (IsDictionaryType(typeWithContext) && contract is JsonDictionaryContract)
                return JsonTypeDescription.CreateForDictionary(type, JsonObjectType.Object, isNullable);

            if (IsArrayType(typeWithContext) && contract is JsonArrayContract)
                return JsonTypeDescription.Create(type, JsonObjectType.Array, isNullable, null);

            if (type.Name == "Nullable`1")
            {
                var typeDescription = GetDescription(typeWithContext.OriginalGenericArguments[0], settings);
                typeDescription.IsNullable = true;
                return typeDescription;
            }

            if (contract is JsonStringContract)
                return JsonTypeDescription.Create(type, JsonObjectType.String, isNullable, null);

            return JsonTypeDescription.Create(type, JsonObjectType.Object, isNullable, null);
        }

        /// <summary>Checks whether a type is nullable.</summary>
        /// <param name="typeWithContext">The type.</param>
        /// <param name="settings">The settings</param>
        /// <returns>true if the type can be null.</returns>
        public virtual bool IsNullable(TypeWithContext typeWithContext, JsonSchemaGeneratorSettings settings)
        {
            var jsonPropertyAttribute = typeWithContext.GetContextAttribute<JsonPropertyAttribute>();
            if (jsonPropertyAttribute != null && jsonPropertyAttribute.Required == Required.DisallowNull)
                return false;

            if (typeWithContext.ContextAttributes.TryGetIfAssignableTo("NotNullAttribute", TypeNameStyle.Name) != null)
                return false;

            if (typeWithContext.ContextAttributes.TryGetIfAssignableTo("CanBeNullAttribute", TypeNameStyle.Name) != null)
                return true;

            if (typeWithContext.OriginalType.Name == "Nullable`1")
                return true;

            var isValueType = typeWithContext.Type != typeof(string) && typeWithContext.Type.GetTypeInfo().IsValueType;
            return isValueType == false && settings.DefaultReferenceTypeNullHandling == ReferenceTypeNullHandling.Null;
        }

        /// <summary>Checks whether the given type is a file/binary type.</summary>
        /// <param name="typeWithContext">The type.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsBinary(TypeWithContext typeWithContext)
        {
            // TODO: Move all file handling to NSwag. How?

            var parameterTypeName = typeWithContext.Type.Name;
            return parameterTypeName == "IFormFile" ||
                   typeWithContext.Type.IsAssignableTo("HttpPostedFile", TypeNameStyle.Name) ||
                   typeWithContext.Type.IsAssignableTo("HttpPostedFileBase", TypeNameStyle.Name) ||
#if !LEGACY
                   typeWithContext.Type.GetTypeInfo().ImplementedInterfaces.Any(i => i.Name == "IFormFile");
#else
                   typeWithContext.Type.GetTypeInfo().GetInterfaces().Any(i => i.Name == "IFormFile");
#endif
        }

#if !LEGACY

        /// <summary>Checks whether the given type is an array type.</summary>
        /// <param name="typeWithContext">The type.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsArrayType(TypeWithContext typeWithContext)
        {
            if (IsDictionaryType(typeWithContext))
                return false;

            // TODO: Improve these checks
            if (typeWithContext.Type.Name == "ObservableCollection`1")
                return true;

            return typeWithContext.Type.IsArray || (typeWithContext.Type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IEnumerable)) &&
                (typeWithContext.Type.GetTypeInfo().BaseType == null ||
                !typeWithContext.Type.GetTypeInfo().BaseType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IEnumerable))));
        }

        /// <summary>Checks whether the given type is a dictionary type.</summary>
        /// <param name="typeWithContext">The type.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsDictionaryType(TypeWithContext typeWithContext)
        {
            if (typeWithContext.Type.Name == "IDictionary`2" || typeWithContext.Type.Name == "IReadOnlyDictionary`2")
                return true;

            return typeWithContext.Type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDictionary)) &&
                (typeWithContext.Type.GetTypeInfo().BaseType == null ||
                !typeWithContext.Type.GetTypeInfo().BaseType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDictionary)));
        }

#else

        /// <summary>Checks whether the given type is an array type.</summary>
        /// <param name="typeWithContext">The type.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsArrayType(TypeWithContext typeWithContext)
        {
            if (IsDictionaryType(typeWithContext))
                return false;

            // TODO: Improve these checks
            if (typeWithContext.OriginalType.Name == "ObservableCollection`1")
                return true;

            return typeWithContext.OriginalType.IsArray || (typeWithContext.OriginalType.GetTypeInfo().GetInterfaces().Contains(typeof(IEnumerable)) &&
                                    (typeWithContext.OriginalType.GetTypeInfo().BaseType == null ||
                                     !typeWithContext.OriginalType.GetTypeInfo().BaseType.GetTypeInfo().GetInterfaces().Contains(typeof(IEnumerable))));
        }

        /// <summary>Checks whether the given type is a dictionary type.</summary>
        /// <param name="typeWithContext">The type.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsDictionaryType(TypeWithContext typeWithContext)
        {
            if (typeWithContext.OriginalType.Name == "IDictionary`2" || typeWithContext.OriginalType.Name == "IReadOnlyDictionary`2")
                return true;

            return typeWithContext.OriginalType.GetTypeInfo().GetInterfaces().Contains(typeof(IDictionary)) &&
                   (typeWithContext.OriginalType.GetTypeInfo().BaseType == null ||
                    !typeWithContext.OriginalType.GetTypeInfo().BaseType.GetTypeInfo().GetInterfaces().Contains(typeof(IDictionary)));
        }

#endif

        private bool IsStringEnum(TypeWithContext type, JsonSchemaGeneratorSettings settings)
        {
            var hasGlobalStringEnumConverter = settings.ActualSerializerSettings.Converters.OfType<StringEnumConverter>().Any();
            var hasStringEnumConverterOnType = HasStringEnumConverter(type.TypeAttributes);
            var hasStringEnumConverterOnProperty = HasStringEnumConverter(type.ContextAttributes);

            return hasGlobalStringEnumConverter || hasStringEnumConverterOnType || hasStringEnumConverterOnProperty;
        }

        private bool HasStringEnumConverter(IEnumerable<Attribute> attributes)
        {
            if (attributes == null)
                return false;

            dynamic jsonConverterAttribute = attributes?.FirstOrDefault(a => a.GetType().Name == "JsonConverterAttribute");
            if (ReflectionExtensions.HasProperty(jsonConverterAttribute, "ConverterType"))
            {
                var converterType = (Type)jsonConverterAttribute.ConverterType;
                return converterType.IsAssignableTo("StringEnumConverter", TypeNameStyle.Name);
            }

            return false;
        }
    }
}