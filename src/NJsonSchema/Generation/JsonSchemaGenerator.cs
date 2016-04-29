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
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Annotations;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Generation
{
    /// <summary>Generates a <see cref="JsonSchema4"/> object for a given type. </summary>
    public class JsonSchemaGenerator
    {
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
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        /// <exception cref="InvalidOperationException">Could not find item type of array type.</exception>
        public JsonSchema4 Generate(Type type, ISchemaResolver schemaResolver)
        {
            return Generate<JsonSchema4>(type, null, null, new JsonSchemaDefinitionAppender(), schemaResolver);
        }

        /// <summary>Generates a <see cref="JsonSchema4" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="type">The type.</param>
        /// <param name="rootSchema">The root schema.</param>
        /// <param name="parentAttributes">The parent property or parameter attributes.</param>
        /// <param name="schemaDefinitionAppender">The schema definition appender.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        /// <exception cref="InvalidOperationException">Could not find item type of array type.</exception>
        public JsonSchema4 Generate(Type type, JsonSchema4 rootSchema, IEnumerable<Attribute> parentAttributes, ISchemaDefinitionAppender schemaDefinitionAppender, ISchemaResolver schemaResolver)
        {
            return Generate<JsonSchema4>(type, rootSchema, parentAttributes, schemaDefinitionAppender, schemaResolver);
        }

        /// <summary>Generates a <see cref="JsonSchema4" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="type">The type.</param>
        /// <param name="rootSchema">The root schema.</param>
        /// <param name="parentAttributes">The parent property or parameter attributes.</param>
        /// <param name="schemaDefinitionAppender">The schema definition appender.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        /// <exception cref="InvalidOperationException">Could not find item type of array type.</exception>
        public TSchemaType Generate<TSchemaType>(Type type, JsonSchema4 rootSchema, IEnumerable<Attribute> parentAttributes, ISchemaDefinitionAppender schemaDefinitionAppender, ISchemaResolver schemaResolver)
            where TSchemaType : JsonSchema4, new()
        {
            var schema = HandleSpecialTypes<TSchemaType>(type);
            if (schema != null)
                return schema;

            schema = new TSchemaType();

            if (rootSchema == null)
                rootSchema = schema;

            var typeDescription = JsonObjectTypeDescription.FromType(type, parentAttributes, Settings.DefaultEnumHandling);
            typeDescription.ApplyType(schema);

            ApplyExtensionDataAttributes(schema, type, parentAttributes);

            if (schema.Type.HasFlag(JsonObjectType.Object))
            {
                if (typeDescription.IsDictionary)
                    GenerateDictionary(type, rootSchema, schema, schemaDefinitionAppender, schemaResolver);
                else
                {
                    schema.TypeName = GetTypeName(type);
                    if (schemaResolver.HasSchema(type, false))
                    {
                        schema.SchemaReference = schemaResolver.GetSchema(type, false);
                        return schema;
                    }

                    if (schema.GetType() == typeof(JsonSchema4))
                    {
                        schema.Description = GetDescription(type.GetTypeInfo(), type.GetTypeInfo().GetCustomAttributes());
                        GenerateObject(type, schema, rootSchema, schemaDefinitionAppender, schemaResolver);
                    }
                    else
                    {
                        schema.SchemaReference = Generate<JsonSchema4>(type, rootSchema, parentAttributes, schemaDefinitionAppender, schemaResolver);
                        return schema;
                    }
                }
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                var isIntegerEnumeration = typeDescription.Type == JsonObjectType.Integer;
                if (schemaResolver.HasSchema(type, isIntegerEnumeration))
                {
                    schema.Type = typeDescription.Type;
                    schema.SchemaReference = schemaResolver.GetSchema(type, isIntegerEnumeration);
                    return schema;
                }

                if (schema.GetType() == typeof(JsonSchema4))
                {
                    LoadEnumerations(type, schema, typeDescription);

                    schema.TypeName = GetTypeName(type);
                    schemaResolver.AddSchema(type, isIntegerEnumeration, schema);
                }
                else
                {
                    schema.SchemaReference = Generate<JsonSchema4>(type, rootSchema, parentAttributes, schemaDefinitionAppender, schemaResolver);
                    return schema;
                }
            }
            else if (schema.Type.HasFlag(JsonObjectType.Array))
            {
                schema.Type = JsonObjectType.Array;

                var genericTypeArguments = GetGenericTypeArguments(type);
                var itemType = genericTypeArguments.Length == 0 ? type.GetElementType() : genericTypeArguments[0];
                if (itemType == null)
                    throw new InvalidOperationException("Could not find item type of array type '" + type.FullName + "'.");

                schema.Item = Generate(itemType, rootSchema, null, schemaDefinitionAppender, schemaResolver);
            }

            return schema;
        }

        private static void ApplyExtensionDataAttributes<TSchemaType>(TSchemaType schema, Type type, IEnumerable<Attribute> parentAttributes)
            where TSchemaType : JsonSchema4, new()
        {
            if (parentAttributes == null)
            {
                // class
                var extensionDataAttributes = type.GetTypeInfo().GetCustomAttributes<JsonSchemaExtensionDataAttribute>();
                if (extensionDataAttributes.Any())
                    schema.ExtensionData = extensionDataAttributes.ToDictionary(a => a.Property, a => a.Value);
            }
            else
            {
                // property or parameter
                var extensionDataAttributes = parentAttributes.OfType<JsonSchemaExtensionDataAttribute>();
                if (extensionDataAttributes.Any())
                    schema.ExtensionData = extensionDataAttributes.ToDictionary(a => a.Property, a => a.Value);
            }
        }

        private TSchemaType HandleSpecialTypes<TSchemaType>(Type type)
            where TSchemaType : JsonSchema4, new()
        {
            if (type == typeof(object) || type == typeof(JObject))
            {
                return new TSchemaType
                {
                    Type = JsonObjectType.Object,
                    AllowAdditionalProperties = true
                };
            }

            return null;
        }

        private string GetTypeName(Type type)
        {
            if (type.IsConstructedGenericType)
                return type.Name.Split('`').First() + GetTypeName(type.GenericTypeArguments[0]);

            return type.Name;
        }

        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        private void GenerateDictionary<TSchemaType>(Type type, JsonSchema4 rootSchema, TSchemaType schema, ISchemaDefinitionAppender schemaDefinitionAppender, ISchemaResolver schemaResolver)
            where TSchemaType : JsonSchema4, new()
        {
            var genericTypeArguments = GetGenericTypeArguments(type);
            if (genericTypeArguments.Length != 2)
                throw new InvalidOperationException("Could not find value type of dictionary type '" + type.FullName + "'.");

            var valueType = genericTypeArguments[1];
            if (valueType == typeof(object))
            {
                schema.AdditionalPropertiesSchema = new JsonSchema4
                {
                    Type =
                        JsonObjectType.Null |
                        JsonObjectType.Object |
                        JsonObjectType.Array |
                        JsonObjectType.String |
                        JsonObjectType.Boolean |
                        JsonObjectType.Integer |
                        JsonObjectType.Number
                };
            }
            else
                schema.AdditionalPropertiesSchema = Generate(valueType, rootSchema, null, schemaDefinitionAppender,
                    schemaResolver);

            schema.AllowAdditionalProperties = true;
        }

        /// <summary>Gets the generic type arguments of a type.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The type arguments.</returns>
        public Type[] GetGenericTypeArguments(Type type)
        {
            var genericTypeArguments = type.GenericTypeArguments;
            while (type != null && type != typeof(object) && genericTypeArguments.Length == 0)
            {
                type = type.GetTypeInfo().BaseType;
                if (type != null)
                    genericTypeArguments = type.GenericTypeArguments;
            }
            return genericTypeArguments;
        }

        /// <summary>Generates the properties for the given type and schema.</summary>
        /// <typeparam name="TSchemaType">The type of the schema type.</typeparam>
        /// <param name="type">The types.</param>
        /// <param name="schema">The properties</param>
        /// <param name="rootSchema">The root schema.</param>
        /// <param name="schemaDefinitionAppender"></param>
        /// <param name="schemaResolver">The schema resolver.</param>
        protected virtual void GenerateObject<TSchemaType>(Type type, TSchemaType schema, JsonSchema4 rootSchema, ISchemaDefinitionAppender schemaDefinitionAppender, ISchemaResolver schemaResolver)
            where TSchemaType : JsonSchema4, new()
        {
            schemaResolver.AddSchema(type, false, schema);
            schema.AllowAdditionalProperties = false;

            GeneratePropertiesAndInheritance(type, schema, rootSchema, schemaDefinitionAppender, schemaResolver);
            if (Settings.GenerateKnownTypes)
                GenerateKnownTypes(type, rootSchema, schemaDefinitionAppender, schemaResolver);
        }

        private void GeneratePropertiesAndInheritance(Type type, JsonSchema4 schema, JsonSchema4 rootSchema, ISchemaDefinitionAppender schemaDefinitionAppender, ISchemaResolver schemaResolver)
        {
            var properties = GetTypeProperties(type);
            foreach (var property in type.GetTypeInfo().DeclaredProperties.Where(p => properties == null || properties.Contains(p.Name)))
                LoadProperty(type, property, schema, rootSchema, schemaDefinitionAppender, schemaResolver);

            GenerateInheritance(type, schema, rootSchema, schemaDefinitionAppender, schemaResolver);
        }

        private void GenerateKnownTypes(Type type, JsonSchema4 rootSchema, ISchemaDefinitionAppender schemaDefinitionAppender, ISchemaResolver schemaResolver)
        {
            foreach (var knownTypeAttribute in type.GetTypeInfo().GetCustomAttributes<KnownTypeAttribute>())
            {
                var typeDescription = JsonObjectTypeDescription.FromType(knownTypeAttribute.Type, null, Settings.DefaultEnumHandling);
                var isIntegerEnum = typeDescription.Type == JsonObjectType.Integer;

                if (!schemaResolver.HasSchema(knownTypeAttribute.Type, isIntegerEnum))
                {
                    var knownSchema = Generate(knownTypeAttribute.Type, rootSchema, null, schemaDefinitionAppender, schemaResolver);
                    schemaDefinitionAppender.Append(rootSchema, knownSchema.ActualSchema);
                }
            }
        }

        private void GenerateInheritance(Type type, JsonSchema4 schema, JsonSchema4 rootSchema, ISchemaDefinitionAppender schemaDefinitionAppender, ISchemaResolver schemaResolver)
        {
            var baseType = type.GetTypeInfo().BaseType;
            if (baseType != null && baseType != typeof(object))
            {
                if (Settings.FlattenInheritanceHierarchy)
                    GeneratePropertiesAndInheritance(baseType, schema, rootSchema, schemaDefinitionAppender, schemaResolver);
                else
                {
                    var baseSchema = Generate(baseType, rootSchema, null, schemaDefinitionAppender, schemaResolver);
                    schema.AllOf.Add(baseSchema);
                }
            }
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

            foreach (var enumName in GetEnumNames(type, typeDescription))
            {
                if (typeDescription.Type == JsonObjectType.Integer)
                {
                    var value = Convert.ChangeType(Enum.Parse(type, enumName), Enum.GetUnderlyingType(type));
                    schema.Enumeration.Add(value);
                }
                else
                    schema.Enumeration.Add(enumName);
                schema.EnumerationNames.Add(enumName);
            }
        }

        private string[] GetEnumNames(Type type, JsonObjectTypeDescription typeDescription)
        {
            if (typeDescription.Type == JsonObjectType.String)
            {
                return Enum.GetNames(type).Select(name =>
                {
                    var attributes = type.GetTypeInfo().GetDeclaredField(name).GetCustomAttributes();
                    dynamic enumMemberAttribute = TryGetAttribute(attributes, "System.Runtime.Serialization.EnumMemberAttribute");
                    if (enumMemberAttribute != null)
                        return (string)enumMemberAttribute.Value;
                    return name;
                }).ToArray();
            }

            return Enum.GetNames(type);
        }

        private void LoadProperty(Type parentType, PropertyInfo property, JsonSchema4 parentSchema, JsonSchema4 rootSchema, ISchemaDefinitionAppender schemaDefinitionAppender, ISchemaResolver schemaResolver)
        {
            var propertyType = property.PropertyType;
            var propertyTypeDescription = JsonObjectTypeDescription.FromType(propertyType, property.GetCustomAttributes(), Settings.DefaultEnumHandling);

            var attributes = property.GetCustomAttributes().ToArray();
            if (IsPropertyIgnored(parentType, attributes) == false)
            {
                if (propertyType.Name == "Nullable`1")
                    propertyType = propertyType.GenericTypeArguments[0];

                JsonProperty jsonProperty;
                if (!propertyTypeDescription.IsDictionary && (propertyTypeDescription.Type.HasFlag(JsonObjectType.Object) || propertyTypeDescription.IsEnum))
                {
                    var jsonPropertySchema = Generate<JsonSchema4>(propertyType, rootSchema, property.GetCustomAttributes(), schemaDefinitionAppender, schemaResolver);
                    if (jsonPropertySchema.ActualSchema.IsAnyType)
                        jsonProperty = JsonProperty.FromJsonSchema(string.Empty, jsonPropertySchema.ActualSchema);
                    else
                    {
                        jsonProperty = new JsonProperty();
                        jsonProperty.SchemaReference = jsonPropertySchema.ActualSchema;

                        // schema is automatically added to Definitions if it is missing in JsonPathUtilities.GetJsonPath()
                    }
                }
                else
                    jsonProperty = Generate<JsonProperty>(propertyType, rootSchema, property.GetCustomAttributes(), schemaDefinitionAppender, schemaResolver);

                propertyTypeDescription.ApplyType(jsonProperty);

                var propertyName = JsonPathUtilities.GetPropertyName(property);
                parentSchema.Properties.Add(propertyName, jsonProperty);

                var requiredAttribute = TryGetAttribute(attributes, "System.ComponentModel.DataAnnotations.RequiredAttribute");
                var jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyAttribute>();

                var hasJsonNetAttributeRequired = jsonPropertyAttribute != null && (
                    jsonPropertyAttribute.Required == Required.Always ||
                    jsonPropertyAttribute.Required == Required.AllowNull);

                var hasRequiredAttribute = requiredAttribute != null;
                if (hasRequiredAttribute || hasJsonNetAttributeRequired)
                    parentSchema.RequiredProperties.Add(propertyName);

                var isJsonNetAttributeNullable = jsonPropertyAttribute != null && jsonPropertyAttribute.Required == Required.AllowNull;

                var isNullable = propertyTypeDescription.IsAlwaysRequired == false;
                if (!hasRequiredAttribute && (isNullable || isJsonNetAttributeNullable))
                    jsonProperty.Type = jsonProperty.Type | JsonObjectType.Null;

                dynamic readOnlyAttribute = TryGetAttribute(attributes, "System.ComponentModel.ReadOnlyAttribute");
                if (readOnlyAttribute != null)
                    jsonProperty.IsReadOnly = readOnlyAttribute.IsReadOnly;

                jsonProperty.Description = GetDescription(property, attributes);

                ApplyPropertyAnnotations(jsonProperty, attributes, propertyTypeDescription);
            }
        }

        private static bool IsPropertyIgnored(Type parentType, Attribute[] propertyAttributes)
        {
            if (propertyAttributes.Any(a => a is JsonIgnoreAttribute))
                return true;

            var hasDataContractAttribute = parentType.GetTypeInfo().GetCustomAttribute<DataContractAttribute>() != null;
            if (hasDataContractAttribute && !propertyAttributes.Any(a => a is DataMemberAttribute) && !propertyAttributes.Any(a => a is JsonPropertyAttribute))
                return true;

            return false;
        }

        /// <summary></summary>
        /// <param name="jsonProperty"></param>
        /// <param name="attributes"></param>
        /// <param name="propertyTypeDescription"></param>
        public void ApplyPropertyAnnotations(JsonSchema4 jsonProperty, IEnumerable<Attribute> attributes, JsonObjectTypeDescription propertyTypeDescription)
        {
            dynamic displayAttribute = TryGetAttribute(attributes, "System.ComponentModel.DataAnnotations.DisplayAttribute");
            if (displayAttribute != null && displayAttribute.Name != null)
                jsonProperty.Title = displayAttribute.Name;

            dynamic defaultValueAttribute = TryGetAttribute(attributes, "System.ComponentModel.DefaultValueAttribute");
            if (defaultValueAttribute != null && defaultValueAttribute.Value != null)
                jsonProperty.Default = defaultValueAttribute.Value;

            dynamic regexAttribute = TryGetAttribute(attributes, "System.ComponentModel.DataAnnotations.RegularExpressionAttribute");
            if (regexAttribute != null)
                jsonProperty.Pattern = regexAttribute.Pattern;

            if (propertyTypeDescription.Type == JsonObjectType.Number || propertyTypeDescription.Type == JsonObjectType.Integer)
            {
                dynamic rangeAttribute = TryGetAttribute(attributes, "System.ComponentModel.DataAnnotations.RangeAttribute");
                if (rangeAttribute != null)
                {
                    if (rangeAttribute.Minimum != null)
                        jsonProperty.Minimum = rangeAttribute.Minimum;
                    if (rangeAttribute.Maximum != null)
                        jsonProperty.Maximum = rangeAttribute.Maximum;
                }

                var multipleOfAttribute = attributes.OfType<MultipleOfAttribute>().SingleOrDefault();
                if (multipleOfAttribute != null)
                    jsonProperty.MultipleOf = multipleOfAttribute.MultipleOf;
            }

            dynamic minLengthAttribute = TryGetAttribute(attributes, "System.ComponentModel.DataAnnotations.MinLengthAttribute");
            if (minLengthAttribute != null && minLengthAttribute.Length != null)
            {
                if (propertyTypeDescription.Type == JsonObjectType.String)
                    jsonProperty.MinLength = minLengthAttribute.Length;
                else if (propertyTypeDescription.Type == JsonObjectType.Array)
                    jsonProperty.MinItems = minLengthAttribute.Length;
            }

            dynamic maxLengthAttribute = TryGetAttribute(attributes, "System.ComponentModel.DataAnnotations.MaxLengthAttribute");
            if (maxLengthAttribute != null && maxLengthAttribute.Length != null)
            {
                if (propertyTypeDescription.Type == JsonObjectType.String)
                    jsonProperty.MaxLength = maxLengthAttribute.Length;
                else if (propertyTypeDescription.Type == JsonObjectType.Array)
                    jsonProperty.MaxItems = maxLengthAttribute.Length;
            }
        }

        private string GetDescription(MemberInfo memberInfo, IEnumerable<Attribute> attributes)
        {
            dynamic descriptionAttribute = TryGetAttribute(attributes, "System.ComponentModel.DescriptionAttribute");
            if (descriptionAttribute != null && descriptionAttribute.Description != null)
                return descriptionAttribute.Description;
            else
            {
                dynamic displayAttribute = TryGetAttribute(attributes, "System.ComponentModel.DataAnnotations.DisplayAttribute");
                if (displayAttribute != null && displayAttribute.Description != null)
                    return displayAttribute.Description;
                else
                {
                    var summary = memberInfo.GetXmlDocumentation();
                    if (summary != string.Empty)
                        return summary;
                }
            }

            return null;
        }

        private Attribute TryGetAttribute(IEnumerable<Attribute> attributes, string attributeType)
        {
            return attributes.FirstOrDefault(a => a.GetType().FullName == attributeType);
        }
    }
}
