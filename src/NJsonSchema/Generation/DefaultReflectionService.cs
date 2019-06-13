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
        /// <param name="contextualType">The type.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonTypeDescription"/>. </returns>
        public JsonTypeDescription GetDescription(ContextualType contextualType, JsonSchemaGeneratorSettings settings)
        {
            return GetDescription(contextualType, settings.DefaultReferenceTypeNullHandling, settings);
        }

        /// <summary>Creates a <see cref="JsonTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="contextualType">The type.</param>
        /// <param name="defaultReferenceTypeNullHandling">The default reference type null handling used when no nullability information is available.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonTypeDescription"/>. </returns>
        public virtual JsonTypeDescription GetDescription(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling, JsonSchemaGeneratorSettings settings)
        {
            var type = contextualType.OriginalType;
            var isNullable = IsNullable(contextualType, defaultReferenceTypeNullHandling);

            var jsonSchemaTypeAttribute = contextualType.GetAttribute<JsonSchemaTypeAttribute>();
            if (jsonSchemaTypeAttribute != null)
            {
                type = jsonSchemaTypeAttribute.Type;
                contextualType = type.ToContextualType();

                if (jsonSchemaTypeAttribute.IsNullableRaw.HasValue)
                {
                    isNullable = jsonSchemaTypeAttribute.IsNullableRaw.Value;
                }
            }

            var jsonSchemaAttribute = contextualType.GetAttribute<JsonSchemaAttribute>();
            if (jsonSchemaAttribute != null)
            {
                var classType = jsonSchemaAttribute.Type != JsonObjectType.None ? jsonSchemaAttribute.Type : JsonObjectType.Object;
                var format = !string.IsNullOrEmpty(jsonSchemaAttribute.Format) ? jsonSchemaAttribute.Format : null;
                return JsonTypeDescription.Create(contextualType, classType, isNullable, format);
            }

            if (type.GetTypeInfo().IsEnum)
            {
                var isStringEnum = IsStringEnum(contextualType, settings.ActualSerializerSettings);
                return JsonTypeDescription.CreateForEnumeration(contextualType,
                    isStringEnum ? JsonObjectType.String : JsonObjectType.Integer, false);
            }

            // Primitive types

            if (type == typeof(short) ||
                type == typeof(uint) ||
                type == typeof(ushort))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Integer, false, null);
            }

            if (type == typeof(int))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Integer, false, JsonFormatStrings.Integer);
            }

            if (type == typeof(long) ||
                type == typeof(ulong))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Integer, false, JsonFormatStrings.Long);
            }

            if (type == typeof(double) ||
                type == typeof(float))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Number, false, JsonFormatStrings.Double);
            }

            if (type == typeof(decimal))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Number, false, JsonFormatStrings.Decimal);
            }

            if (type == typeof(bool))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Boolean, false, null);
            }

            if (type == typeof(string) || type == typeof(Type))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, isNullable, null);
            }

            if (type == typeof(char))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, false, null);
            }

            if (type == typeof(Guid))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, false, JsonFormatStrings.Guid);
            }

            // Date & time types

            if (type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type.FullName == "NodaTime.OffsetDateTime" ||
                type.FullName == "NodaTime.LocalDateTime" ||
                type.FullName == "NodaTime.ZonedDateTime" ||
                type.FullName == "NodaTime.Instant")
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, false, JsonFormatStrings.DateTime);
            }

            if (type == typeof(TimeSpan) ||
                type.FullName == "NodaTime.Duration")
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, false, JsonFormatStrings.TimeSpan);
            }

            if (type.FullName == "NodaTime.LocalDate")
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, false, JsonFormatStrings.Date);
            }

            if (type.FullName == "NodaTime.LocalTime")
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, false, JsonFormatStrings.Time);
            }

            // Special types

            if (type == typeof(Uri))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, isNullable, JsonFormatStrings.Uri);
            }

            if (type == typeof(byte))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Integer, false, JsonFormatStrings.Byte);
            }

            if (type == typeof(byte[]))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, isNullable, JsonFormatStrings.Byte);
            }

            if (type.IsAssignableToTypeName(nameof(JArray), TypeNameStyle.Name))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Array, isNullable, null);
            }

            if (type.IsAssignableToTypeName(nameof(JToken), TypeNameStyle.Name) ||
                type.FullName == "System.Dynamic.ExpandoObject" ||
                type == typeof(object))
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.None, isNullable, null);
            }

            if (IsBinary(contextualType))
            {
                if (settings.SchemaType == SchemaType.Swagger2)
                {
                    return JsonTypeDescription.Create(contextualType, JsonObjectType.File, isNullable, null);
                }
                else
                {
                    return JsonTypeDescription.Create(contextualType, JsonObjectType.String, isNullable, JsonFormatStrings.Binary);
                }
            }

            if (contextualType.IsNullableType)
            {
                var typeDescription = GetDescription(contextualType.OriginalGenericArguments[0], defaultReferenceTypeNullHandling, settings);
                typeDescription.IsNullable = true;
                return typeDescription;
            }

            var contract = settings.ResolveContract(type);
            if (IsDictionaryType(contextualType) && contract is JsonDictionaryContract)
            {
                return JsonTypeDescription.CreateForDictionary(contextualType, JsonObjectType.Object, isNullable);
            }

            if (IsArrayType(contextualType) && contract is JsonArrayContract)
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.Array, isNullable, null);
            }

            if (contract is JsonStringContract)
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, isNullable, null);
            }

            return JsonTypeDescription.Create(contextualType, JsonObjectType.Object, isNullable, null);
        }

        /// <summary>Checks whether a type is nullable.</summary>
        /// <param name="contextualType">The type.</param>
        /// <param name="defaultReferenceTypeNullHandling">The default reference type null handling used when no nullability information is available.</param>
        /// <returns>true if the type can be null.</returns>
        public virtual bool IsNullable(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling)
        {
            var jsonPropertyAttribute = contextualType.GetContextAttribute<JsonPropertyAttribute>();
            if (jsonPropertyAttribute != null && jsonPropertyAttribute.Required == Required.DisallowNull)
            {
                return false;
            }

            if (contextualType.ContextAttributes.FirstAssignableToTypeNameOrDefault("NotNullAttribute", TypeNameStyle.Name) != null)
            {
                return false;
            }

            if (contextualType.ContextAttributes.FirstAssignableToTypeNameOrDefault("CanBeNullAttribute", TypeNameStyle.Name) != null)
            {
                return true;
            }

            if (contextualType.Nullability != Nullability.Unknown)
            {
                return contextualType.Nullability == Nullability.Nullable;
            }

            var isValueType = contextualType.Type != typeof(string) &&
                              contextualType.TypeInfo.IsValueType;

            return isValueType == false &&
                   defaultReferenceTypeNullHandling != ReferenceTypeNullHandling.NotNull;
        }

        /// <summary>Checks whether the give type is a string enum.</summary>
        /// <param name="contextualType">The type.</param>
        /// <param name="serializerSettings">The serializer settings.</param>
        /// <returns>The result.</returns>
        public bool IsStringEnum(ContextualType contextualType, JsonSerializerSettings serializerSettings)
        {
            if (!contextualType.TypeInfo.IsEnum)
            {
                return false;
            }

            var hasGlobalStringEnumConverter = serializerSettings.Converters.OfType<StringEnumConverter>().Any();
            return hasGlobalStringEnumConverter || HasStringEnumConverter(contextualType);
        }

        /// <summary>Checks whether the given type is a file/binary type.</summary>
        /// <param name="contextualType">The type.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsBinary(ContextualType contextualType)
        {
            // TODO: Move all file handling to NSwag. How?

            var parameterTypeName = contextualType.TypeName;
            return parameterTypeName == "IFormFile" ||
                   contextualType.IsAssignableToTypeName("HttpPostedFile", TypeNameStyle.Name) ||
                   contextualType.IsAssignableToTypeName("HttpPostedFileBase", TypeNameStyle.Name) ||
#if !LEGACY
                   contextualType.TypeInfo.ImplementedInterfaces.Any(i => i.Name == "IFormFile");
#else
                   contextualType.TypeInfo.GetInterfaces().Any(i => i.Name == "IFormFile");
#endif
        }

