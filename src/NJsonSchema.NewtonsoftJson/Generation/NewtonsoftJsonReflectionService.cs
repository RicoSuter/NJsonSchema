//-----------------------------------------------------------------------
// <copyright file="DefaultReflectionService.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;
using Newtonsoft.Json.Converters;
using Namotion.Reflection;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Infrastructure;
using System.Runtime.Serialization;
using System.Reflection;

namespace NJsonSchema.Generation
{
    public class NewtonsoftJsonReflectionService : ReflectionServiceBase<NewtonsoftJsonSchemaGeneratorSettings>
    {
        protected override JsonTypeDescription GetDescription(ContextualType contextualType, NewtonsoftJsonSchemaGeneratorSettings settings,
            Type originalType, bool isNullable, ReferenceTypeNullHandling defaultReferenceTypeNullHandling)
        {
            var contract = settings.ResolveContract(originalType);
            if (contract is JsonStringContract)
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, isNullable, null);
            }

            return base.GetDescription(contextualType, settings, originalType, isNullable, defaultReferenceTypeNullHandling);
        }

        public override bool IsNullable(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling)
        {
            var jsonPropertyAttribute = contextualType.GetContextAttribute<JsonPropertyAttribute>();
            if (jsonPropertyAttribute != null && jsonPropertyAttribute.Required == Required.DisallowNull)
            {
                return false;
            }

            return base.IsNullable(contextualType, defaultReferenceTypeNullHandling);
        }

        public override bool IsStringEnum(ContextualType contextualType, NewtonsoftJsonSchemaGeneratorSettings settings)
        {
            if (settings.SerializerSettings.Converters.OfType<StringEnumConverter>().Any())
            {
                return true;
            }

            return base.IsStringEnum(contextualType, settings);
        }

        public override void GenerateProperties(Type type, JsonSchema schema, NewtonsoftJsonSchemaGeneratorSettings settings, JsonSchemaGenerator schemaGenerator, JsonSchemaResolver schemaResolver)
        {
            // TODO(reflection): Here we should use ContextualAccessorInfo to avoid losing information

            var members = type.GetTypeInfo()
                .DeclaredFields
                .Where(f => !f.IsPrivate && !f.IsStatic || f.IsDefined(typeof(DataMemberAttribute)))
                .OfType<MemberInfo>()
                .Concat(
                    type.GetTypeInfo().DeclaredProperties
                    .Where(p => p.GetMethod?.IsPrivate != true && p.GetMethod?.IsStatic == false ||
                                p.SetMethod?.IsPrivate != true && p.SetMethod?.IsStatic == false ||
                                p.IsDefined(typeof(DataMemberAttribute)))
                )
                .ToList();

            var contextualAccessors = members.Select(m => m.ToContextualAccessor()); // TODO(reflection): Do not use this method
            var contract = settings.ResolveContract(type);

            var allowedProperties = schemaGenerator.GetTypeProperties(type);
            var objectContract = contract as JsonObjectContract;
            if (objectContract != null && allowedProperties == null)
            {
                foreach (var jsonProperty in objectContract.Properties.Where(p => p.DeclaringType == type))
                {
                    bool shouldSerialize;
                    try
                    {
                        shouldSerialize = jsonProperty.ShouldSerialize?.Invoke(null) != false;
                    }
                    catch
                    {
                        shouldSerialize = true;
                    }

                    if (shouldSerialize)
                    {
                        var memberInfo = contextualAccessors.FirstOrDefault(p => p.Name == jsonProperty.UnderlyingName);
                        if (memberInfo != null && (settings.GenerateAbstractProperties || !schemaGenerator.IsAbstractProperty(memberInfo)))
                        {
                            LoadPropertyOrField(jsonProperty, memberInfo, type, schema, settings, schemaGenerator, schemaResolver);
                        }
                    }
                }
            }
            else
            {
                // TODO: Remove this hacky code (used to support serialization of exceptions and restore the old behavior [pre 9.x])
                foreach (var memberInfo in contextualAccessors.Where(m => allowedProperties == null || allowedProperties.Contains(m.Name)))
                {
                    var attribute = memberInfo.GetContextAttribute<JsonPropertyAttribute>();
                    var memberType = (memberInfo as ContextualPropertyInfo)?.PropertyInfo.PropertyType ??
                                     (memberInfo as ContextualFieldInfo)?.FieldInfo.FieldType;

                    var jsonProperty = new JsonProperty
                    {
                        AttributeProvider = new ReflectionAttributeProvider(memberInfo),
                        PropertyType = memberType,
                        Ignored = schemaGenerator.IsPropertyIgnored(memberInfo, type)
                    };

                    if (attribute != null)
                    {
                        jsonProperty.PropertyName = attribute.PropertyName ?? memberInfo.Name;
                        jsonProperty.Required = attribute.Required;
                        jsonProperty.DefaultValueHandling = attribute.DefaultValueHandling;
                        jsonProperty.TypeNameHandling = attribute.TypeNameHandling;
                        jsonProperty.NullValueHandling = attribute.NullValueHandling;
                        jsonProperty.TypeNameHandling = attribute.TypeNameHandling;
                    }
                    else
                    {
                        jsonProperty.PropertyName = memberInfo.Name;
                    }

                    LoadPropertyOrField(jsonProperty, memberInfo, type, schema, settings, schemaGenerator, schemaResolver);
                }
            }
        }

        private void LoadPropertyOrField(JsonProperty jsonProperty, ContextualAccessorInfo accessorInfo, Type parentType, JsonSchema parentSchema, NewtonsoftJsonSchemaGeneratorSettings settings, JsonSchemaGenerator schemaGenerator, JsonSchemaResolver schemaResolver)
        {
            var propertyTypeDescription = ((IReflectionService)this).GetDescription(accessorInfo.AccessorType, settings);
            if (jsonProperty.Ignored == false && schemaGenerator.IsPropertyIgnoredBySettings(accessorInfo) == false)
            {
                var propertyName = GetPropertyName(jsonProperty, accessorInfo, settings);
                var propertyAlreadyExists = parentSchema.Properties.ContainsKey(propertyName);

                if (propertyAlreadyExists)
                {
                    if (settings.GetActualFlattenInheritanceHierarchy(parentType))
                    {
                        parentSchema.Properties.Remove(propertyName);
                    }
                    else
                    {
                        throw new InvalidOperationException("The JSON property '" + propertyName + "' is defined multiple times on type '" + parentType.FullName + "'.");
                    }
                }

                var requiredAttribute = accessorInfo.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.RequiredAttribute");

                var hasJsonNetAttributeRequired = jsonProperty.Required == Required.Always || jsonProperty.Required == Required.AllowNull;
                var isDataContractMemberRequired = schemaGenerator.GetDataMemberAttribute(accessorInfo, parentType)?.IsRequired == true;

                var hasRequiredAttribute = requiredAttribute != null;
                if (hasRequiredAttribute || isDataContractMemberRequired || hasJsonNetAttributeRequired)
                {
                    parentSchema.RequiredProperties.Add(propertyName);
                }

                var isNullable = propertyTypeDescription.IsNullable &&
                    hasRequiredAttribute == false &&
                    (jsonProperty.Required == Required.Default || jsonProperty.Required == Required.AllowNull);

                var defaultValue = jsonProperty.DefaultValue;

                schemaGenerator.AddProperty(parentSchema, accessorInfo, propertyTypeDescription, propertyName, requiredAttribute, hasRequiredAttribute, isNullable, defaultValue, schemaResolver);
            }
        }

        public override string ConvertEnumValue(object value, NewtonsoftJsonSchemaGeneratorSettings settings)
        {
            var converters = settings.SerializerSettings.Converters.ToList();
            if (!converters.OfType<StringEnumConverter>().Any())
            {
                converters.Add(new StringEnumConverter());
            }

            var json = JsonConvert.SerializeObject(value, Formatting.None, converters.ToArray());
            var enumString = JsonConvert.DeserializeObject<string>(json);
            return enumString;
        }

        private string GetPropertyName(JsonProperty jsonProperty, ContextualAccessorInfo accessorInfo, NewtonsoftJsonSchemaGeneratorSettings settings)
        {
            if (jsonProperty?.PropertyName != null)
            {
                return jsonProperty.PropertyName;
            }

            try
            {
                var propertyName = accessorInfo.GetName();

                var contractResolver = settings.ActualContractResolver as DefaultContractResolver;
                return contractResolver != null
                    ? contractResolver.GetResolvedPropertyName(propertyName)
                    : propertyName;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Could not get JSON property name of property '" +
                    (accessorInfo != null ? accessorInfo.Name : "n/a") + "' and type '" +
                    (accessorInfo?.MemberInfo?.DeclaringType != null ? accessorInfo.MemberInfo.DeclaringType.FullName : "n/a") + "'.", e);
            }
        }
    }
}
