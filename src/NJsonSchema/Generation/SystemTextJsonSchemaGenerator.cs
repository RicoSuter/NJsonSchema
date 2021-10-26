//-----------------------------------------------------------------------
// <copyright file="SystemTextJsonSchemaGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NJsonSchema.Generation
{
    /// <summary>
    /// 
    /// </summary>
    public class SystemTextJsonSchemaGenerator : JsonSchemaGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public SystemTextJsonSchemaGenerator(SystemTextJsonSchemaGeneratorSettings settings)
            : base(settings)
        {
        }

        /// <summary>Gets the settings.</summary>
        public new SystemTextJsonSchemaGeneratorSettings Settings => (SystemTextJsonSchemaGeneratorSettings)base.Settings;

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>()
        {
            return FromType<TType>(new SystemTextJsonSchemaGeneratorSettings());
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type)
        {
            return FromType(type, new SystemTextJsonSchemaGeneratorSettings());
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>(SystemTextJsonSchemaGeneratorSettings settings)
        {
            var generator = new SystemTextJsonSchemaGenerator(settings);
            return generator.Generate(typeof(TType));
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type, SystemTextJsonSchemaGeneratorSettings settings)
        {
            var generator = new SystemTextJsonSchemaGenerator(settings);
            return generator.Generate(type);
        }

        /// <inheritdocs />
        protected override void GenerateProperties(Type type, JsonSchema schema, JsonSchemaResolver schemaResolver)
        {
            foreach (var accessorInfo in type.GetContextualAccessors())
            {
                if (accessorInfo.MemberInfo is FieldInfo fieldInfo && (fieldInfo.IsPrivate || fieldInfo.IsStatic || !fieldInfo.IsDefined(typeof(DataMemberAttribute))))
                {
                    continue;
                }

                if (accessorInfo.MemberInfo is PropertyInfo propertyInfo && (
                    !(propertyInfo.GetMethod?.IsPrivate != true && propertyInfo.GetMethod?.IsStatic == false) ||
                    !(propertyInfo.SetMethod?.IsPrivate != true && propertyInfo.SetMethod?.IsStatic == false) &&
                    !propertyInfo.IsDefined(typeof(DataMemberAttribute))))
                {
                    continue;
                }

                var propertyIgnored = false;
                var jsonIgnoreAttribute = accessorInfo
                    .ContextAttributes
                    .FirstAssignableToTypeNameOrDefault("System.Text.Json.Serialization.JsonIgnoreAttribute", TypeNameStyle.FullName);

                if (jsonIgnoreAttribute != null)
                {
                    var condition = jsonIgnoreAttribute.TryGetPropertyValue<object>("Condition");
                    if (condition is null || condition.ToString() == "Always")
                    {
                        propertyIgnored = true;
                    }
                }

                var ignored = propertyIgnored 
                    || IsPropertyIgnoredBySettings(accessorInfo) 
                    || accessorInfo.ContextAttributes
                    .FirstAssignableToTypeNameOrDefault("System.Text.Json.Serialization.JsonExtensionDataAttribute", TypeNameStyle.FullName) != null;

                if (!ignored)
                {
                    var propertyTypeDescription = Settings.ReflectionService.GetDescription(accessorInfo.AccessorType, Settings);
                    var propertyName = GetPropertyName(accessorInfo);
                    var propertyAlreadyExists = schema.Properties.ContainsKey(propertyName);

                    if (propertyAlreadyExists)
                    {
                        if (Settings.GetActualFlattenInheritanceHierarchy(type))
                        {
                            schema.Properties.Remove(propertyName);
                        }
                        else
                        {
                            throw new InvalidOperationException("The JSON property '" + propertyName + "' is defined multiple times on type '" + type.FullName + "'.");
                        }
                    }

                    var requiredAttribute = accessorInfo.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.RequiredAttribute");

                    var isDataContractMemberRequired = GetDataMemberAttribute(accessorInfo, type)?.IsRequired == true;

                    var hasRequiredAttribute = requiredAttribute != null;
                    if (hasRequiredAttribute || isDataContractMemberRequired)
                    {
                        schema.RequiredProperties.Add(propertyName);
                    }

                    var isNullable = propertyTypeDescription.IsNullable && hasRequiredAttribute == false; 

                    // TODO: Add default value
                    AddProperty(accessorInfo, schema, schemaResolver, propertyTypeDescription, propertyName, requiredAttribute, hasRequiredAttribute, isNullable, null);
                }
            }
        }

        /// <inheritdocs />
        protected override string ConvertEnumValue(object value)
        {
            // TODO(performance): How to improve this one here?
            var settings = new JsonSerializerOptions();
            foreach (var converter in Settings.SerializerOptions.Converters)
            {
                settings.Converters.Add(converter);
            }

            if (!settings.Converters.OfType<JsonStringEnumConverter>().Any())
            {
                settings.Converters.Add(new JsonStringEnumConverter());
            }

            var json = JsonSerializer.Serialize(value, value.GetType(), settings);
            return JsonSerializer.Deserialize<string>(json);
        }

        private string GetPropertyName(ContextualAccessorInfo accessorInfo)
        {
            dynamic jsonPropertyNameAttribute = accessorInfo.ContextAttributes
                               .FirstAssignableToTypeNameOrDefault("System.Text.Json.Serialization.JsonPropertyNameAttribute", TypeNameStyle.FullName);

            if (jsonPropertyNameAttribute != null && !string.IsNullOrEmpty(jsonPropertyNameAttribute.Name))
            {
                return jsonPropertyNameAttribute.Name;
            }

            if (Settings.SerializerOptions.PropertyNamingPolicy != null)
            {
                return Settings.SerializerOptions.PropertyNamingPolicy.ConvertName(accessorInfo.Name);
            }

            return accessorInfo.Name;
        }
    }
}