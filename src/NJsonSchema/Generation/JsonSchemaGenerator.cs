//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Annotations;
using NJsonSchema.Converters;
using NJsonSchema.Generation.TypeMappers;
using NJsonSchema.Infrastructure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NJsonSchema.Generation
{
    /// <summary>Generates a <see cref="JsonSchema"/> object for a given type. </summary>
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

        /// <summary>Generates a <see cref="JsonSchema" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        public JsonSchema Generate(Type type)
        {
            var schema = new JsonSchema();
            var schemaResolver = new JsonSchemaResolver(schema, Settings);
            Generate(schema, type.ToContextualType(), schemaResolver);
            return schema;
        }

        /// <summary>Generates a <see cref="JsonSchema" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="type">The type.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        public JsonSchema Generate(Type type, JsonSchemaResolver schemaResolver)
        {
            return Generate<JsonSchema>(type, schemaResolver);
        }

        /// <summary>Generates a <see cref="JsonSchema" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="type">The type.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        public TSchemaType Generate<TSchemaType>(Type type, JsonSchemaResolver schemaResolver)
            where TSchemaType : JsonSchema, new()
        {
            return Generate<TSchemaType>(type.ToContextualType(), schemaResolver);
        }

        /// <summary>Generates a <see cref="JsonSchema" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="contextualType">The type.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        public JsonSchema Generate(ContextualType contextualType, JsonSchemaResolver schemaResolver)
        {
            return Generate<JsonSchema>(contextualType, schemaResolver);
        }

        /// <summary>Generates a <see cref="JsonSchema" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <param name="contextualType">The type.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        public TSchemaType Generate<TSchemaType>(ContextualType contextualType, JsonSchemaResolver schemaResolver)
            where TSchemaType : JsonSchema, new()
        {
            var schema = new TSchemaType();
            Generate(schema, contextualType, schemaResolver);
            return schema;
        }

        /// <summary>Generates into the given <see cref="JsonSchema" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <typeparam name="TSchemaType">The type of the schema.</typeparam>
        /// <param name="schema">The schema.</param>
        /// <param name="type">The type.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        public void Generate<TSchemaType>(TSchemaType schema, Type type, JsonSchemaResolver schemaResolver)
            where TSchemaType : JsonSchema, new()
        {
            // This overload should not be used in this library directly
            Generate(schema, type.ToContextualType(), schemaResolver);
        }

        /// <summary>Generates into the given <see cref="JsonSchema" /> object for the given type and adds the mapping to the given resolver.</summary>
        /// <typeparam name="TSchemaType">The type of the schema.</typeparam>
        /// <param name="schema">The schema.</param>
        /// <param name="contextualType">The type.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns>The schema.</returns>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        public virtual void Generate<TSchemaType>(TSchemaType schema, ContextualType contextualType, JsonSchemaResolver schemaResolver)
            where TSchemaType : JsonSchema, new()
        {
            var type = contextualType.OriginalType;

            var jsonSchemaTypeAttribute = contextualType.GetAttribute<JsonSchemaTypeAttribute>();
            if (jsonSchemaTypeAttribute != null)
            {
                type = jsonSchemaTypeAttribute.Type;
            }

            ApplyExtensionDataAttributes(schema, contextualType);

            if (TryHandleSpecialTypes(schema, contextualType, schemaResolver))
            {
                ApplySchemaProcessors(schema, contextualType, schemaResolver);
                return;
            }

            if (schemaResolver.RootObject == schema)
            {
                schema.Title = Settings.SchemaNameGenerator.Generate(type);
            }

            var typeDescription = Settings.ReflectionService.GetDescription(contextualType, Settings);
            if (typeDescription.Type.HasFlag(JsonObjectType.Object))
            {
                if (typeDescription.IsDictionary)
                {
                    GenerateDictionary(schema, contextualType, typeDescription, schemaResolver);
                }
                else
                {
                    if (schemaResolver.HasSchema(type, false))
                    {
                        schema.Reference = schemaResolver.GetSchema(type, false);
                    }
                    else if (schema.GetType() == typeof(JsonSchema))
                    {
                        GenerateObject(schema, type, typeDescription, schemaResolver);
                    }
                    else
                    {
                        schema.Reference = Generate(contextualType, schemaResolver);
                    }
                }
            }
            else if (typeDescription.IsEnum)
            {
                GenerateEnum(schema, contextualType, typeDescription, schemaResolver);
            }
            else if (typeDescription.Type.HasFlag(JsonObjectType.Array)) // TODO: Add support for tuples?
            {
                GenerateArray(schema, contextualType, typeDescription, schemaResolver);
            }
            else
            {
                typeDescription.ApplyType(schema);
            }

            ApplySchemaProcessors(schema, contextualType, schemaResolver);
        }

        /// <summary>Generetes a schema directly or referenced for the requested schema type; 
        /// does NOT change nullability.</summary>
        /// <typeparam name="TSchemaType">The resulted schema type which may reference the actual schema.</typeparam>
        /// <param name="contextualType">The type of the schema to generate.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <param name="transformation">An action to transform the resulting schema (e.g. property or parameter) before the type of reference is determined (with $ref or allOf/oneOf).</param>
        /// <returns>The requested schema object.</returns>
        public TSchemaType GenerateWithReference<TSchemaType>(
            ContextualType contextualType,
            JsonSchemaResolver schemaResolver,
            Action<TSchemaType, JsonSchema> transformation = null)
            where TSchemaType : JsonSchema, new()
        {
            return GenerateWithReferenceAndNullability(contextualType, false, schemaResolver, transformation);
        }

        /// <summary>Generetes a schema directly or referenced for the requested schema type; 
        /// also adds nullability if required by looking at the type's <see cref="JsonTypeDescription" />.</summary>
        /// <typeparam name="TSchemaType">The resulted schema type which may reference the actual schema.</typeparam>
        /// <param name="contextualType">The type of the schema to generate.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <param name="transformation">An action to transform the resulting schema (e.g. property or parameter) before the type of reference is determined (with $ref or allOf/oneOf).</param>
        /// <returns>The requested schema object.</returns>
        public TSchemaType GenerateWithReferenceAndNullability<TSchemaType>(
            ContextualType contextualType, JsonSchemaResolver schemaResolver,
            Action<TSchemaType, JsonSchema> transformation = null)
            where TSchemaType : JsonSchema, new()
        {
            var typeDescription = Settings.ReflectionService.GetDescription(contextualType, Settings);
            return GenerateWithReferenceAndNullability(contextualType, typeDescription.IsNullable, schemaResolver, transformation);
        }

        /// <summary>Generetes a schema directly or referenced for the requested schema type; also adds nullability if required.</summary>
        /// <typeparam name="TSchemaType">The resulted schema type which may reference the actual schema.</typeparam>
        /// <param name="contextualType">The type of the schema to generate.</param>
        /// <param name="isNullable">Specifies whether the property, parameter or requested schema type is nullable.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <param name="transformation">An action to transform the resulting schema (e.g. property or parameter) before the type of reference is determined (with $ref or allOf/oneOf).</param>
        /// <returns>The requested schema object.</returns>
        public virtual TSchemaType GenerateWithReferenceAndNullability<TSchemaType>(
            ContextualType contextualType, bool isNullable, JsonSchemaResolver schemaResolver,
            Action<TSchemaType, JsonSchema> transformation = null)
            where TSchemaType : JsonSchema, new()
        {
            var typeDescription = Settings.ReflectionService.GetDescription(contextualType, Settings);
            var requiresSchemaReference = typeDescription.RequiresSchemaReference(Settings.TypeMappers);

            JsonSchema referencedSchema;
            if (!requiresSchemaReference)
            {
                var schema = Generate<TSchemaType>(contextualType, schemaResolver);
                if (!schema.HasReference)
                {
                    transformation?.Invoke(schema, schema);

                    if (isNullable)
                    {
                        if (Settings.SchemaType == SchemaType.JsonSchema)
                        {
                            if (schema.Type == JsonObjectType.None)
                            {
                                schema.OneOf.Add(new JsonSchema { Type = JsonObjectType.None });
                                schema.OneOf.Add(new JsonSchema { Type = JsonObjectType.Null });
                            }
                            else
                            {
                                schema.Type = schema.Type | JsonObjectType.Null;
                            }
                        }
                        else if (Settings.SchemaType == SchemaType.OpenApi3 || Settings.GenerateCustomNullableProperties)
                        {
                            schema.IsNullableRaw = isNullable;
                        }
                    }

                    return schema;
                }
                else // TODO: Is this else needed?
                {
                    referencedSchema = schema.ActualSchema;
                }
            }
            else
            {
                referencedSchema = Generate<JsonSchema>(contextualType, schemaResolver);
            }

            var referencingSchema = new TSchemaType();
            transformation?.Invoke(referencingSchema, referencedSchema);

            if (isNullable)
            {
                if (Settings.SchemaType == SchemaType.JsonSchema)
                {
                    referencingSchema.OneOf.Add(new JsonSchema { Type = JsonObjectType.Null });
                }
                else if (Settings.SchemaType == SchemaType.OpenApi3 || Settings.GenerateCustomNullableProperties)
                {
                    referencingSchema.IsNullableRaw = true;
                }
            }

            // See https://github.com/RSuter/NJsonSchema/issues/531
            var useDirectReference = Settings.AllowReferencesWithProperties ||
                !JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(referencingSchema)).Properties().Any(); // TODO: Improve performance

            if (useDirectReference && referencingSchema.OneOf.Count == 0)
            {
                referencingSchema.Reference = referencedSchema.ActualSchema;
            }
            else if (Settings.SchemaType != SchemaType.Swagger2)
            {
                referencingSchema.OneOf.Add(new JsonSchema
                {
                    Reference = referencedSchema.ActualSchema
                });
            }
            else
            {
                referencingSchema.AllOf.Add(new JsonSchema
                {
                    Reference = referencedSchema.ActualSchema
                });
            }

            return referencingSchema;
        }

        /// <summary>Gets the converted property name.</summary>
        /// <param name="jsonProperty">The property.</param>
        /// <param name="contextualMember">The contextual member info.</param>
        /// <returns>The property name.</returns>
        public virtual string GetPropertyName(JsonProperty jsonProperty, ContextualMemberInfo contextualMember)
        {
            try
            {
                var propertyName = contextualMember?.MemberInfo != null ?
                    contextualMember.MemberInfo.DeclaringType.GetContextualPropertiesAndFields().First(p => p.Name == contextualMember.Name).GetName() :
                    jsonProperty.PropertyName;

                var contractResolver = Settings.ActualContractResolver as DefaultContractResolver;
                return contractResolver != null
                    ? contractResolver.GetResolvedPropertyName(propertyName)
                    : propertyName;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Could not get JSON property name of property '" +
                    (contextualMember != null ? contextualMember.Name : "n/a") + "' and type '" +
                    (contextualMember?.MemberInfo?.DeclaringType != null ? contextualMember.MemberInfo.DeclaringType.FullName : "n/a") + "'.", e);
            }
        }

        /// <summary>Applies the property annotations to the JSON property.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="contextualType">The contextual type.</param>
        /// <param name="typeDescription">The property type description.</param>
        public virtual void ApplyDataAnnotations(JsonSchema schema, ContextualType contextualType, JsonTypeDescription typeDescription)
        {
            // TODO: Refactor out

            dynamic displayAttribute = contextualType.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.DisplayAttribute");
            if (displayAttribute != null && displayAttribute.Name != null)
            {
                schema.Title = displayAttribute.Name;
            }

            dynamic defaultValueAttribute = contextualType.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DefaultValueAttribute");
            if (defaultValueAttribute != null)
            {
                if (typeDescription.IsEnum &&
                    typeDescription.Type.HasFlag(JsonObjectType.String))
                {
                    schema.Default = defaultValueAttribute.Value?.ToString();
                }
                else
                {
                    schema.Default = defaultValueAttribute.Value;
                }
            }

            dynamic regexAttribute = contextualType.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.RegularExpressionAttribute");
            if (regexAttribute != null)
            {
                if (typeDescription.IsDictionary)
                {
                    schema.AdditionalPropertiesSchema.Pattern = regexAttribute.Pattern;
                }
                else
                {
                    schema.Pattern = regexAttribute.Pattern;
                }
            }

            if (typeDescription.Type == JsonObjectType.Number ||
                typeDescription.Type == JsonObjectType.Integer)
            {
                ApplyRangeAttribute(schema, contextualType.ContextAttributes);

                var multipleOfAttribute = contextualType.ContextAttributes.OfType<MultipleOfAttribute>().SingleOrDefault();
                if (multipleOfAttribute != null)
                {
                    schema.MultipleOf = multipleOfAttribute.MultipleOf;
                }
            }

            dynamic minLengthAttribute = contextualType.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.MinLengthAttribute");
            if (minLengthAttribute != null && minLengthAttribute.Length != null)
            {
                if (typeDescription.Type == JsonObjectType.String)
                {
                    schema.MinLength = minLengthAttribute.Length;
                }
                else if (typeDescription.Type == JsonObjectType.Array)
                {
                    schema.MinItems = minLengthAttribute.Length;
                }
            }

            dynamic maxLengthAttribute = contextualType.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.MaxLengthAttribute");
            if (maxLengthAttribute != null && maxLengthAttribute.Length != null)
            {
                if (typeDescription.Type == JsonObjectType.String)
                {
                    schema.MaxLength = maxLengthAttribute.Length;
                }
                else if (typeDescription.Type == JsonObjectType.Array)
                {
                    schema.MaxItems = maxLengthAttribute.Length;
                }
            }

            dynamic stringLengthAttribute = contextualType.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.StringLengthAttribute");
            if (stringLengthAttribute != null)
            {
                if (typeDescription.Type == JsonObjectType.String)
                {
                    schema.MinLength = stringLengthAttribute.MinimumLength;
                    schema.MaxLength = stringLengthAttribute.MaximumLength;
                }
            }

            dynamic dataTypeAttribute = contextualType.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.DataTypeAttribute");
            if (dataTypeAttribute != null)
            {
                var dataType = dataTypeAttribute.DataType.ToString();
                if (DataTypeFormats.ContainsKey(dataType))
                {
                    schema.Format = DataTypeFormats[dataType];
                }
            }
        }

        /// <summary>Gets the actual default value for the given object (e.g. correctly converts enums).</summary>
        /// <param name="type">The value type.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The converted default value.</returns>
        public virtual object ConvertDefaultValue(ContextualType type, object defaultValue)
        {
            if (defaultValue != null && defaultValue.GetType().GetTypeInfo().IsEnum)
            {
                var hasStringEnumConverter = Settings.ReflectionService.IsStringEnum(type, Settings.ActualSerializerSettings);
                if (hasStringEnumConverter)
                {
                    return defaultValue.ToString();
                }
                else
                {
                    return (int)defaultValue;
                }
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>Generates the properties for the given type and schema.</summary>
        /// <param name="schema">The properties</param>
        /// <param name="type">The type.</param>
        /// <param name="typeDescription">The type description.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns>The task.</returns>
        protected virtual void GenerateObject(JsonSchema schema, Type type, JsonTypeDescription typeDescription, JsonSchemaResolver schemaResolver)
        {
            schemaResolver.AddSchema(type, false, schema);

            var rootSchema = schema;
            var actualSchema = GenerateInheritance(type, schema, schemaResolver);
            if (actualSchema != null)
            {
                schema = actualSchema;
            }
            else
            {
                GenerateProperties(type, schema, schemaResolver);
                ApplyAdditionalProperties(schema, type, schemaResolver);
            }

            if (!schema.Type.HasFlag(JsonObjectType.Array))
            {
                typeDescription.ApplyType(schema);
            }

            schema.Description = type.ToCachedType().GetDescription();

            if (Settings.GetActualGenerateAbstractSchema(type))
            {
                schema.IsAbstract = type.GetTypeInfo().IsAbstract;
            }

            GenerateInheritanceDiscriminator(type, rootSchema, schema);
            GenerateKnownTypes(type, schemaResolver);

            if (Settings.GenerateXmlObjects)
            {
                schema.GenerateXmlObjectForType(type);
            }
        }

        /// <summary>Gets the properties of the given type or null to take all properties.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The property names or null for all.</returns>
        protected virtual string[] GetTypeProperties(Type type)
        {
            if (type == typeof(Exception))
            {
                return new[] { "InnerException", "Message", "Source", "StackTrace" };
            }

            return null;
        }

        /// <summary>Generates an array in the given schema.</summary>
        /// <typeparam name="TSchemaType">The schema type.</typeparam>
        /// <param name="schema">The schema.</param>
        /// <param name="contextualType">The contextual type.</param>
        /// <param name="typeDescription">The type description.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        protected virtual void GenerateArray<TSchemaType>(
            TSchemaType schema, ContextualType contextualType, JsonTypeDescription typeDescription, JsonSchemaResolver schemaResolver)
            where TSchemaType : JsonSchema, new()
        {
            typeDescription.ApplyType(schema);

            var jsonSchemaAttribute = contextualType.GetTypeAttribute<JsonSchemaAttribute>();
            var itemType = jsonSchemaAttribute?.ArrayItem ?? contextualType.OriginalType.GetEnumerableItemType();
            if (itemType != null)
            {
                var contextualItemType = itemType.ToContextualType();
                var itemIsNullable = contextualType.GetContextAttribute<ItemsCanBeNullAttribute>() != null ||
                                     contextualItemType.Nullability == Nullability.Nullable;

                schema.Item = GenerateWithReferenceAndNullability<JsonSchema>(
                    contextualItemType, itemIsNullable, schemaResolver, (itemSchema, typeSchema) =>
                    {
                        if (Settings.GenerateXmlObjects)
                        {
                            itemSchema.GenerateXmlObjectForItemType(contextualItemType);
                        }
                    });

                if (Settings.GenerateXmlObjects)
                {
                    schema.GenerateXmlObjectForArrayType();
                }
            }
            else
            {
                schema.Item = JsonSchema.CreateAnySchema();
            }
        }

        /// <summary>Generates an array in the given schema.</summary>
        /// <typeparam name="TSchemaType">The schema type.</typeparam>
        /// <param name="schema">The schema.</param>
        /// <param name="contextualType">The contextual type.</param>
        /// <param name="typeDescription">The type description.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        protected virtual void GenerateDictionary<TSchemaType>(TSchemaType schema, ContextualType contextualType, JsonTypeDescription typeDescription, JsonSchemaResolver schemaResolver)
            where TSchemaType : JsonSchema, new()
        {
            typeDescription.ApplyType(schema);
            var genericTypeArguments = contextualType.GenericArguments;

            var keyType = genericTypeArguments.Length == 2 ? genericTypeArguments[0] : typeof(string).ToContextualType();
            if (keyType.OriginalType.GetTypeInfo().IsEnum)
            {
                schema.DictionaryKey = GenerateWithReference<JsonSchema>(
                    keyType, schemaResolver);
            }

            var valueType = genericTypeArguments.Length == 2 ? genericTypeArguments[1] : typeof(object).ToContextualType();
            if (valueType.OriginalType == typeof(object))
            {
                schema.AdditionalPropertiesSchema = JsonSchema.CreateAnySchema();
            }
            else
            {
                var valueIsNullable = valueType.GetContextAttribute<ItemsCanBeNullAttribute>() != null ||
                    valueType.OriginalType.Name == "Nullable`1";

                schema.AdditionalPropertiesSchema = GenerateWithReferenceAndNullability<JsonSchema>(
                    valueType, valueIsNullable, schemaResolver/*, (s, r) =>
                    {
                        // TODO: Generate xml for key
                        if (Settings.GenerateXmlObjects)
                        {
                            s.GenerateXmlObjectForItemType(keyType);
                        }
                    }*/);
            }

            schema.AllowAdditionalProperties = true;
        }

        /// <summary>Generates an enumeration in the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="type">The enum type.</param>
        /// <param name="typeDescription">The type description.</param>
        protected virtual void GenerateEnum(JsonSchema schema, CachedType type, JsonTypeDescription typeDescription)
        {
            schema.Type = typeDescription.Type;
            schema.Enumeration.Clear();
            schema.EnumerationNames.Clear();
            schema.IsFlagEnumerable = type.GetTypeAttribute<FlagsAttribute>() != null;

            var underlyingType = Enum.GetUnderlyingType(type);

            var converters = Settings.ActualSerializerSettings.Converters.ToList();
            if (!converters.OfType<StringEnumConverter>().Any())
            {
                converters.Add(new StringEnumConverter());
            }

            foreach (var enumName in Enum.GetNames(type))
            {
                if (typeDescription.Type == JsonObjectType.Integer)
                {
                    var value = Convert.ChangeType(Enum.Parse(type, enumName), underlyingType);
                    schema.Enumeration.Add(value);
                }
                else
                {
                    // EnumMember only checked if StringEnumConverter is used
                    var attributes = type.TypeInfo.GetDeclaredField(enumName).GetCustomAttributes();
                    dynamic enumMemberAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.Runtime.Serialization.EnumMemberAttribute");
                    if (enumMemberAttribute != null && !string.IsNullOrEmpty(enumMemberAttribute.Value))
                    {
                        schema.Enumeration.Add((string)enumMemberAttribute.Value);
                    }
                    else
                    {
                        var value = Enum.Parse(type, enumName);
                        var json = JsonConvert.SerializeObject(value, Formatting.None, converters.ToArray());
                        schema.Enumeration.Add(JsonConvert.DeserializeObject<string>(json));
                    }
                }

                schema.EnumerationNames.Add(enumName);
            }

            if (typeDescription.Type == JsonObjectType.Integer && Settings.GenerateEnumMappingDescription)
            {
                schema.Description = (schema.Description + "\n\n" +
                    string.Join("\n", schema.Enumeration.Select((e, i) => e + " = " + schema.EnumerationNames[i]))).Trim();
            }
        }

        private void ApplyAdditionalProperties<TSchemaType>(TSchemaType schema, Type type, JsonSchemaResolver schemaResolver)
            where TSchemaType : JsonSchema, new()
        {
            var extensionDataProperty = type.GetContextualProperties()
                .FirstOrDefault(p => p.GetContextAttribute<JsonExtensionDataAttribute>() != null);

            if (extensionDataProperty != null)
            {
                var genericTypeArguments = extensionDataProperty.GenericArguments;
                var extensionDataPropertyType = genericTypeArguments.Length == 2 ? genericTypeArguments[1] : typeof(object).ToContextualType();

                schema.AdditionalPropertiesSchema = GenerateWithReferenceAndNullability<JsonSchema>(
                    extensionDataPropertyType, schemaResolver);
            }
            else
            {
                schema.AllowAdditionalProperties = false;
            }
        }

        private void ApplySchemaProcessors(JsonSchema schema, ContextualType contextualType, JsonSchemaResolver schemaResolver)
        {
            var context = new SchemaProcessorContext(contextualType.OriginalType, schema, schemaResolver, this, Settings);
            foreach (var processor in Settings.SchemaProcessors)
            {
                processor.Process(context);
            }

            var operationProcessorAttributes = contextualType
                .TypeAttributes
                .GetAssignableToTypeName(nameof(JsonSchemaProcessorAttribute), TypeNameStyle.Name);

            foreach (dynamic attribute in operationProcessorAttributes)
            {
                var processor = Activator.CreateInstance(attribute.Type, attribute.Parameters);
                processor.Process(context);
            }
        }

        private void ApplyExtensionDataAttributes<TSchemaType>(TSchemaType schema, ContextualType contextualType)
            where TSchemaType : JsonSchema, new()
        {
            // class
            var extensionDataAttributes = contextualType.GetAttributes<JsonSchemaExtensionDataAttribute>().ToArray();
            if (extensionDataAttributes.Any())
            {
                schema.ExtensionData = extensionDataAttributes.ToDictionary(a => a.Key, a => a.Value);
            }
            else
            {
                // property or parameter
                extensionDataAttributes = contextualType.GetAttributes<JsonSchemaExtensionDataAttribute>().ToArray();
                if (extensionDataAttributes.Any())
                {
                    schema.ExtensionData = extensionDataAttributes.ToDictionary(a => a.Key, a => a.Value);
                }
            }
        }

        private bool TryHandleSpecialTypes<TSchemaType>(TSchemaType schema, ContextualType contextualType, JsonSchemaResolver schemaResolver)
            where TSchemaType : JsonSchema, new()
        {
            var typeMapper = Settings.TypeMappers.FirstOrDefault(m => m.MappedType == contextualType.OriginalType);
            if (typeMapper == null && contextualType.OriginalType.GetTypeInfo().IsGenericType)
            {
                var genericType = contextualType.OriginalType.GetGenericTypeDefinition();
                typeMapper = Settings.TypeMappers.FirstOrDefault(m => m.MappedType == genericType);
            }

            if (typeMapper != null)
            {
                var context = new TypeMapperContext(contextualType.OriginalType, this, schemaResolver, contextualType.ContextAttributes);
                typeMapper.GenerateSchema(schema, context);
                return true;
            }

            if (contextualType.OriginalType.IsAssignableToTypeName(nameof(JArray), TypeNameStyle.Name) == false &&
                (contextualType.OriginalType.IsAssignableToTypeName(nameof(JToken), TypeNameStyle.Name) == true ||
                 contextualType.OriginalType == typeof(object)))
            {
                return true;
            }

            return false;
        }

        private void GenerateEnum<TSchemaType>(
            TSchemaType schema, CachedType contextualType, JsonTypeDescription typeDescription, JsonSchemaResolver schemaResolver)
            where TSchemaType : JsonSchema, new()
        {
            var type = contextualType.Type;

            var isIntegerEnumeration = typeDescription.Type == JsonObjectType.Integer;
            if (schemaResolver.HasSchema(type, isIntegerEnumeration))
            {
                schema.Reference = schemaResolver.GetSchema(type, isIntegerEnumeration);
            }
            else if (schema.GetType() == typeof(JsonSchema))
            {
                typeDescription.ApplyType(schema);
                schema.Description = type.GetXmlDocsSummary();

                GenerateEnum(schema, type.ToCachedType(), typeDescription);

                schemaResolver.AddSchema(type, isIntegerEnumeration, schema);
            }
            else
            {
                schema.Reference = Generate(contextualType, schemaResolver);
            }
        }

        private void GenerateProperties(Type type, JsonSchema schema, JsonSchemaResolver schemaResolver)
        {
#if !LEGACY
            var members = type.GetTypeInfo()
                .DeclaredFields
                .Where(f => f.IsPublic && !f.IsStatic)
                .OfType<MemberInfo>()
                .Concat(
                    type.GetTypeInfo().DeclaredProperties
                    .Where(p => (p.GetMethod?.IsPublic == true && p.GetMethod?.IsStatic == false) ||
                                (p.SetMethod?.IsPublic == true && p.SetMethod?.IsStatic == false))
                )
                .ToList();
#else
            var members = type.GetTypeInfo()
                .GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.IsPublic && !f.IsStatic)
                .OfType<MemberInfo>()
                .Concat(
                    type.GetTypeInfo()
                    .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => (p.GetGetMethod()?.IsPublic == true && p.GetGetMethod()?.IsStatic == false) ||
                                (p.GetSetMethod()?.IsPublic == true && p.GetSetMethod()?.IsStatic == false))
                )
                .ToList();
