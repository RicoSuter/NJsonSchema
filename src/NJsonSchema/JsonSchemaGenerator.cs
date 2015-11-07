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
using NJsonSchema.Infrastructure;

namespace NJsonSchema
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
        public JsonSchemaGeneratorSettings Settings { get; private set; }

        /// <summary>Generates a <see cref="JsonSchema4" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="type">The type.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        /// <exception cref="InvalidOperationException">Could not find item type of enumeration type.</exception>
        public JsonSchema4 Generate(Type type, ISchemaResolver schemaResolver)
        {
            return Generate<JsonSchema4>(type, schemaResolver);
        }

        /// <summary>Generates a <see cref="JsonSchema4" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <typeparam name="TSchemaType">The type of the schema type.</typeparam>
        /// <param name="type">The type.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        /// <exception cref="InvalidOperationException">Could not find item type of enumeration type.</exception>
        public TSchemaType Generate<TSchemaType>(Type type, ISchemaResolver schemaResolver)
            where TSchemaType : JsonSchema4, new()
        {
            var schema = new TSchemaType();

            var typeDescription = JsonObjectTypeDescription.FromType(type);
            schema.Type = typeDescription.Type;
            schema.Format = typeDescription.Format;

            if (schema.Type.HasFlag(JsonObjectType.Object))
            {
                if (typeDescription.IsDictionary)
                    GenerateDictionary(type, schema, schemaResolver);
                else
                {
                    if (type == typeof (object))
                    {
                        return new TSchemaType
                        {
                            Type = JsonObjectType.Object,
                            AllowAdditionalProperties = false
                        };
                    }

                    schema.TypeName = GetTypeName(type);

                    if (schemaResolver.HasSchema(type))
                    {
                        schema.SchemaReference = schemaResolver.GetSchema(type);
                        return schema;
                    }

                    schema.Description = GetDescription(type.GetTypeInfo(), type.GetTypeInfo().GetCustomAttributes());
                    
                    GenerateObject(type, schema, schemaResolver);
                    GenerateInheritance(type, schema, schemaResolver);
                }
            }
            else if (schema.Type.HasFlag(JsonObjectType.Array))
            {
                schema.Type = JsonObjectType.Array;

                var itemType = type.GenericTypeArguments.Length == 0 ? type.GetElementType() : type.GenericTypeArguments[0];
                if (itemType == null)
                    throw new InvalidOperationException("Could not find item type of enumeration type '" + type.FullName + "'.");

                schema.Item = Generate<JsonSchema4>(itemType, schemaResolver);
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                if (schemaResolver.HasSchema(type))
                {
                    schema.SchemaReference = schemaResolver.GetSchema(type);
                    return schema;
                }

                dynamic jsonConverterAttribute = type.GetTypeInfo().GetCustomAttributes()
                        .SingleOrDefault(a => a.GetType().Name == "JsonConverterAttribute");

                if (jsonConverterAttribute != null)
                {
                    var converterType = (Type)jsonConverterAttribute.ConverterType;
                    if (converterType.Name == "StringEnumConverter")
                        LoadEnumerations(type, schema);
                    else if (Settings.DefaultEnumHandling == EnumHandling.String)
                        LoadEnumerations(type, schema);
                }
                else if (Settings.DefaultEnumHandling == EnumHandling.String)
                    LoadEnumerations(type, schema);

                schema.TypeName = type.Name; 
                schemaResolver.AddSchema(type, schema);
            }

            return schema;
        }

        private static string GetTypeName(Type type)
        {
            if (type.IsConstructedGenericType)
                return type.Name.Split('`').First() + type.GenericTypeArguments[0].Name;

            return type.Name;
        }

        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        private void GenerateDictionary<TSchemaType>(Type type, TSchemaType schema, ISchemaResolver schemaResolver)
            where TSchemaType : JsonSchema4, new()
        {
            if (type.GenericTypeArguments.Length != 2)
                throw new InvalidOperationException("Could not find value type of dictionary type '" + type.FullName + "'.");

            var valueType = type.GenericTypeArguments[1];

            schema.AdditionalPropertiesSchema = Generate<JsonProperty>(valueType, schemaResolver);
            schema.AllowAdditionalProperties = true;
        }

        /// <summary>Generates the properties for the given type and schema.</summary>
        /// <typeparam name="TSchemaType">The type of the schema type.</typeparam>
        /// <param name="type">The types.</param>
        /// <param name="schema">The properties</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        protected virtual void GenerateObject<TSchemaType>(Type type, TSchemaType schema, ISchemaResolver schemaResolver)
            where TSchemaType : JsonSchema4, new()
        {
            schemaResolver.AddSchema(type, schema);
            schema.AllowAdditionalProperties = false;

            var properties = GetTypeProperties(type);
            foreach (var property in type.GetTypeInfo().DeclaredProperties
                .Where(p => properties == null || properties.Contains(p.Name)))
            {
                LoadProperty(property, schema, schemaResolver);
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
        
        private void LoadEnumerations<TSchemaType>(Type type, TSchemaType schema) where TSchemaType : JsonSchema4, new()
        {
            schema.Type = JsonObjectType.String;
            foreach (var enumValue in Enum.GetNames(type))
                schema.Enumeration.Add(enumValue);
        }

        private void GenerateInheritance(Type type, JsonSchema4 schema, ISchemaResolver schemaResolver)
        {
            var baseType = type.GetTypeInfo().BaseType;
            if (baseType != null && baseType != typeof(object))
            {
                var baseSchema = Generate<JsonProperty>(baseType, schemaResolver);
                schema.AllOf.Add(baseSchema);
            }
        }

        private void LoadProperty<TSchemaType>(PropertyInfo property, TSchemaType parentSchema, ISchemaResolver schemaResolver)
            where TSchemaType : JsonSchema4, new()
        {
            var propertyType = property.PropertyType;
            var propertyTypeDescription = JsonObjectTypeDescription.FromType(propertyType);

            var attributes = property.GetCustomAttributes().ToArray();

            var jsonProperty = Generate<JsonProperty>(propertyType, schemaResolver);
            var propertyName = JsonPathUtilities.GetPropertyName(property);
            parentSchema.Properties.Add(propertyName, jsonProperty);

            var requiredAttribute = TryGetAttribute(attributes, "System.ComponentModel.DataAnnotations.RequiredAttribute");
            if (propertyTypeDescription.IsAlwaysRequired || requiredAttribute != null)
                parentSchema.RequiredProperties.Add(property.Name);

            jsonProperty.Description = GetDescription(property, attributes);

            dynamic regexAttribute = TryGetAttribute(attributes, "System.ComponentModel.DataAnnotations.RegularExpressionAttribute");
            if (regexAttribute != null)
                jsonProperty.Pattern = regexAttribute.Pattern;

            dynamic rangeAttribute = TryGetAttribute(attributes, "System.ComponentModel.DataAnnotations.RangeAttribute");
            if (rangeAttribute != null)
            {
                if (rangeAttribute.Minimum != null)
                    jsonProperty.Minimum = rangeAttribute.Minimum;
                if (rangeAttribute.Maximum != null)
                    jsonProperty.Maximum = rangeAttribute.Maximum;
            }
        }

        private string GetDescription(MemberInfo memberInfo, IEnumerable<Attribute> attributes)
        {
            dynamic descriptionAttribute = TryGetAttribute(attributes, "System.ComponentModel.DescriptionAttribute");
            if (descriptionAttribute != null)
                return descriptionAttribute.Description;
            else
            {
                var summary = memberInfo.GetXmlDocumentation();
                if (summary != string.Empty)
                    return summary;
            }

            return null; 
        }

        private Attribute TryGetAttribute(IEnumerable<Attribute> attributes, string attributeType)
        {
            return attributes.FirstOrDefault(a => a.GetType().FullName == attributeType);
        }
    }
}
