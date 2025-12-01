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
using Newtonsoft.Json.Serialization;
using NJsonSchema.Infrastructure;
using System.Runtime.Serialization;
using System.Reflection;
using NJsonSchema.Generation;

namespace NJsonSchema.NewtonsoftJson.Generation
{
    /// <inheritdoc />
    public class NewtonsoftJsonReflectionService : ReflectionServiceBase<NewtonsoftJsonSchemaGeneratorSettings>
    {
        /// <inheritdoc />
        protected override JsonTypeDescription GetDescription(ContextualType contextualType, NewtonsoftJsonSchemaGeneratorSettings settings,
            Type originalType, bool isNullable, ReferenceTypeNullHandling defaultReferenceTypeNullHandling)
        {
            var contract = settings.ResolveContract(originalType);
            if (contract is JsonStringContract)
            {
                var description = base.GetDescription(contextualType, settings, originalType, isNullable, defaultReferenceTypeNullHandling);
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, isNullable, description.Format);
            }

            return base.GetDescription(contextualType, settings, originalType, isNullable, defaultReferenceTypeNullHandling);
        }

        /// <inheritdoc />
        public override bool IsNullable(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling)
        {
            var jsonPropertyAttribute = contextualType.GetContextAttribute<JsonPropertyAttribute>(true);
            if (jsonPropertyAttribute != null && jsonPropertyAttribute.Required == Required.DisallowNull)
            {
                return false;
            }

            return base.IsNullable(contextualType, defaultReferenceTypeNullHandling);
        }

        /// <inheritdoc />
        public override bool IsStringEnum(ContextualType contextualType, NewtonsoftJsonSchemaGeneratorSettings settings)
        {
            var hasGlobalStringEnumConverter = settings.SerializerSettings?.Converters.OfType<StringEnumConverter>().Any() == true;
            return hasGlobalStringEnumConverter || base.IsStringEnum(contextualType, settings);
        }


        /// <inheritdoc />
        public override Func<object, string?> GetEnumValueConverter(NewtonsoftJsonSchemaGeneratorSettings settings)
        {
            var converters = settings.SerializerSettings.Converters.ToList();
            if (!converters.OfType<StringEnumConverter>().Any())
            {
                converters.Add(new StringEnumConverter());
            }

            return x => JsonConvert.DeserializeObject<string?>(JsonConvert.SerializeObject(x, Formatting.None, [.. converters]));
        }