#endif

            var contextualMembers = members.Select(m => m.ToContextualMember());
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
                        var memberInfo = contextualMembers.FirstOrDefault(p => p.Name == jsonProperty.UnderlyingName);
                        var propertyInfo = memberInfo as ContextualPropertyInfo;

                        if (Settings.GenerateAbstractProperties || propertyInfo == null || propertyInfo.PropertyInfo.DeclaringType.GetTypeInfo().IsInterface ||
#if !LEGACY
                            (propertyInfo.PropertyInfo.GetMethod?.IsAbstract != true && propertyInfo.PropertyInfo.SetMethod?.IsAbstract != true))
#else
                            (propertyInfo.PropertyInfo.GetGetMethod()?.IsAbstract != true && propertyInfo.PropertyInfo.GetSetMethod()?.IsAbstract != true))
#endif
                        {
                            LoadPropertyOrField(jsonProperty, memberInfo, type, schema, schemaResolver);
                        }
                    }
                }
            }
            else
            {
                // TODO: Remove this hacky code (used to support serialization of exceptions and restore the old behavior [pre 9.x])
                foreach (var memberInfo in contextualMembers.Where(m => allowedProperties == null || allowedProperties.Contains(m.Name)))
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

        private void GenerateKnownTypes(Type type, JsonSchemaResolver schemaResolver)
        {
            var attributes = type.GetTypeInfo()
                .GetCustomAttributes(Settings.GetActualFlattenInheritanceHierarchy(type));

            if (Settings.GenerateKnownTypes)
            {
                var knownTypeAttributes = attributes
                   // Known types of inherited classes will be generated later (in GenerateInheritance)
                   .GetAssignableToTypeName("KnownTypeAttribute", TypeNameStyle.Name)
                   .OfType<Attribute>();

                foreach (dynamic attribute in knownTypeAttributes)
                {
                    if (attribute.Type != null)
                    {
                        AddKnownType(attribute.Type, schemaResolver);
                    }
                    else if (attribute.MethodName != null)
                    {
                        var methodInfo = type.GetRuntimeMethod((string)attribute.MethodName, new Type[0]);
                        if (methodInfo != null)
                        {
                            var knownTypes = methodInfo.Invoke(null, null) as IEnumerable<Type>;
                            if (knownTypes != null)
                            {
                                foreach (var knownType in knownTypes)
                                {
                                    AddKnownType(knownType, schemaResolver);
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"A KnownType attribute on {type.FullName} does not specify a type or a method name.", nameof(type));
                    }
                }
            }

            foreach (var jsonConverterAttribute in attributes
                .GetAssignableToTypeName("JsonInheritanceAttribute", TypeNameStyle.Name))
            {
                var knownType = ObjectExtensions.TryGetPropertyValue<Type>(
                    jsonConverterAttribute, "Type", null);

                if (knownType != null)
                {
                    AddKnownType(knownType, schemaResolver);
                }
            }
        }

        private void AddKnownType(Type type, JsonSchemaResolver schemaResolver)
        {
            var typeDescription = Settings.ReflectionService.GetDescription(type.ToContextualType(), Settings);
            var isIntegerEnum = typeDescription.Type == JsonObjectType.Integer;

            if (!schemaResolver.HasSchema(type, isIntegerEnum))
            {
                Generate(type, schemaResolver);
            }
        }

        private JsonSchema GenerateInheritance(Type type, JsonSchema schema, JsonSchemaResolver schemaResolver)
        {
            var baseType = type.GetTypeInfo().BaseType;
            if (baseType != null && baseType != typeof(object) && baseType != typeof(ValueType))
            {
                if (baseType.GetTypeInfo().GetCustomAttributes(false).FirstAssignableToTypeNameOrDefault("JsonSchemaIgnoreAttribute", TypeNameStyle.Name) == null &&
                    baseType.GetTypeInfo().GetCustomAttributes(false).FirstAssignableToTypeNameOrDefault("SwaggerIgnoreAttribute", TypeNameStyle.Name) == null &&
                    Settings.ExcludedTypeNames?.Contains(baseType.FullName) != true)
                {
                    if (Settings.GetActualFlattenInheritanceHierarchy(type))
                    {
                        var typeDescription = Settings.ReflectionService.GetDescription(baseType.ToContextualType(), Settings);
                        if (!typeDescription.IsDictionary && !type.IsArray)
                        {
                            GenerateProperties(baseType, schema, schemaResolver);
                            var actualSchema = GenerateInheritance(baseType, schema, schemaResolver);

                            GenerateInheritanceDiscriminator(baseType, schema, actualSchema ?? schema);
                        }
                    }
                    else
                    {
                        var actualSchema = new JsonSchema();

                        GenerateProperties(type, actualSchema, schemaResolver);
                        ApplyAdditionalProperties(actualSchema, type, schemaResolver);

                        var baseTypeInfo = Settings.ReflectionService.GetDescription(baseType.ToContextualType(), Settings);
                        var requiresSchemaReference = baseTypeInfo.RequiresSchemaReference(Settings.TypeMappers);

                        if (actualSchema.Properties.Any() || requiresSchemaReference)
                        {
                            // Use allOf inheritance only if the schema is an object with properties 
                            // (not empty class which just inherits from array or dictionary)

                            var baseSchema = Generate(baseType, schemaResolver);
                            if (requiresSchemaReference)
                            {
                                if (schemaResolver.RootObject != baseSchema.ActualSchema)
                                {
                                    schemaResolver.AppendSchema(baseSchema.ActualSchema, Settings.SchemaNameGenerator.Generate(baseType));
                                }

                                schema.AllOf.Add(new JsonSchema
                                {
                                    Reference = baseSchema.ActualSchema
                                });
                            }
                            else
                            {
                                schema.AllOf.Add(baseSchema);
                            }

                            // First schema is the (referenced) base schema, second is the type schema itself
                            schema.AllOf.Add(actualSchema);
                            return actualSchema;
                        }
                        else
                        {
                            // Array and dictionary inheritance are not expressed with allOf but inline
                            Generate(schema, baseType.ToContextualType(), schemaResolver);
                            return schema;
                        }
                    }
                }
            }

            if (Settings.GetActualFlattenInheritanceHierarchy(type) && Settings.GenerateAbstractProperties)
            {
#if !LEGACY
                foreach (var i in type.GetTypeInfo().ImplementedInterfaces)
#else
                foreach (var i in type.GetTypeInfo().GetInterfaces())
#endif
                {
                    var typeDescription = Settings.ReflectionService.GetDescription(i.ToContextualType(), Settings);
                    if (!typeDescription.IsDictionary && !type.IsArray &&
                        !typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(i.GetTypeInfo()))
                    {
                        GenerateProperties(i, schema, schemaResolver);
                        var actualSchema = GenerateInheritance(i, schema, schemaResolver);

                        GenerateInheritanceDiscriminator(i, schema, actualSchema ?? schema);
                    }
                }
            }

            return null;
        }

        private void GenerateInheritanceDiscriminator(Type type, JsonSchema schema, JsonSchema typeSchema)
        {
            if (!Settings.GetActualFlattenInheritanceHierarchy(type))
            {
                var discriminatorConverter = TryGetInheritanceDiscriminatorConverter(type);
                if (discriminatorConverter != null)
                {
                    var discriminatorName = TryGetInheritanceDiscriminatorName(discriminatorConverter);

                    // Existing property can be discriminator only if it has String type  
                    if (typeSchema.Properties.TryGetValue(discriminatorName, out JsonSchemaProperty existingProperty) &&
                        (existingProperty.Type & JsonObjectType.String) == 0)
                    {
                        throw new InvalidOperationException("The JSON discriminator property '" + discriminatorName + "' must be a string property on type '" + type.FullName + "' (it is recommended to not implement the discriminator property at all).");
                    }

                    var discriminator = new OpenApiDiscriminator
                    {
                        JsonInheritanceConverter = discriminatorConverter,
                        PropertyName = discriminatorName
                    };

                    typeSchema.DiscriminatorObject = discriminator;
                    typeSchema.Properties[discriminatorName] = new JsonSchemaProperty
                    {
                        Type = JsonObjectType.String,
                        IsRequired = true
                    };
                }
                else
                {
                    var baseDiscriminator = schema.ResponsibleDiscriminatorObject ?? schema.ActualTypeSchema.ResponsibleDiscriminatorObject;
                    baseDiscriminator?.AddMapping(type, schema);
                }
            }
        }

        private object TryGetInheritanceDiscriminatorConverter(Type type)
        {
            var typeAttributes = type.GetTypeInfo().GetCustomAttributes(false).OfType<Attribute>();

            dynamic jsonConverterAttribute = typeAttributes.FirstAssignableToTypeNameOrDefault(nameof(JsonConverterAttribute), TypeNameStyle.Name);
            if (jsonConverterAttribute != null)
            {
                var converterType = (Type)jsonConverterAttribute.ConverterType;
                if (converterType.IsAssignableToTypeName(nameof(JsonInheritanceConverter), TypeNameStyle.Name))
                {
                    return jsonConverterAttribute.ConverterParameters != null && jsonConverterAttribute.ConverterParameters.Length > 0 ?
                        Activator.CreateInstance(jsonConverterAttribute.ConverterType, jsonConverterAttribute.ConverterParameters) :
                        Activator.CreateInstance(jsonConverterAttribute.ConverterType);
                }
            }

            return null;
        }

        private string TryGetInheritanceDiscriminatorName(dynamic jsonInheritanceConverter)
        {
            if (ObjectExtensions.HasProperty(jsonInheritanceConverter, nameof(JsonInheritanceConverter.DiscriminatorName)))
            {
                return jsonInheritanceConverter.DiscriminatorName;
            }

            return JsonInheritanceConverter.DefaultDiscriminatorName;
        }

        private void LoadPropertyOrField(JsonProperty jsonProperty, ContextualMemberInfo memberInfo, Type parentType, JsonSchema parentSchema, JsonSchemaResolver schemaResolver)
        {
            var propertyTypeDescription = Settings.ReflectionService.GetDescription(memberInfo, Settings);
            if (jsonProperty.Ignored == false && IsPropertyIgnoredBySettings(memberInfo) == false)
            {
                var propertyName = GetPropertyName(jsonProperty, memberInfo);
                if (parentSchema.Properties.ContainsKey(propertyName))
                {
                    throw new InvalidOperationException("The JSON property '" + propertyName + "' is defined multiple times on type '" + parentType.FullName + "'.");
                }

                var requiredAttribute = memberInfo.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.RequiredAttribute");

                var hasJsonNetAttributeRequired = jsonProperty.Required == Required.Always || jsonProperty.Required == Required.AllowNull;
                var isDataContractMemberRequired = GetDataMemberAttribute(memberInfo, parentType)?.IsRequired == true;

                var hasRequiredAttribute = requiredAttribute != null;
                if (hasRequiredAttribute || isDataContractMemberRequired || hasJsonNetAttributeRequired)
                {
                    parentSchema.RequiredProperties.Add(propertyName);
                }

                var isNullable = propertyTypeDescription.IsNullable &&
                    hasRequiredAttribute == false &&
                    (bool)isDataContractMemberRequired == false &&
                    (jsonProperty.Required == Required.Default || jsonProperty.Required == Required.AllowNull);

                Action<JsonSchemaProperty, JsonSchema> TransformSchema = (propertySchema, typeSchema) =>
                {
                    if (Settings.GenerateXmlObjects)
                    {
                        propertySchema.GenerateXmlObjectForProperty(memberInfo, propertyName);
                    }

                    if (hasRequiredAttribute &&
                        propertyTypeDescription.Type == JsonObjectType.String &&
                        requiredAttribute.TryGetPropertyValue("AllowEmptyStrings", false) == false)
                    {
                        propertySchema.MinLength = 1;
                    }

                    if (!isNullable && Settings.SchemaType == SchemaType.Swagger2)
                    {
                        if (!parentSchema.RequiredProperties.Contains(propertyName))
                        {
                            parentSchema.RequiredProperties.Add(propertyName);
                        }
                    }

                    dynamic readOnlyAttribute = memberInfo.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.ReadOnlyAttribute");
                    if (readOnlyAttribute != null)
                    {
                        propertySchema.IsReadOnly = readOnlyAttribute.IsReadOnly;
                    }

                    if (propertySchema.Description == null)
                    {
                        propertySchema.Description = memberInfo.GetDescription();
                    }

                    propertySchema.Default = ConvertDefaultValue(memberInfo, jsonProperty.DefaultValue);

                    ApplyDataAnnotations(propertySchema, memberInfo, propertyTypeDescription);
                };

                var referencingProperty = GenerateWithReferenceAndNullability(
                    memberInfo, isNullable, schemaResolver, TransformSchema);

                parentSchema.Properties.Add(propertyName, referencingProperty);
            }
        }

        private bool IsPropertyIgnored(ContextualMemberInfo property, Type parentType)
        {
            if (property.GetContextAttribute<JsonIgnoreAttribute>() != null)
            {
                return true;
            }

            if (property.GetContextAttribute<JsonPropertyAttribute>() == null &&
                HasDataContractAttribute(parentType) &&
                GetDataMemberAttribute(property, parentType) == null)
            {
                return true;
            }

            return IsPropertyIgnoredBySettings(property);
        }

        private bool IsPropertyIgnoredBySettings(ContextualMemberInfo property)
        {
            if (Settings.IgnoreObsoleteProperties && property.GetContextAttribute<ObsoleteAttribute>() != null)
            {
                return true;
            }

            return false;
        }

        private dynamic GetDataMemberAttribute(ContextualMemberInfo property, Type parentType)
        {
            if (!HasDataContractAttribute(parentType))
            {
                return null;
            }

            return property.ContextAttributes.FirstAssignableToTypeNameOrDefault("DataMemberAttribute", TypeNameStyle.Name);
        }

        private bool HasDataContractAttribute(Type parentType)
        {
            return parentType.ToCachedType().TypeAttributes
                .FirstAssignableToTypeNameOrDefault("DataContractAttribute", TypeNameStyle.Name) != null;
        }

        private void ApplyRangeAttribute(JsonSchema schema, IEnumerable<Attribute> parentAttributes)
        {
            dynamic rangeAttribute = parentAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.RangeAttribute");
            if (rangeAttribute != null)
            {
                if (rangeAttribute.Minimum != null)
                {
                    if (rangeAttribute.OperandType == typeof(double))
                    {
                        var minimum = (double)Convert.ChangeType(rangeAttribute.Minimum, typeof(double));
                        if (minimum > double.MinValue)
                        {
                            schema.Minimum = (decimal)minimum;
                        }
                    }
                    else
                    {
                        var minimum = (decimal)Convert.ChangeType(rangeAttribute.Minimum, typeof(decimal));
                        if (minimum > decimal.MinValue)
                        {
                            schema.Minimum = minimum;
                        }
                    }
                }

                if (rangeAttribute.Maximum != null)
                {
                    if (rangeAttribute.OperandType == typeof(double))
                    {
                        var maximum = (double)Convert.ChangeType(rangeAttribute.Maximum, typeof(double));
                        if (maximum < double.MaxValue)
                        {
                            schema.Maximum = (decimal)maximum;
                        }
                    }
                    else
                    {
                        var maximum = (decimal)Convert.ChangeType(rangeAttribute.Maximum, typeof(decimal));
                        if (maximum < decimal.MaxValue)
                        {
                            schema.Maximum = maximum;
                        }
                    }
                }
            }
        }
    }
}
