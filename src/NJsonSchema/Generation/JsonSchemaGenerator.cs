//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Annotations;
using NJsonSchema.Converters;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Generation
{
    /// <summary>Generates a <see cref="JsonSchema4"/> object for a given type. </summary>
    public class JsonSchemaGenerator
    {
        private static readonly Dictionary<string, string> DataTypeFormats = new Dictionary<string, string>
        {
            {"DateTime", JsonFormatStrings.DateTime},
            {"Date", JsonFormatStrings.Date},
            {"Time", JsonFormatStrings.Time},
            {"EmailAddress", JsonFormatStrings.Email},
            {"PhoneNumber", JsonFormatStrings.Phone},
            {"Url", JsonFormatStrings.Uri}
        };

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaGenerator"/> class.</summary>
        /// <param name="settings">The settings.</param>
        public JsonSchemaGenerator(JsonSchemaGeneratorSettings settings)
        {
            Settings = settings;
        }

        /// <summary>Gets the settings.</summary>
        public JsonSchemaGeneratorSettings Settings { get; }

        /// <summary>Generates a <see cref="JsonSchema4" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        public JsonSchema4 Generate(Type type)
        {
            var schemaResolver = new SchemaResolver(Settings);
            return Generate<JsonSchema4>(type, null, schemaResolver);
        }

        /// <summary>Generates a <see cref="JsonSchema4" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="type">The type.</param>
        /// <param name="schemaResolver"></param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        public JsonSchema4 Generate(Type type, SchemaResolver schemaResolver)
        {
            return Generate<JsonSchema4>(type, null, schemaResolver);
        }

        /// <summary>Generates a <see cref="JsonSchema4" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="type">The type.</param>
        /// <param name="parentAttributes">The parent property or parameter attributes.</param>
        /// <param name="schemaResolver"></param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        public virtual TSchemaType Generate<TSchemaType>(Type type, IEnumerable<Attribute> parentAttributes, SchemaResolver schemaResolver)
            where TSchemaType : JsonSchema4, new()
        {
            var schema = HandleSpecialTypes<TSchemaType>(type, schemaResolver);
            if (schema != null)
                return schema;

            schema = new TSchemaType();
            if (!schemaResolver.Schemas.Any() && type.GetTypeInfo().IsClass)
                schema.Title = Settings.SchemaNameGenerator.Generate(type);

            ApplyExtensionDataAttributes(schema, type, parentAttributes);

            var typeDescription = JsonObjectTypeDescription.FromType(type, parentAttributes, Settings.DefaultEnumHandling);
            if (typeDescription.Type.HasFlag(JsonObjectType.Object))
            {
                if (typeDescription.IsDictionary)
                {
                    typeDescription.ApplyType(schema);
                    GenerateDictionary(type, schema, schemaResolver);
                }
                else
                {
                    if (schemaResolver.HasSchema(type, false))
                    {
                        schema.SchemaReference = schemaResolver.GetSchema(type, false);
                        return schema;
                    }

                    if (schema.GetType() == typeof(JsonSchema4))
                    {
                        typeDescription.ApplyType(schema);
                        schema.Description = GetDescription(type.GetTypeInfo(), type.GetTypeInfo().GetCustomAttributes());
                        GenerateObject(type, schema, schemaResolver);
                    }
                    else
                    {
                        schema.SchemaReference = Generate<JsonSchema4>(type, parentAttributes, schemaResolver);
                        return schema;
                    }
                }
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                var isIntegerEnumeration = typeDescription.Type == JsonObjectType.Integer;
                if (schemaResolver.HasSchema(type, isIntegerEnumeration))
                {
                    schema.SchemaReference = schemaResolver.GetSchema(type, isIntegerEnumeration);
                    return schema;
                }

                if (schema.GetType() == typeof(JsonSchema4))
                {
                    LoadEnumerations(type, schema, typeDescription);

                    typeDescription.ApplyType(schema);
                    schema.Description = type.GetXmlSummary();

                    schemaResolver.AddSchema(type, isIntegerEnumeration, schema);
                }
                else
                {
                    schema.SchemaReference = Generate<JsonSchema4>(type, parentAttributes, schemaResolver);
                    return schema;
                }
            }
            else if (typeDescription.Type.HasFlag(JsonObjectType.Array))
            {
                typeDescription.ApplyType(schema);

                var itemType = type.GetEnumerableItemType();
                if (itemType == null)
                {
                    var jsonSchemaAttribute = type.GetTypeInfo().GetCustomAttribute<JsonSchemaAttribute>();
                    if (jsonSchemaAttribute?.ArrayItem != null)
                        schema.Item = GenerateWithReference(schemaResolver, itemType);
                    else
                        schema.Item = JsonSchema4.CreateAnySchema();
                }
                else
                    schema.Item = GenerateWithReference(schemaResolver, itemType);
            }
            else
                typeDescription.ApplyType(schema);

            return schema;
        }

        private JsonSchema4 GenerateWithReference(SchemaResolver schemaResolver, Type itemType)
        {
            if (RequiresSchemaReference(itemType, null))
                return new JsonSchema4 { SchemaReference = Generate(itemType, schemaResolver) };

            return Generate(itemType, schemaResolver);
        }

        private void ApplyExtensionDataAttributes<TSchemaType>(TSchemaType schema, Type type, IEnumerable<Attribute> parentAttributes)
            where TSchemaType : JsonSchema4, new()
        {
            if (parentAttributes == null)
            {
                // class
                var extensionDataAttributes = type.GetTypeInfo().GetCustomAttributes<JsonSchemaExtensionDataAttribute>().ToArray();
                if (extensionDataAttributes.Any())
                    schema.ExtensionData = extensionDataAttributes.ToDictionary(a => a.Property, a => a.Value);
            }
            else
            {
                // property or parameter
                var extensionDataAttributes = parentAttributes.OfType<JsonSchemaExtensionDataAttribute>().ToArray();
                if (extensionDataAttributes.Any())
                    schema.ExtensionData = extensionDataAttributes.ToDictionary(a => a.Property, a => a.Value);
            }
        }

        private TSchemaType HandleSpecialTypes<TSchemaType>(Type type, SchemaResolver schemaResolver)
            where TSchemaType : JsonSchema4, new()
        {
            var typeMapper = Settings.TypeMappers.FirstOrDefault(m => m.MappedType == type);
            if (typeMapper != null)
            {
                var schema = typeMapper.GetSchema<TSchemaType>(this, schemaResolver);
                if (schema != null)
                    return schema;
            }

            if (type == typeof(JObject) || type == typeof(JToken) || type == typeof(object))
                return JsonSchema4.CreateAnySchema<TSchemaType>();

            return null;
        }

        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        private void GenerateDictionary<TSchemaType>(Type type, TSchemaType schema, SchemaResolver schemaResolver)
            where TSchemaType : JsonSchema4, new()
        {
            var genericTypeArguments = ReflectionExtensions.GetGenericTypeArguments(type);

            var valueType = genericTypeArguments.Length == 2 ? genericTypeArguments[1] : typeof(object);
            if (valueType == typeof(object))
                schema.AdditionalPropertiesSchema = JsonSchema4.CreateAnySchema();
            else
            {
                if (RequiresSchemaReference(valueType, null))
                {
                    schema.AdditionalPropertiesSchema = new JsonSchema4
                    {
                        SchemaReference = Generate(valueType, schemaResolver)
                    };
                }
                else
                    schema.AdditionalPropertiesSchema = Generate(valueType, schemaResolver);
            }

            schema.AllowAdditionalProperties = true;
        }

        /// <summary>Generates the properties for the given type and schema.</summary>
        /// <typeparam name="TSchemaType">The type of the schema type.</typeparam>
        /// <param name="type">The types.</param>
        /// <param name="schema">The properties</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        protected virtual void GenerateObject<TSchemaType>(Type type, TSchemaType schema, SchemaResolver schemaResolver)
            where TSchemaType : JsonSchema4, new()
        {
            schemaResolver.AddSchema(type, false, schema);

            schema.AllowAdditionalProperties = false;

            GeneratePropertiesAndInheritance(type, schema, schemaResolver);
            if (Settings.GenerateKnownTypes)
                GenerateKnownTypes(type, schemaResolver);
        }

        private void GeneratePropertiesAndInheritance(Type type, JsonSchema4 schema, SchemaResolver schemaResolver)
        {
            var properties = GetTypeProperties(type);

#if !LEGACY
            var declaredFields = type.GetTypeInfo().DeclaredFields
                .Where(f => f.IsPublic);

            var declaredProperties = type.GetTypeInfo().DeclaredProperties
                .Where(p => p.GetMethod?.IsPublic == true || p.SetMethod?.IsPublic == true);
#else
            var declaredFields = type.GetTypeInfo()
                .GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

            var declaredProperties = type.GetTypeInfo()
                .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetGetMethod()?.IsPublic == true || p.GetSetMethod()?.IsPublic == true);
#endif

            foreach (var property in declaredProperties.Where(p => properties == null || properties.Contains(p.Name)))
                LoadPropertyOrField(property, property.PropertyType, type, schema, schemaResolver);

            foreach (var field in declaredFields.Where(p => properties == null || properties.Contains(p.Name)))
                LoadPropertyOrField(field, field.FieldType, type, schema, schemaResolver);

            GenerateInheritance(type, schema, schemaResolver);
        }

        private void GenerateKnownTypes(Type type, SchemaResolver schemaResolver)
        {
            foreach (dynamic knownTypeAttribute in type.GetTypeInfo().GetCustomAttributes().Where(a => a.GetType().Name == "KnownTypeAttribute"))
            {
                var typeDescription = JsonObjectTypeDescription.FromType(knownTypeAttribute.Type, null, Settings.DefaultEnumHandling);
                var isIntegerEnum = typeDescription.Type == JsonObjectType.Integer;

                if (!schemaResolver.HasSchema(knownTypeAttribute.Type, isIntegerEnum))
                    Generate(knownTypeAttribute.Type, schemaResolver);
            }
        }

        private void GenerateInheritance(Type type, JsonSchema4 schema, SchemaResolver schemaResolver)
        {
            GenerateInheritanceDiscriminator(type, schema);

            var baseType = type.GetTypeInfo().BaseType;
            if (baseType != null && baseType != typeof(object))
            {
                if (Settings.FlattenInheritanceHierarchy)
                    GeneratePropertiesAndInheritance(baseType, schema, schemaResolver);
                else
                {
                    var baseSchema = Generate(baseType, schemaResolver);
                    schema.AllOf.Add(baseSchema);
                }
            }
        }

        private void GenerateInheritanceDiscriminator(Type type, JsonSchema4 schema)
        {
            if (!Settings.FlattenInheritanceHierarchy)
            {
                var discriminator = TryGetInheritanceDiscriminator(type.GetTypeInfo().GetCustomAttributes(false).OfType<Attribute>());
                if (!string.IsNullOrEmpty(discriminator))
                {
                    if (schema.Properties.ContainsKey(discriminator))
                        throw new InvalidOperationException("The JSON property '" + discriminator + "' is defined multiple times on type '" + type.FullName + "'.");

                    schema.Discriminator = discriminator;
                    schema.Properties[discriminator] = new JsonProperty
                    {
                        Type = JsonObjectType.String,
                        IsRequired = true
                    };
                }
            }
        }

        private string TryGetInheritanceDiscriminator(IEnumerable<Attribute> typeAttributes)
        {
            dynamic jsonConverterAttribute = typeAttributes?.FirstOrDefault(a => a.GetType().Name == "JsonConverterAttribute");
            if (jsonConverterAttribute != null)
            {
                var converterType = (Type)jsonConverterAttribute.ConverterType;
                if (converterType.Name == "JsonInheritanceConverter")
                {
                    if (jsonConverterAttribute.ConverterParameters != null && jsonConverterAttribute.ConverterParameters.Length > 0)
                        return jsonConverterAttribute.ConverterParameters[0];
                    return JsonInheritanceConverter.DefaultDiscriminatorName;
                }
            }
            return null;
        }

        /// <summary>Gets the properties of the given type or null to take all properties.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The property names or null for all.</returns>
        protected virtual string[] GetTypeProperties(Type type)
        {
            if (type == typeof(Exception))
                return new[] { "InnerException", "Message", "Source", "StackTrace" };

            return null;
        }

        private void LoadEnumerations(Type type, JsonSchema4 schema, JsonObjectTypeDescription typeDescription)
        {
            schema.Type = typeDescription.Type;
            schema.Enumeration.Clear();
            schema.EnumerationNames.Clear();

            foreach (var enumName in Enum.GetNames(type))
            {
                if (typeDescription.Type == JsonObjectType.Integer)
                {
                    var value = Convert.ChangeType(Enum.Parse(type, enumName), Enum.GetUnderlyingType(type));
                    schema.Enumeration.Add(value);
                }
                else
                {
                    var attributes = type.GetTypeInfo().GetDeclaredField(enumName).GetCustomAttributes(); // EnumMember only checked if StringEnumConverter is used
                    dynamic enumMemberAttribute = attributes.TryGetIfAssignableTo("System.Runtime.Serialization.EnumMemberAttribute");
                    if (enumMemberAttribute != null && !string.IsNullOrEmpty(enumMemberAttribute.Value))
                        schema.Enumeration.Add((string)enumMemberAttribute.Value);
                    else
                        schema.Enumeration.Add(enumName);
                }

                schema.EnumerationNames.Add(enumName);
            }
        }

        private void LoadPropertyOrField(MemberInfo property, Type propertyType, Type parentType, JsonSchema4 parentSchema, SchemaResolver schemaResolver)
        {
            var attributes = property.GetCustomAttributes(inherit: true).OfType<Attribute>().ToArray();
            var propertyTypeDescription = JsonObjectTypeDescription.FromType(propertyType, attributes, Settings.DefaultEnumHandling);

            if (IsPropertyIgnored(parentType, attributes) == false)
            {
                JsonProperty jsonProperty;

                if (propertyType.Name == "Nullable`1")
#if !LEGACY
                    propertyType = propertyType.GenericTypeArguments[0];
#else
                    propertyType = propertyType.GetGenericArguments()[0];
#endif

                var requiresSchemaReference = RequiresSchemaReference(propertyType, attributes);
                if (requiresSchemaReference)
                {
                    var propertySchema = Generate<JsonSchema4>(propertyType, attributes, schemaResolver);

                    // The schema is automatically added to Definitions if it is missing in JsonPathUtilities.GetJsonPath()
                    if (Settings.NullHandling == NullHandling.JsonSchema)
                    {
                        jsonProperty = new JsonProperty();
                        jsonProperty.OneOf.Add(new JsonSchema4
                        {
                            SchemaReference = propertySchema.ActualSchema
                        });
                    }
                    else
                    {
                        jsonProperty = new JsonProperty
                        {
                            SchemaReference = propertySchema.ActualSchema
                        };
                    }
                }
                else
                    jsonProperty = Generate<JsonProperty>(propertyType, attributes, schemaResolver);

                var propertyName = JsonPathUtilities.GetPropertyName(property, Settings.DefaultPropertyNameHandling);
                if (parentSchema.Properties.ContainsKey(propertyName))
                    throw new InvalidOperationException("The JSON property '" + propertyName + "' is defined multiple times on type '" + parentType.FullName + "'.");

                parentSchema.Properties.Add(propertyName, jsonProperty);

                var requiredAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DataAnnotations.RequiredAttribute");
                var jsonPropertyAttribute = attributes.OfType<JsonPropertyAttribute>().SingleOrDefault();

                var hasJsonNetAttributeRequired = jsonPropertyAttribute != null && (
                    jsonPropertyAttribute.Required == Required.Always ||
                    jsonPropertyAttribute.Required == Required.AllowNull);

                var isDataContractMemberRequired = GetDataMemberAttribute(parentType, attributes)?.IsRequired == true;

                var hasRequiredAttribute = requiredAttribute != null;
                if (hasRequiredAttribute || isDataContractMemberRequired || hasJsonNetAttributeRequired)
                    parentSchema.RequiredProperties.Add(propertyName);

                var isJsonNetAttributeNullable = jsonPropertyAttribute != null && jsonPropertyAttribute.Required == Required.AllowNull;

                var isNullable = !hasRequiredAttribute && !isDataContractMemberRequired && (propertyTypeDescription.IsNullable || isJsonNetAttributeNullable);
                if (isNullable)
                {
                    if (Settings.NullHandling == NullHandling.JsonSchema)
                    {
                        if (requiresSchemaReference)
                            jsonProperty.OneOf.Add(new JsonSchema4 { Type = JsonObjectType.Null });
                        else if (jsonProperty.Type == JsonObjectType.None)
                        {
                            jsonProperty.OneOf.Add(new JsonSchema4 { Type = JsonObjectType.None });
                            jsonProperty.OneOf.Add(new JsonSchema4 { Type = JsonObjectType.Null });
                        }
                        else
                            jsonProperty.Type = jsonProperty.Type | JsonObjectType.Null;
                    }
                }
                else if (Settings.NullHandling == NullHandling.Swagger)
                {
                    if (!parentSchema.RequiredProperties.Contains(propertyName))
                        parentSchema.RequiredProperties.Add(propertyName);
                }

                dynamic readOnlyAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.ReadOnlyAttribute");
                if (readOnlyAttribute != null)
                    jsonProperty.IsReadOnly = readOnlyAttribute.IsReadOnly;

                jsonProperty.Description = GetDescription(property, attributes);

                ApplyPropertyAnnotations(jsonProperty, parentType, attributes, propertyTypeDescription);
            }
        }

        private bool RequiresSchemaReference(Type type, IEnumerable<Attribute> parentAttributes)
        {
            var typeDescription = JsonObjectTypeDescription.FromType(type, parentAttributes, Settings.DefaultEnumHandling);

            var typeMapper = Settings.TypeMappers.FirstOrDefault(m => m.MappedType == type);
            if (typeMapper != null)
                return typeMapper.UseReference;

            return !typeDescription.IsDictionary && (typeDescription.Type.HasFlag(JsonObjectType.Object) || typeDescription.IsEnum);
        }

        private static bool IsPropertyIgnored(Type parentType, Attribute[] propertyAttributes)
        {
            if (propertyAttributes.Any(a => a is JsonIgnoreAttribute))
                return true;

            if (HasDataContractAttribute(parentType) && GetDataMemberAttribute(parentType, propertyAttributes) == null && !propertyAttributes.Any(a => a is JsonPropertyAttribute))
                return true;

            return false;
        }

        private static dynamic GetDataMemberAttribute(Type parentType, Attribute[] propertyAttributes)
        {
            if (!HasDataContractAttribute(parentType))
                return null;

            return propertyAttributes.FirstOrDefault(a => a.GetType().Name == "DataMemberAttribute");
        }

        private static bool HasDataContractAttribute(Type parentType)
        {
            return parentType.GetTypeInfo().GetCustomAttributes().Any(a => a.GetType().Name == "DataContractAttribute");
        }

        /// <summary>Applies the property annotations to the JSON property.</summary>
        /// <param name="jsonProperty">The JSON property.</param>
        /// <param name="parentType">The type of the parent.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="propertyTypeDescription">The property type description.</param>
        public void ApplyPropertyAnnotations(JsonSchema4 jsonProperty, Type parentType, IList<Attribute> attributes,
            JsonObjectTypeDescription propertyTypeDescription)
        {
            // TODO: Refactor out

            dynamic displayAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DataAnnotations.DisplayAttribute");
            if (displayAttribute != null && displayAttribute.Name != null)
                jsonProperty.Title = displayAttribute.Name;

            dynamic defaultValueAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DefaultValueAttribute");
            if (defaultValueAttribute != null && defaultValueAttribute.Value != null)
                jsonProperty.Default = ConvertDefaultValue(parentType, attributes, defaultValueAttribute);

            dynamic regexAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DataAnnotations.RegularExpressionAttribute");
            if (regexAttribute != null)
                jsonProperty.Pattern = regexAttribute.Pattern;

            if (propertyTypeDescription.Type == JsonObjectType.Number ||
                propertyTypeDescription.Type == JsonObjectType.Integer)
            {
                dynamic rangeAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DataAnnotations.RangeAttribute");
                if (rangeAttribute != null)
                {
                    if (rangeAttribute.Minimum != null && rangeAttribute.Minimum > double.MinValue)
                        jsonProperty.Minimum = (decimal?)(double)rangeAttribute.Minimum;
                    if (rangeAttribute.Maximum != null && rangeAttribute.Maximum < double.MaxValue)
                        jsonProperty.Maximum = (decimal?)(double)rangeAttribute.Maximum;
                }

                var multipleOfAttribute = attributes.OfType<MultipleOfAttribute>().SingleOrDefault();
                if (multipleOfAttribute != null)
                    jsonProperty.MultipleOf = multipleOfAttribute.MultipleOf;
            }

            dynamic minLengthAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DataAnnotations.MinLengthAttribute");
            if (minLengthAttribute != null && minLengthAttribute.Length != null)
            {
                if (propertyTypeDescription.Type == JsonObjectType.String)
                    jsonProperty.MinLength = minLengthAttribute.Length;
                else if (propertyTypeDescription.Type == JsonObjectType.Array)
                    jsonProperty.MinItems = minLengthAttribute.Length;
            }

            dynamic maxLengthAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DataAnnotations.MaxLengthAttribute");
            if (maxLengthAttribute != null && maxLengthAttribute.Length != null)
            {
                if (propertyTypeDescription.Type == JsonObjectType.String)
                    jsonProperty.MaxLength = maxLengthAttribute.Length;
                else if (propertyTypeDescription.Type == JsonObjectType.Array)
                    jsonProperty.MaxItems = maxLengthAttribute.Length;
            }

            dynamic stringLengthAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DataAnnotations.StringLengthAttribute");
            if (stringLengthAttribute != null)
            {
                if (propertyTypeDescription.Type == JsonObjectType.String)
                {
                    jsonProperty.MinLength = stringLengthAttribute.MinimumLength;
                    jsonProperty.MaxLength = stringLengthAttribute.MaximumLength;
                }
            }

            dynamic dataTypeAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DataAnnotations.DataTypeAttribute");
            if (dataTypeAttribute != null)
            {
                var dataType = dataTypeAttribute.DataType.ToString();
                if (DataTypeFormats.ContainsKey(dataType))
                    jsonProperty.Format = DataTypeFormats[dataType];
            }
        }

        private object ConvertDefaultValue(Type parentType, IEnumerable<Attribute> propertyAttributes, dynamic defaultValueAttribute)
        {
            if (((Type)defaultValueAttribute.Value.GetType()).GetTypeInfo().IsEnum)
            {
                if (JsonObjectTypeDescription.IsStringEnum(parentType, propertyAttributes, Settings.DefaultEnumHandling))
                    return defaultValueAttribute.Value.ToString();
                else
                    return (int)defaultValueAttribute.Value;
            }
            else
                return defaultValueAttribute.Value;
        }

        private string GetDescription(MemberInfo memberInfo, IEnumerable<Attribute> attributes)
        {
            dynamic descriptionAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DescriptionAttribute", TypeNameStyle.FullName);
            if (descriptionAttribute != null && descriptionAttribute.Description != null)
                return descriptionAttribute.Description;
            else
            {
                dynamic displayAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DataAnnotations.DisplayAttribute", TypeNameStyle.FullName);
                if (displayAttribute != null && displayAttribute.Description != null)
                    return displayAttribute.Description;
                else
                {
                    var summary = memberInfo.GetXmlSummary();
                    if (summary != string.Empty)
                        return summary;
                }
            }

            return null;
        }
    }
}