        /// <inheritdoc />
        public override void GenerateProperties(JsonSchema schema, ContextualType contextualType, NewtonsoftJsonSchemaGeneratorSettings settings, JsonSchemaGenerator schemaGenerator, JsonSchemaResolver schemaResolver)
        {
            var contextualAccessors = contextualType
                .Properties
                .Where(p => p.PropertyInfo.DeclaringType == contextualType.Type &&
                            (p.PropertyInfo.GetMethod?.IsPrivate != true && p.PropertyInfo.GetMethod?.IsStatic == false ||
                             p.PropertyInfo.SetMethod?.IsPrivate != true && p.PropertyInfo.SetMethod?.IsStatic == false ||
                             p.PropertyInfo.IsDefined(typeof(DataMemberAttribute))))
                .OfType<ContextualAccessorInfo>()
                .Concat(contextualType
                    .Fields
                    .Where(f => f.FieldInfo.DeclaringType == contextualType.Type &&
                                (!f.FieldInfo.IsPrivate &&
                                 !f.FieldInfo.IsStatic || f.FieldInfo.IsDefined(typeof(DataMemberAttribute)))));

            var contract = settings.ResolveContract(contextualType.Type);

            var allowedProperties = schemaGenerator.GetTypeProperties(contextualType.Type);
            if (allowedProperties == null && contract is JsonObjectContract objectContract)
            {
                foreach (var jsonProperty in objectContract.Properties.Where(p => p.DeclaringType == contextualType.Type))
                {
                    bool shouldSerialize;
                    try
                    {
                        shouldSerialize = jsonProperty.ShouldSerialize?.Invoke(null!) != false;
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
                            LoadPropertyOrField(jsonProperty, memberInfo, contextualType.Type, schema, settings, schemaGenerator, schemaResolver);
                        }
                    }
                }
            }
            else
            {
                // TODO: Remove this hacky code (used to support serialization of exceptions and restore the old behavior [pre 9.x])
                foreach (var accessorInfo in contextualAccessors.Where(m => allowedProperties == null || allowedProperties.Contains(m.Name)))
                {
                    var attribute = accessorInfo.GetAttribute<JsonPropertyAttribute>(true);
                    var memberType = (accessorInfo as ContextualPropertyInfo)?.PropertyInfo.PropertyType ??
                                     (accessorInfo as ContextualFieldInfo)?.FieldInfo.FieldType;

                    var jsonProperty = new JsonProperty
                    {
                        AttributeProvider = new ReflectionAttributeProvider(accessorInfo),
                        PropertyType = memberType,
                        Ignored = schemaGenerator.IsPropertyIgnored(accessorInfo, contextualType.Type)
                    };

                    if (attribute != null)
                    {
                        jsonProperty.PropertyName = attribute.PropertyName ?? accessorInfo.Name;
                        jsonProperty.Required = attribute.Required;
                        jsonProperty.DefaultValueHandling = attribute.DefaultValueHandling;
                        jsonProperty.TypeNameHandling = attribute.TypeNameHandling;
                        jsonProperty.NullValueHandling = attribute.NullValueHandling;
                        jsonProperty.TypeNameHandling = attribute.TypeNameHandling;
                    }
                    else
                    {
                        jsonProperty.PropertyName = accessorInfo.Name;
                    }

                    LoadPropertyOrField(jsonProperty, accessorInfo, contextualType.Type, schema, settings, schemaGenerator, schemaResolver);
                }
            }
        }

        /// <inheritdoc />
        public override string GetPropertyName(ContextualAccessorInfo accessorInfo, JsonSchemaGeneratorSettings settings)
        {
            return GetPropertyName(null, accessorInfo, (NewtonsoftJsonSchemaGeneratorSettings)settings);
        }

        private void LoadPropertyOrField(JsonProperty jsonProperty, ContextualAccessorInfo accessorInfo, Type parentType, JsonSchema parentSchema, NewtonsoftJsonSchemaGeneratorSettings settings, JsonSchemaGenerator schemaGenerator, JsonSchemaResolver schemaResolver)
        {
            var propertyTypeDescription = ((IReflectionService)this).GetDescription(accessorInfo.AccessorType, settings);
            if (!jsonProperty.Ignored && !schemaGenerator.IsPropertyIgnoredBySettings(accessorInfo))
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

                var requiredAttribute = accessorInfo
                    .GetAttributes(true)
                    .FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.RequiredAttribute");

                var hasJsonNetAttributeRequired = jsonProperty.Required is Required.Always or Required.AllowNull;
                var isDataContractMemberRequired = schemaGenerator.GetDataMemberAttribute(accessorInfo, parentType)?.IsRequired == true;

                var hasRequiredAttribute = requiredAttribute != null;
                if (hasRequiredAttribute || isDataContractMemberRequired || hasJsonNetAttributeRequired)
                {
                    parentSchema.RequiredProperties.Add(propertyName);
                }

                var isNullable = propertyTypeDescription.IsNullable && !hasRequiredAttribute && jsonProperty.Required is Required.Default or Required.AllowNull;

                var defaultValue = jsonProperty.DefaultValue;

                schemaGenerator.AddProperty(parentSchema, accessorInfo, propertyTypeDescription, propertyName, requiredAttribute, hasRequiredAttribute, isNullable, defaultValue, schemaResolver);
            }
        }

        private static string GetPropertyName(JsonProperty? jsonProperty, ContextualAccessorInfo accessorInfo, NewtonsoftJsonSchemaGeneratorSettings settings)
        {
            if (jsonProperty?.PropertyName != null)
            {
                return jsonProperty.PropertyName;
            }

            try
            {
                var propertyName = accessorInfo.GetName();

                return settings.ActualContractResolver is DefaultContractResolver contractResolver
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
