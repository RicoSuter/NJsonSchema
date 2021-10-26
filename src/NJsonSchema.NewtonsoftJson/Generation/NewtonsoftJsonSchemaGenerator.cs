using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using NJsonSchema.Infrastructure;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace NJsonSchema.NewtonsoftJson.Generation
{
    public class NewtonsoftJsonSchemaGenerator : JsonSchemaGenerator
    {
        public NewtonsoftJsonSchemaGenerator(NewtonsoftJsonSchemaGeneratorSettings settings)
            : base(settings)
        {
        }

        /// <summary>Gets the settings.</summary>
        public new NewtonsoftJsonSchemaGeneratorSettings Settings => (NewtonsoftJsonSchemaGeneratorSettings)base.Settings;

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>()
        {
            return FromType<TType>(new NewtonsoftJsonSchemaGeneratorSettings());
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type)
        {
            return FromType(type, new NewtonsoftJsonSchemaGeneratorSettings());
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>(NewtonsoftJsonSchemaGeneratorSettings settings)
        {
            var generator = new NewtonsoftJsonSchemaGenerator(settings);
            return generator.Generate(typeof(TType));
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type, NewtonsoftJsonSchemaGeneratorSettings settings)
        {
            var generator = new NewtonsoftJsonSchemaGenerator(settings);
            return generator.Generate(type);
        }

        protected override void GenerateProperties(Type type, JsonSchema schema, JsonSchemaResolver schemaResolver)
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
            var contract = Settings.ResolveContract(type);

            var allowedProperties = GetTypeProperties(type);
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
                        if (memberInfo != null && (Settings.GenerateAbstractProperties || !IsAbstractProperty(memberInfo)))
                        {
                            LoadPropertyOrField(jsonProperty, memberInfo, type, schema, schemaResolver);
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
                        Ignored = IsPropertyIgnored(memberInfo, type)
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

                    LoadPropertyOrField(jsonProperty, memberInfo, type, schema, schemaResolver);
                }
            }
        }

        private void LoadPropertyOrField(JsonProperty jsonProperty, ContextualAccessorInfo accessorInfo, Type parentType, JsonSchema parentSchema, JsonSchemaResolver schemaResolver)
        {
            var propertyTypeDescription = Settings.ReflectionService.GetDescription(accessorInfo.AccessorType, Settings);
            if (jsonProperty.Ignored == false && IsPropertyIgnoredBySettings(accessorInfo) == false)
            {
                var propertyName = GetPropertyName(jsonProperty, accessorInfo);
                var propertyAlreadyExists = parentSchema.Properties.ContainsKey(propertyName);

                if (propertyAlreadyExists)
                {
                    if (Settings.GetActualFlattenInheritanceHierarchy(parentType))
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
                var isDataContractMemberRequired = GetDataMemberAttribute(accessorInfo, parentType)?.IsRequired == true;

                var hasRequiredAttribute = requiredAttribute != null;
                if (hasRequiredAttribute || isDataContractMemberRequired || hasJsonNetAttributeRequired)
                {
                    parentSchema.RequiredProperties.Add(propertyName);
                }

                var isNullable = propertyTypeDescription.IsNullable &&
                    hasRequiredAttribute == false &&
                    (jsonProperty.Required == Required.Default || jsonProperty.Required == Required.AllowNull);

                var defaultValue = jsonProperty.DefaultValue;

                AddProperty(accessorInfo, parentSchema, schemaResolver, propertyTypeDescription, propertyName, requiredAttribute, hasRequiredAttribute, isNullable, defaultValue);
            }
        }

        protected override string ConvertEnumValue(object value)
        {
            var converters = Settings.SerializerSettings.Converters.ToList();
            if (!converters.OfType<StringEnumConverter>().Any())
            {
                converters.Add(new StringEnumConverter());
            }

            var json = JsonConvert.SerializeObject(value, Formatting.None, converters.ToArray());
            var enumString = JsonConvert.DeserializeObject<string>(json);
            return enumString;
        }

        private string GetPropertyName(JsonProperty jsonProperty, ContextualAccessorInfo accessorInfo)
        {
            if (jsonProperty?.PropertyName != null)
            {
                return jsonProperty.PropertyName;
            }

            try
            {
                var propertyName = accessorInfo.GetName();

                var contractResolver = Settings.ActualContractResolver as DefaultContractResolver;
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