#if !LEGACY

        /// <summary>Checks whether the given type is an array type.</summary>
        /// <param name="contextualType">The type.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsArrayType(ContextualType contextualType)
        {
            if (IsDictionaryType(contextualType))
            {
                return false;
            }

            // TODO: Improve these checks
            if (contextualType.TypeName == "ObservableCollection`1")
            {
                return true;
            }

            return contextualType.Type.IsArray ||
                (contextualType.TypeInfo.ImplementedInterfaces.Contains(typeof(IEnumerable)) &&
                (contextualType.TypeInfo.BaseType == null ||
                    !contextualType.TypeInfo.BaseType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IEnumerable))));
        }

        /// <summary>Checks whether the given type is a dictionary type.</summary>
        /// <param name="contextualType">The type.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsDictionaryType(ContextualType contextualType)
        {
            if (contextualType.TypeName == "IDictionary`2" || contextualType.TypeName == "IReadOnlyDictionary`2")
            {
                return true;
            }

            return contextualType.TypeInfo.ImplementedInterfaces.Contains(typeof(IDictionary)) &&
                (contextualType.TypeInfo.BaseType == null ||
                    !contextualType.TypeInfo.BaseType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDictionary)));
        }

#else

        /// <summary>Checks whether the given type is an array type.</summary>
        /// <param name="type">The type.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsArrayType(ContextualType contextualType)
        {
            if (IsDictionaryType(contextualType))
            {
                return false;
            }

            // TODO: Improve these checks
            if (contextualType.TypeName == "ObservableCollection`1")
            {
                return true;
            }

            return contextualType.Type.IsArray || 
                (contextualType.Type.GetInterfaces().Contains(typeof(IEnumerable)) &&
                (contextualType.TypeInfo.BaseType == null ||
                    !contextualType.TypeInfo.BaseType.GetTypeInfo().GetInterfaces().Contains(typeof(IEnumerable))));
        }

        /// <summary>Checks whether the given type is a dictionary type.</summary>
        /// <param name="contextualType">The type.</param>
        /// <returns>true or false.</returns>
        protected virtual bool IsDictionaryType(ContextualType contextualType)
        {
            if (contextualType.TypeName == "IDictionary`2" || contextualType.TypeName == "IReadOnlyDictionary`2")
            {
                return true;
            }

            return contextualType.Type.GetInterfaces().Contains(typeof(IDictionary)) &&
                (contextualType.TypeInfo.BaseType == null ||
                    !contextualType.TypeInfo.BaseType.GetTypeInfo().GetInterfaces().Contains(typeof(IDictionary)));
        }

#endif

        private bool HasStringEnumConverter(ContextualType contextualType)
        {
            dynamic jsonConverterAttribute = contextualType.Attributes?.FirstOrDefault(a => a.GetType().Name == "JsonConverterAttribute");
            if (jsonConverterAttribute != null && ObjectExtensions.HasProperty(jsonConverterAttribute, "ConverterType"))
            {
                var converterType = (Type)jsonConverterAttribute.ConverterType;
                return converterType.IsAssignableToTypeName("StringEnumConverter", TypeNameStyle.Name);
            }

            return false;
        }
    }
}