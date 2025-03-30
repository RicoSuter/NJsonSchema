//-----------------------------------------------------------------------
// <copyright file="DefaultReflectionService.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NJsonSchema.Generation
{
    /// <inheritdoc />
    public class SystemTextJsonReflectionService : ReflectionServiceBase<SystemTextJsonSchemaGeneratorSettings>
    {
        /// <inheritdoc />
        public override void GenerateProperties(JsonSchema schema, ContextualType contextualType, SystemTextJsonSchemaGeneratorSettings settings, JsonSchemaGenerator schemaGenerator, JsonSchemaResolver schemaResolver)
        {
            foreach (var accessorInfo in contextualType.Properties.OfType<ContextualAccessorInfo>().Concat(contextualType.Fields))
            {
                if (accessorInfo.MemberInfo.DeclaringType != contextualType.Type ||
                    (accessorInfo.MemberInfo is FieldInfo fieldInfo && (fieldInfo.IsPrivate || fieldInfo.IsStatic || !fieldInfo.IsDefined(typeof(DataMemberAttribute)))))
                {
                    continue;
                }

                if (accessorInfo.MemberInfo is PropertyInfo propertyInfo &&
                    (propertyInfo.GetMethod == null || propertyInfo.GetMethod.IsPrivate || propertyInfo.GetMethod.IsStatic) &&
                    (propertyInfo.SetMethod == null || propertyInfo.SetMethod.IsPrivate || propertyInfo.SetMethod.IsStatic) &&
                    !propertyInfo.IsDefined(typeof(DataMemberAttribute)))
                {
                    continue;
                }

                if (accessorInfo.Name == "EqualityContract" &&
                    accessorInfo.IsAttributeDefined<CompilerGeneratedAttribute>(true))
                {
                    continue;
                }

                if (accessorInfo.MemberInfo is PropertyInfo propInfo &&
                    propInfo.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                var propertyIgnored = false;
                var attributes = accessorInfo.GetAttributes(true).ToArray();
                var jsonIgnoreAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.Text.Json.Serialization.JsonIgnoreAttribute", TypeNameStyle.FullName);

                if (jsonIgnoreAttribute != null)
                {
                    var condition = jsonIgnoreAttribute.TryGetPropertyValue<object>("Condition");
                    if (condition is null || condition.ToString() == "Always")
                    {
                        propertyIgnored = true;
                    }
                }

                var ignored = propertyIgnored
                    || schemaGenerator.IsPropertyIgnoredBySettings(accessorInfo)
                    || attributes.FirstAssignableToTypeNameOrDefault("System.Text.Json.Serialization.JsonExtensionDataAttribute", TypeNameStyle.FullName) != null
                    || Array.IndexOf(settings.ExcludedTypeNames, accessorInfo.AccessorType.Type.FullName) != -1;

                if (!ignored)
                {
                    var propertyTypeDescription = GetDescription(accessorInfo.AccessorType, settings.DefaultReferenceTypeNullHandling, settings);
                    var propertyName = GetPropertyNameInternal(accessorInfo, settings);

                    var propertyAlreadyExists = schema.Properties.ContainsKey(propertyName);
                    if (propertyAlreadyExists)
                    {
                        if (settings.GetActualFlattenInheritanceHierarchy(contextualType.Type))
                        {
                            schema.Properties.Remove(propertyName);
                        }
                        else
                        {
                            throw new InvalidOperationException($"The JSON property '{propertyName}' is defined multiple times on type '{contextualType.Type.FullName}'.");
                        }
                    }

                    var requiredAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.RequiredAttribute");

                    var isDataContractMemberRequired = schemaGenerator.GetDataMemberAttribute(accessorInfo, contextualType.Type)?.IsRequired == true;

                    var hasRequiredAttribute = requiredAttribute != null;
                    if (hasRequiredAttribute || isDataContractMemberRequired)
                    {
                        schema.RequiredProperties.Add(propertyName);
                    }

                    var isNullable = propertyTypeDescription.IsNullable && !hasRequiredAttribute;

                    // TODO: Add default value
                    schemaGenerator.AddProperty(schema, accessorInfo, propertyTypeDescription, propertyName, requiredAttribute, hasRequiredAttribute, isNullable, null, schemaResolver);
                }
            }
        }

        /// <inheritdoc />
        public override bool IsStringEnum(ContextualType contextualType, SystemTextJsonSchemaGeneratorSettings settings)
        {
            var hasGlobalStringEnumConverter = settings.SerializerOptions.Converters.OfType<JsonStringEnumConverter>().Any();
            return hasGlobalStringEnumConverter || base.IsStringEnum(contextualType, settings);
        }

        /// <inheritdoc />
        public override Func<object, string?> GetEnumValueConverter(SystemTextJsonSchemaGeneratorSettings settings)
        {
            var serializerOptions = new JsonSerializerOptions();
            foreach (var converter in settings.SerializerOptions.Converters)
            {
                serializerOptions.Converters.Add(converter);
            }

            if (!serializerOptions.Converters.OfType<JsonStringEnumConverter>().Any())
            {
                serializerOptions.Converters.Add(new JsonStringEnumConverter());
            }

            return x => JsonSerializer.Deserialize<string?>(JsonSerializer.Serialize(x, x.GetType(), serializerOptions));
        }

        /// <inheritdoc />
        public override string GetPropertyName(ContextualAccessorInfo accessorInfo, JsonSchemaGeneratorSettings settings)
        {
            return GetPropertyNameInternal(accessorInfo, (SystemTextJsonSchemaGeneratorSettings)settings);
        }

        private static string GetPropertyNameInternal(ContextualAccessorInfo accessorInfo, SystemTextJsonSchemaGeneratorSettings settings)
        {
            dynamic? jsonPropertyNameAttribute = accessorInfo.GetAttributes(true)
                .FirstAssignableToTypeNameOrDefault("System.Text.Json.Serialization.JsonPropertyNameAttribute", TypeNameStyle.FullName);

            if (!string.IsNullOrEmpty(jsonPropertyNameAttribute?.Name))
            {
                return jsonPropertyNameAttribute!.Name;
            }

            if (settings.SerializerOptions.PropertyNamingPolicy != null)
            {
                return settings.SerializerOptions.PropertyNamingPolicy.ConvertName(accessorInfo.Name);
            }

            return accessorInfo.Name;
        }
    }
}
