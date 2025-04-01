//-----------------------------------------------------------------------
// <copyright file="JsonSchemaGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Annotations;
using NJsonSchema.Converters;
using NJsonSchema.Generation.TypeMappers;
using NJsonSchema.Infrastructure;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

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

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <typeparam name="TType">The type to create the schema for.</typeparam>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType<TType>(JsonSchemaGeneratorSettings settings)
        {
            var generator = new JsonSchemaGenerator(settings);
            return generator.Generate(typeof(TType));
        }

        /// <summary>Creates a <see cref="JsonSchema" /> from a given type.</summary>
        /// <param name="type">The type to create the schema for.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The <see cref="JsonSchema" />.</returns>
        public static JsonSchema FromType(Type type, JsonSchemaGeneratorSettings settings)
        {
            var generator = new JsonSchemaGenerator(settings);
            return generator.Generate(type);
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
            var typeDescription = Settings.ReflectionService.GetDescription(contextualType, Settings);

            ApplyTypeExtensionDataAttributes(schema, contextualType);

            if (TryHandleSpecialTypes(schema, typeDescription.ContextualType, schemaResolver))
            {
                ApplySchemaProcessors(schema, typeDescription.ContextualType, schemaResolver);
                return;
            }

            if (schemaResolver.RootObject == schema)
            {
                schema.Title = Settings.SchemaNameGenerator.Generate(typeDescription.ContextualType.Type);
            }

            if (typeDescription.Type.IsObject())
            {
                if (typeDescription.IsDictionary)
                {
                    GenerateDictionary(schema, typeDescription, schemaResolver);
                }
                else
                {
                    if (schemaResolver.HasSchema(typeDescription.ContextualType.Type, false))
                    {
                        schema.Reference = schemaResolver.GetSchema(typeDescription.ContextualType.Type, false);
                    }
                    else if (schema.GetType() == typeof(JsonSchema))
                    {
                        GenerateObject(schema, typeDescription, schemaResolver);
                    }
                    else
                    {
                        schema.Reference = Generate(typeDescription.ContextualType, schemaResolver);
                    }
                }
            }
            else if (typeDescription.IsEnum)
            {
                GenerateEnum(schema, typeDescription, schemaResolver);
            }
            else if (typeDescription.Type.IsArray()) // TODO: Add support for tuples?
            {
                GenerateArray(schema, typeDescription, schemaResolver);
            }
            else
            {
                typeDescription.ApplyType(schema);
            }

            if (contextualType != typeDescription.ContextualType)
            {
                ApplySchemaProcessors(schema, typeDescription.ContextualType, schemaResolver);
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
            Action<TSchemaType, JsonSchema>? transformation = null)
            where TSchemaType : JsonSchema, new()
        {
            return GenerateWithReferenceAndNullability(contextualType, false, schemaResolver, transformation);
        }

        /// <summary>Generates a schema directly or referenced for the requested schema type;
        /// also adds nullability if required by looking at the type's <see cref="JsonTypeDescription" />.</summary>
        /// <typeparam name="TSchemaType">The resulted schema type which may reference the actual schema.</typeparam>
        /// <param name="contextualType">The type of the schema to generate.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <param name="transformation">An action to transform the resulting schema (e.g. property or parameter) before the type of reference is determined (with $ref or allOf/oneOf).</param>
        /// <returns>The requested schema object.</returns>
        public TSchemaType GenerateWithReferenceAndNullability<TSchemaType>(
            ContextualType contextualType, JsonSchemaResolver schemaResolver,
            Action<TSchemaType, JsonSchema>? transformation = null)
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
            Action<TSchemaType, JsonSchema>? transformation = null)
            where TSchemaType : JsonSchema, new()
        {
            var typeDescription = Settings.ReflectionService.GetDescription(contextualType, Settings);
            var requiresSchemaReference = typeDescription.RequiresSchemaReference(Settings.TypeMappers);

            JsonSchema referencedSchema;
            if (!requiresSchemaReference)
            {
                var schema = Generate<TSchemaType>(typeDescription.ContextualType, schemaResolver);
                if (!schema.HasReference)
                {
                    transformation?.Invoke(schema, schema);

                    if (isNullable)
                    {
                        if (Settings.SchemaType == SchemaType.JsonSchema)
                        {
                            if (schema.Type == JsonObjectType.None)
                            {
                                schema._oneOf.Add(new JsonSchema { Type = JsonObjectType.None });
                                schema._oneOf.Add(new JsonSchema { Type = JsonObjectType.Null });
                            }
                            else
                            {
                                schema.Type |= JsonObjectType.Null;
                            }
                        }
                        else if (Settings.SchemaType == SchemaType.OpenApi3 || Settings.GenerateCustomNullableProperties)
                        {
                            schema.IsNullableRaw = true;
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
                referencedSchema = Generate<JsonSchema>(typeDescription.ContextualType, schemaResolver);
            }

            var referencingSchema = new TSchemaType();
            transformation?.Invoke(referencingSchema, referencedSchema);

            if (isNullable)
            {
                if (Settings.SchemaType == SchemaType.JsonSchema)
                {
                    referencingSchema._oneOf.Add(new JsonSchema { Type = JsonObjectType.Null });
                }
                else if (Settings.SchemaType == SchemaType.OpenApi3 || Settings.GenerateCustomNullableProperties)
                {
                    referencingSchema.IsNullableRaw = true;
                }
            }

            // See https://github.com/RicoSuter/NJsonSchema/issues/531
            var useDirectReference = Settings.AllowReferencesWithProperties ||
                JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(referencingSchema))?.Properties()?.Any() == false; // TODO: Improve performance

            if (useDirectReference && referencingSchema._oneOf.Count == 0)
            {
                referencingSchema.Reference = referencedSchema.ActualSchema;
            }
            else if (Settings.SchemaType != SchemaType.Swagger2)
            {
                referencingSchema._oneOf.Add(new JsonSchema
                {
                    Reference = referencedSchema.ActualSchema
                });
            }
            else
            {
                referencingSchema._allOf.Add(new JsonSchema
                {
                    Reference = referencedSchema.ActualSchema
                });
            }

            return referencingSchema;
        }

        /// <summary>Applies the property annotations to the JSON property.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeDescription">The property type description.</param>
        public virtual void ApplyDataAnnotations(JsonSchema schema, JsonTypeDescription typeDescription)
        {
            var contextualType = typeDescription.ContextualType;

            var attributes = contextualType.GetContextAttributes(true).ToArray();
            dynamic? displayAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.DisplayAttribute");
            if (displayAttribute != null)
            {
                // GetName returns null if the Name property on the attribute is not specified.
                var name = displayAttribute.GetName();
                if (name != null)
                {
                    schema.Title = name;
                }
            }

            dynamic? defaultValueAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DefaultValueAttribute");
            if (defaultValueAttribute != null)
            {
                if (typeDescription.IsEnum &&
                    typeDescription.Type.IsString())
                {
                    schema.Default = defaultValueAttribute.Value?.ToString();
                }
                else
                {
                    schema.Default = defaultValueAttribute.Value;
                }
            }

            dynamic? regexAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.RegularExpressionAttribute");
            if (regexAttribute != null)
            {
                if (typeDescription.IsDictionary)
                {
                    schema.AdditionalPropertiesSchema ??= new JsonSchema();

                    schema.AdditionalPropertiesSchema.Pattern = regexAttribute.Pattern;
                }
                else
                {
                    schema.Pattern = regexAttribute.Pattern;
                }
            }

            if (typeDescription.Type is JsonObjectType.Number or JsonObjectType.Integer)
            {
                ApplyRangeAttribute(schema, attributes);

                var multipleOfAttribute = contextualType.GetContextAttribute<MultipleOfAttribute>(true);
                if (multipleOfAttribute != null)
                {
                    schema.MultipleOf = multipleOfAttribute.MultipleOf;
                }
            }

            dynamic? minLengthAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.MinLengthAttribute");
            if (minLengthAttribute?.Length != null)
            {
                if (typeDescription.Type == JsonObjectType.String)
                {
                    schema.MinLength = minLengthAttribute?.Length;
                }
                else if (typeDescription.Type == JsonObjectType.Array)
                {
                    schema.MinItems = minLengthAttribute?.Length;
                }
            }

            dynamic? maxLengthAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.MaxLengthAttribute");
            if (maxLengthAttribute?.Length != null)
            {
                if (typeDescription.Type == JsonObjectType.String)
                {
                    schema.MaxLength = maxLengthAttribute?.Length;
                }
                else if (typeDescription.Type == JsonObjectType.Array)
                {
                    schema.MaxItems = maxLengthAttribute?.Length;
                }
            }

            dynamic? stringLengthAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.StringLengthAttribute");
            if (stringLengthAttribute != null)
            {
                if (typeDescription.Type == JsonObjectType.String)
                {
                    schema.MinLength = stringLengthAttribute.MinimumLength;
                    schema.MaxLength = stringLengthAttribute.MaximumLength;
                }
            }

            dynamic? dataTypeAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.DataTypeAttribute");
            if (dataTypeAttribute != null)
            {
                var dataType = dataTypeAttribute.DataType.ToString();
                if (DataTypeFormats.TryGetValue(dataType, out string format))
                {
                    schema.Format = format;
                }
            }
        }

        /// <summary>Gets the actual default value for the given object (e.g. correctly converts enums).</summary>
        /// <param name="type">The value type.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The converted default value.</returns>
        public virtual object? ConvertDefaultValue(ContextualType type, object? defaultValue)
        {
            if (defaultValue != null && defaultValue.GetType().GetTypeInfo().IsEnum)
            {
                var hasStringEnumConverter = Settings.ReflectionService.IsStringEnum(type, Settings);
                if (hasStringEnumConverter)
                {
                    return defaultValue?.ToString();
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

        /// <summary>Generates the example from the type's xml docs.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The JToken or null.</returns>
        public virtual object? GenerateExample(ContextualType type)
        {
            if (Settings.GenerateExamples && Settings.UseXmlDocumentation)
            {
                try
                {
                    var docs = type.GetXmlDocsTag("example", Settings.GetXmlDocsOptions());
                    return GenerateExample(docs);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>Generates the example from the accessor's xml docs.</summary>
        /// <param name="accessorInfo">The accessor info.</param>
        /// <returns>The JToken or null.</returns>
        public virtual object? GenerateExample(ContextualAccessorInfo accessorInfo)
        {
            if (Settings.GenerateExamples && Settings.UseXmlDocumentation)
            {
                try
                {
                    var docs = accessorInfo.GetXmlDocsTag("example", Settings.GetXmlDocsOptions());
                    return GenerateExample(docs);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        private static object? GenerateExample(string xmlDocs)
        {
            try
            {
                return !string.IsNullOrEmpty(xmlDocs) ?
                    JsonConvert.DeserializeObject<JToken>(xmlDocs) :
                    null;
            }
            catch
            {
                return xmlDocs;
            }
        }

        /// <summary>Generates the properties for the given type and schema.</summary>
        /// <param name="schema">The properties</param>
        /// <param name="typeDescription">The type description.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <returns>The task.</returns>
        protected virtual void GenerateObject(JsonSchema schema, JsonTypeDescription typeDescription, JsonSchemaResolver schemaResolver)
        {
            var type = typeDescription.ContextualType.Type;
            schemaResolver.AddSchema(type, false, schema);

            var rootSchema = schema;
            var actualSchema = GenerateInheritance(typeDescription.ContextualType, schema, schemaResolver);
            if (actualSchema != null)
            {
                schema = actualSchema;
            }
            else
            {
                Settings.ReflectionService.GenerateProperties(schema, typeDescription.ContextualType, Settings, this, schemaResolver);
                ApplyAdditionalProperties(schema, type, schemaResolver);
            }

            if (!schema.Type.IsArray())
            {
                typeDescription.ApplyType(schema);
            }

            schema.Description = type.ToCachedType().GetDescription(Settings);
            schema.Example = GenerateExample(type.ToContextualType());

            dynamic? obsoleteAttribute = type.GetTypeInfo().GetCustomAttributes(false).FirstAssignableToTypeNameOrDefault("System.ObsoleteAttribute");
            if (obsoleteAttribute != null)
            {
                schema.IsDeprecated = true;
                schema.DeprecatedMessage = obsoleteAttribute.Message;
            }

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
        public virtual string[]? GetTypeProperties(Type type)
        {
            if (type == typeof(Exception))
            {
                return ["InnerException", "Message", "Source", "StackTrace"];
            }

            return null;
        }

        /// <summary>Generates an array in the given schema.</summary>
        /// <typeparam name="TSchemaType">The schema type.</typeparam>
        /// <param name="schema">The schema.</param>
        /// <param name="typeDescription">The type description.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        protected virtual void GenerateArray<TSchemaType>(
            TSchemaType schema, JsonTypeDescription typeDescription, JsonSchemaResolver schemaResolver)
            where TSchemaType : JsonSchema, new()
        {
            var contextualType = typeDescription.ContextualType;

            typeDescription.ApplyType(schema);

            var jsonSchemaAttribute = contextualType.GetContextOrTypeAttribute<JsonSchemaAttribute>(true);
            var itemType = jsonSchemaAttribute?.ArrayItem?.ToContextualType() ??
                           contextualType.EnumerableItemType ??
                           contextualType.GenericArguments.FirstOrDefault();

            if (itemType != null)
            {
                var itemIsNullable = contextualType.IsContextAttributeDefined<ItemsCanBeNullAttribute>(true) ||
                                     itemType.Nullability == Nullability.Nullable;

                schema.Item = GenerateWithReferenceAndNullability<JsonSchema>(
                    itemType, itemIsNullable, schemaResolver, (itemSchema, typeSchema) =>
                    {
                        if (Settings.GenerateXmlObjects)
                        {
                            itemSchema.GenerateXmlObjectForItemType(itemType);
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

            var attributes = contextualType.GetContextAttributes(true).ToArray();
            dynamic? minLengthAttribute = attributes.FirstAssignableToTypeNameOrDefault("MinLengthAttribute", TypeNameStyle.Name);
            if (minLengthAttribute != null && ObjectExtensions.HasProperty(minLengthAttribute, "Length"))
            {
                schema.MinItems = minLengthAttribute?.Length;
            }

            dynamic? maxLengthAttribute = attributes.FirstAssignableToTypeNameOrDefault("MaxLengthAttribute", TypeNameStyle.Name);
            if (maxLengthAttribute != null && ObjectExtensions.HasProperty(maxLengthAttribute, "Length"))
            {
                schema.MaxItems = maxLengthAttribute?.Length;
            }
        }

        /// <summary>Generates an array in the given schema.</summary>
        /// <typeparam name="TSchemaType">The schema type.</typeparam>
        /// <param name="schema">The schema.</param>
        /// <param name="typeDescription">The type description.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
        /// <exception cref="InvalidOperationException">Could not find value type of dictionary type.</exception>
        protected virtual void GenerateDictionary<TSchemaType>(TSchemaType schema, JsonTypeDescription typeDescription, JsonSchemaResolver schemaResolver)
            where TSchemaType : JsonSchema, new()
        {
            var contextualType = typeDescription.ContextualType;

            typeDescription.ApplyType(schema);
            var genericTypeArguments = contextualType.GenericArguments;

            var keyType = genericTypeArguments.Length == 2 ? genericTypeArguments[0] : typeof(string).ToContextualType();
            if (keyType.OriginalType.GetTypeInfo().IsEnum)
            {
                schema.DictionaryKey = GenerateWithReference<JsonSchema>(keyType, schemaResolver);
            }

            var valueType = genericTypeArguments.Length == 2 ? genericTypeArguments[1] : typeof(object).ToContextualType();

            var attributes = contextualType.GetContextAttributes(true).ToArray();

            var patternPropertiesAttributes = attributes.OfType<JsonSchemaPatternPropertiesAttribute>();
            if (patternPropertiesAttributes.Any())
            {
                schema.AllowAdditionalProperties = false;
                foreach (var patternPropertiesAttribute in patternPropertiesAttributes)
                {
                    var property = GenerateDictionaryValueSchema<JsonSchemaProperty>(
                        schemaResolver, patternPropertiesAttribute.Type?.ToContextualType() ?? valueType);
                    schema.PatternProperties.Add(patternPropertiesAttribute.RegularExpression, property);
                }
            }
            else
            {
                schema.AdditionalPropertiesSchema = GenerateDictionaryValueSchema<JsonSchema>(schemaResolver, valueType);
                schema.AllowAdditionalProperties = true;
            }

            dynamic? minLengthAttribute = attributes.FirstAssignableToTypeNameOrDefault("MinLengthAttribute", TypeNameStyle.Name);
            if (minLengthAttribute != null && ObjectExtensions.HasProperty(minLengthAttribute, "Length"))
            {
                schema.MinProperties = minLengthAttribute?.Length;
            }

            dynamic? maxLengthAttribute = attributes.FirstAssignableToTypeNameOrDefault("MaxLengthAttribute", TypeNameStyle.Name);
            if (maxLengthAttribute != null && ObjectExtensions.HasProperty(maxLengthAttribute, "Length"))
            {
                schema.MaxProperties = maxLengthAttribute?.Length;
            }
        }

        /// <summary>Generates an enumeration in the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeDescription">The type description.</param>
        protected virtual void GenerateEnum(JsonSchema schema, JsonTypeDescription typeDescription)
        {
            var contextualType = typeDescription.ContextualType;

            schema.Type = typeDescription.Type;
            schema.Enumeration.Clear();
            schema.EnumerationNames.Clear();
            schema.EnumerationDescriptions.Clear();
            schema.IsFlagEnumerable = contextualType.IsAttributeDefined<FlagsAttribute>(true);

            Func<object, string?>? enumValueConverter = null;
            var underlyingType = Enum.GetUnderlyingType(contextualType.Type);
            foreach (var enumName in Enum.GetNames(contextualType.Type))
            {
                string? enumDescription = null;
                var field = contextualType.Type.GetRuntimeField(enumName);
                // Retrieve the Description attribute value, if present.
                var descriptionAttribute = field?.GetCustomAttribute<DescriptionAttribute>();

                if (descriptionAttribute != null)
                {
                    enumDescription = descriptionAttribute.Description;
                }

                if (typeDescription.Type == JsonObjectType.Integer)
                {
                    var value = Convert.ChangeType(Enum.Parse(contextualType.Type, enumName), underlyingType, CultureInfo.InvariantCulture);
                    schema.Enumeration.Add(value);
                }
                else
                {
                    // EnumMember only checked if StringEnumConverter is used
                    var enumMemberAttribute = field?.GetCustomAttribute<EnumMemberAttribute>();
                    if (enumMemberAttribute != null && !string.IsNullOrEmpty(enumMemberAttribute.Value))
                    {
                        schema.Enumeration.Add(enumMemberAttribute.Value);
                    }
                    else
                    {
                        enumValueConverter ??= Settings.ReflectionService.GetEnumValueConverter(Settings);
                        var value = Enum.Parse(contextualType.Type, enumName);
                        schema.Enumeration.Add(enumValueConverter(value));
                    }
                }

                schema.EnumerationNames.Add(enumName);
                schema.EnumerationDescriptions.Add(enumDescription);
            }

            if (typeDescription.Type == JsonObjectType.Integer && Settings.GenerateEnumMappingDescription)
            {
                schema.Description = (schema.Description + "\n\n" +
                    string.Join("\n", schema.Enumeration.Select((e, i) => e + " = " + schema.EnumerationNames[i]))).Trim();
            }
        }

        private TSchema GenerateDictionaryValueSchema<TSchema>(JsonSchemaResolver schemaResolver, ContextualType valueType)
            where TSchema : JsonSchema, new()
        {
            if (valueType.OriginalType == typeof(object))
            {
                var additionalPropertiesSchema = new TSchema();

                if (Settings.SchemaType == SchemaType.Swagger2)
                {
                    additionalPropertiesSchema.AllowAdditionalProperties = false;
                }

                return additionalPropertiesSchema;
            }
            else
            {
                var valueTypeInfo = Settings.ReflectionService.GetDescription(valueType, Settings.DefaultDictionaryValueReferenceTypeNullHandling, Settings);
                var valueTypeIsNullable = valueType.IsContextAttributeDefined<ItemsCanBeNullAttribute>(true) ||
                                          valueTypeInfo.IsNullable;

                return GenerateWithReferenceAndNullability<TSchema>(valueType, valueTypeIsNullable, schemaResolver);
            }
        }

        private void ApplyAdditionalProperties<TSchemaType>(TSchemaType schema, Type type, JsonSchemaResolver schemaResolver)
            where TSchemaType : JsonSchema, new()
        {
            var extensionDataProperty = type.GetContextualProperties()
                .FirstOrDefault(p => p.GetAttributes(true).Any(a =>
                    Namotion.Reflection.TypeExtensions.IsAssignableToTypeName(a.GetType(), "JsonExtensionDataAttribute", TypeNameStyle.Name)));

            if (extensionDataProperty != null)
            {
                var genericTypeArguments = extensionDataProperty.AccessorType.GenericArguments;
                var extensionDataPropertyType = genericTypeArguments.Length == 2 ? genericTypeArguments[1] : typeof(object).ToContextualType();

                schema.AdditionalPropertiesSchema = GenerateWithReferenceAndNullability<JsonSchema>(
                    extensionDataPropertyType, schemaResolver);
            }
            else
            {
                schema.AllowAdditionalProperties = Settings.AlwaysAllowAdditionalObjectProperties;
            }
        }

        private void ApplySchemaProcessors(JsonSchema schema, ContextualType contextualType, JsonSchemaResolver schemaResolver)
        {
            var context = new SchemaProcessorContext(contextualType, schema, schemaResolver, this, Settings);
            foreach (var processor in Settings.SchemaProcessors)
            {
                processor.Process(context);
            }

            var operationProcessorAttributes = contextualType
                .GetAttributes(true)
                .GetAssignableToTypeName(nameof(JsonSchemaProcessorAttribute), TypeNameStyle.Name);

            foreach (dynamic attribute in operationProcessorAttributes)
            {
                var processor = Activator.CreateInstance(attribute.Type, attribute.Parameters);
                processor.Process(context);
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
                var context = new TypeMapperContext(contextualType.OriginalType, this, schemaResolver, contextualType.GetContextAttributes(true));
                typeMapper.GenerateSchema(schema, context);
                return true;
            }

            if (!contextualType.OriginalType.IsAssignableToTypeName(nameof(JArray), TypeNameStyle.Name) &&
                (contextualType.OriginalType.IsAssignableToTypeName(nameof(JToken), TypeNameStyle.Name) ||
                 contextualType.OriginalType == typeof(object)))
            {
                if (Settings.SchemaType == SchemaType.Swagger2)
                {
                    schema.AllowAdditionalProperties = false;
                }

                return true;
            }

            return false;
        }

        private void GenerateEnum<TSchemaType>(
            TSchemaType schema, JsonTypeDescription typeDescription, JsonSchemaResolver schemaResolver)
            where TSchemaType : JsonSchema, new()
        {
            var type = typeDescription.ContextualType.Type;

            var isIntegerEnumeration = typeDescription.Type == JsonObjectType.Integer;
            if (schemaResolver.HasSchema(type, isIntegerEnumeration))
            {
                schema.Reference = schemaResolver.GetSchema(type, isIntegerEnumeration);
            }
            else if (schema.GetType() == typeof(JsonSchema))
            {
                typeDescription.ApplyType(schema);

                if (Settings.UseXmlDocumentation)
                {
                    schema.Description = type.GetXmlDocsSummary(Settings.GetXmlDocsOptions());
                }

                GenerateEnum(schema, typeDescription);
                schemaResolver.AddSchema(type, isIntegerEnumeration, schema);
            }
            else
            {
                schema.Reference = Generate(typeDescription.ContextualType, schemaResolver);
            }
        }

        /// <summary>
        /// Checks whether a member info is abstract.
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
#pragma warning disable CA1822
        public bool IsAbstractProperty(ContextualMemberInfo memberInfo)
#pragma warning restore CA1822
        {
            return memberInfo is ContextualPropertyInfo propertyInfo &&
                   propertyInfo.PropertyInfo.DeclaringType?.GetTypeInfo().IsInterface == false &&
                   (propertyInfo.PropertyInfo.GetMethod?.IsAbstract == true || propertyInfo.PropertyInfo.SetMethod?.IsAbstract == true);
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
                        var methodInfo = type.GetRuntimeMethod((string)attribute.MethodName, Type.EmptyTypes);
                        if (methodInfo != null)
                        {
                            if (methodInfo.Invoke(null, null) is IEnumerable<Type> knownTypes)
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

            foreach (var jsonConverterAttribute in attributes
                .GetAssignableToTypeName("System.Text.Json.Serialization.JsonDerivedTypeAttribute", TypeNameStyle.FullName))
            {
                var knownType = ObjectExtensions.TryGetPropertyValue<Type>(
                    jsonConverterAttribute, "DerivedType", null);

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

        private JsonSchema? GenerateInheritance(ContextualType type, JsonSchema schema, JsonSchemaResolver schemaResolver)
        {
            var baseType = type.BaseType;
            if (baseType != null && baseType.Type != typeof(object) && baseType.Type != typeof(ValueType))
            {
                if (baseType.GetContextOrTypeAttributes(false).FirstAssignableToTypeNameOrDefault("JsonSchemaIgnoreAttribute", TypeNameStyle.Name) == null &&
                    baseType.GetContextOrTypeAttributes(false).FirstAssignableToTypeNameOrDefault("SwaggerIgnoreAttribute", TypeNameStyle.Name) == null &&
                    Settings.ExcludedTypeNames?.Contains(baseType.Type.FullName) != true)
                {
                    if (Settings.GetActualFlattenInheritanceHierarchy(type))
                    {
                        var typeDescription = Settings.ReflectionService.GetDescription(baseType, Settings);
                        if (!typeDescription.IsDictionary && !type.Type.IsArray)
                        {
                            Settings.ReflectionService.GenerateProperties(schema, baseType, Settings, this, schemaResolver);
                            var actualSchema = GenerateInheritance(baseType, schema, schemaResolver);

                            GenerateInheritanceDiscriminator(baseType, schema, actualSchema ?? schema);
                        }
                    }
                    else
                    {
                        var actualSchema = new JsonSchema();

                        Settings.ReflectionService.GenerateProperties(actualSchema, type, Settings, this, schemaResolver);
                        ApplyAdditionalProperties(actualSchema, type, schemaResolver);

                        var baseTypeInfo = Settings.ReflectionService.GetDescription(baseType, Settings);
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

                                schema._allOf.Add(new JsonSchema
                                {
                                    Reference = baseSchema.ActualSchema
                                });
                            }
                            else
                            {
                                schema._allOf.Add(baseSchema);
                            }

                            // First schema is the (referenced) base schema, second is the type schema itself
                            schema._allOf.Add(actualSchema);
                            return actualSchema;
                        }
                        else
                        {
                            // Array and dictionary inheritance are not expressed with allOf but inline
                            Generate(schema, baseType, schemaResolver);
                            return schema;
                        }
                    }
                }
            }

            if (Settings.GetActualFlattenInheritanceHierarchy(type) && Settings.GenerateAbstractProperties)
            {
                foreach (var implementedInterface in type.Type.GetTypeInfo().ImplementedInterfaces)
                {
                    var contextualType = implementedInterface.ToContextualType();
                    var typeDescription = Settings.ReflectionService.GetDescription(contextualType, Settings);
                    if (!typeDescription.IsDictionary && !type.Type.IsArray &&
                        !typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(implementedInterface.GetTypeInfo()))
                    {
                        Settings.ReflectionService.GenerateProperties(schema, contextualType, Settings, this, schemaResolver);
                        var actualSchema = GenerateInheritance(contextualType, schema, schemaResolver);

                        GenerateInheritanceDiscriminator(implementedInterface, schema, actualSchema ?? schema);
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
                    if (discriminatorName is not null)
                    {
                        // Existing property can be discriminator only if it has String type
                        if (typeSchema.Properties.TryGetValue(discriminatorName, out var existingProperty))
                        {
                            if (!existingProperty.ActualTypeSchema.Type.IsInteger() &&
                                !existingProperty.ActualTypeSchema.Type.IsString())
                            {
                                throw new InvalidOperationException("The JSON discriminator property '" + discriminatorName +
                                    "' must be a string|int property on type '" + type.FullName +
                                    "' (it is recommended to not implement the discriminator property at all).");
                            }

                            existingProperty.IsRequired = true;
                        }

                        var discriminator = new OpenApiDiscriminator
                        {
                            JsonInheritanceConverter = discriminatorConverter,
                            PropertyName = discriminatorName
                        };

                        typeSchema.DiscriminatorObject = discriminator;

                        if (!typeSchema.Properties.ContainsKey(discriminatorName))
                        {
                            typeSchema.Properties[discriminatorName] = new JsonSchemaProperty
                            {
                                Type = JsonObjectType.String,
                                IsRequired = true
                            };
                        }
                    }
                }
                else
                {
                    var baseDiscriminator = schema.ResponsibleDiscriminatorObject ?? schema.ActualTypeSchema.ResponsibleDiscriminatorObject;
                    baseDiscriminator?.AddMapping(type, schema);
                }
            }
        }

#pragma warning disable CA1859
        private object? TryGetInheritanceDiscriminatorConverter(Type type)
        {
            var typeAttributes = type.GetTypeInfo().GetCustomAttributes(false).OfType<Attribute>();

            // support for NJsonSchema provided inheritance converters
            dynamic? jsonConverterAttribute = typeAttributes.FirstAssignableToTypeNameOrDefault(nameof(JsonConverterAttribute), TypeNameStyle.Name);
            if (jsonConverterAttribute != null)
            {
                var converterType = (Type)jsonConverterAttribute.ConverterType;
                if (converterType != null && (
                        converterType.IsAssignableToTypeName("JsonInheritanceConverter", TypeNameStyle.Name) || // Newtonsoft's converter
                        converterType.IsAssignableToTypeName("JsonInheritanceConverter`1", TypeNameStyle.Name) // System.Text.Json's converter
                    ))
                {
                    return ObjectExtensions.HasProperty(jsonConverterAttribute, "ConverterParameters") &&
                           jsonConverterAttribute.ConverterParameters != null &&
                           jsonConverterAttribute.ConverterParameters.Length > 0 ?
                        Activator.CreateInstance(jsonConverterAttribute.ConverterType, jsonConverterAttribute.ConverterParameters) :
                        Activator.CreateInstance(jsonConverterAttribute.ConverterType);
                }
            }

            // support for native System.Text.Json inheritance
            dynamic[] jsonDerivedTypeAttributes = typeAttributes
                .Where(a => a.GetType().IsAssignableToTypeName("System.Text.Json.Serialization.JsonDerivedTypeAttribute", TypeNameStyle.FullName))
                .ToArray();

            if (jsonDerivedTypeAttributes.Length > 0)
            {
                dynamic? jsonPolymorphicAttribute = typeAttributes
                    .FirstAssignableToTypeNameOrDefault("System.Text.Json.Serialization.JsonPolymorphicAttribute", TypeNameStyle.FullName);
                return new SystemTextJsonInheritanceWrapper(jsonPolymorphicAttribute?.TypeDiscriminatorPropertyName ?? "$type", jsonDerivedTypeAttributes);
            }

            return null;
        }
#pragma warning restore CA1859


        private sealed class SystemTextJsonInheritanceWrapper
        {
            private readonly dynamic[] _jsonDerivedTypeAttributes;

            public SystemTextJsonInheritanceWrapper(string discriminatorName, dynamic[] jsonDerivedTypeAttributes)
            {
                DiscriminatorName = discriminatorName;
                _jsonDerivedTypeAttributes = jsonDerivedTypeAttributes;
            }

            public string DiscriminatorName { get; }

            public string GetDiscriminatorValue(Type type)
            {
                return _jsonDerivedTypeAttributes.FirstOrDefault(a => a.DerivedType == type)?.TypeDiscriminator?.ToString()
                    ?? throw new InvalidOperationException($"Discriminator value for {type.FullName} not found.");
            }
        }

        private static string? TryGetInheritanceDiscriminatorName(object jsonInheritanceConverter)
        {
            return ObjectExtensions.TryGetPropertyValue(
                jsonInheritanceConverter,
                nameof(JsonInheritanceConverterAttribute.DiscriminatorName),
                JsonInheritanceConverterAttribute.DefaultDiscriminatorName);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="parentSchema"></param>
        /// <param name="property"></param>
        /// <param name="propertyTypeDescription"></param>
        /// <param name="propertyName"></param>
        /// <param name="requiredAttribute"></param>
        /// <param name="hasRequiredAttribute"></param>
        /// <param name="isNullable"></param>
        /// <param name="defaultValue"></param>
        /// <param name="schemaResolver"></param>
        public void AddProperty(
            JsonSchema parentSchema,
            ContextualAccessorInfo property,
            JsonTypeDescription propertyTypeDescription, string propertyName,
            Attribute? requiredAttribute, bool hasRequiredAttribute, bool isNullable, object? defaultValue, JsonSchemaResolver schemaResolver)
        {
            // TODO: Extension method on JsonSchema class?

            Action<JsonSchemaProperty, JsonSchema> TransformSchema = (propertySchema, typeSchema) =>
            {
                if (Settings.GenerateXmlObjects)
                {
                    propertySchema.GenerateXmlObjectForProperty(property.AccessorType, propertyName);
                }

                if (hasRequiredAttribute &&
                    !propertyTypeDescription.IsEnum &&
                    propertyTypeDescription.Type == JsonObjectType.String &&
                    !requiredAttribute.TryGetPropertyValue("AllowEmptyStrings", false))
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

                dynamic? readOnlyAttribute = property.GetAttributes(true).FirstAssignableToTypeNameOrDefault("System.ComponentModel.ReadOnlyAttribute");
                if (readOnlyAttribute != null)
                {
                    propertySchema.IsReadOnly = readOnlyAttribute.IsReadOnly;
                }

                propertySchema.Description ??= property.GetDescription(Settings);
                propertySchema.Example ??= GenerateExample(property);

                dynamic? obsoleteAttribute = property.GetAttributes(true).FirstAssignableToTypeNameOrDefault("System.ObsoleteAttribute");
                if (obsoleteAttribute != null)
                {
                    propertySchema.IsDeprecated = true;
                    propertySchema.DeprecatedMessage = obsoleteAttribute.Message;
                }

                propertySchema.Default = ConvertDefaultValue(property.AccessorType, defaultValue);

                ApplyDataAnnotations(propertySchema, propertyTypeDescription);
                ApplyPropertyExtensionDataAttributes(propertySchema, property);
            };

            var referencingProperty = GenerateWithReferenceAndNullability(
                property.AccessorType, isNullable, schemaResolver, TransformSchema);

            parentSchema.Properties.Add(propertyName, referencingProperty);
        }

        /// <summary>Checks whether a property is ignored.</summary>
        /// <param name="accessorInfo">The accessor info.</param>
        /// <param name="parentType">The properties parent type.</param>
        /// <returns>The result.</returns>
        public virtual bool IsPropertyIgnored(ContextualAccessorInfo accessorInfo, Type parentType)
        {
            if (accessorInfo.IsAttributeDefined<JsonIgnoreAttribute>(true))
            {
                return true;
            }

            if (!accessorInfo.IsAttributeDefined<JsonPropertyAttribute>(true) &&
                HasDataContractAttribute(parentType) &&
                GetDataMemberAttribute(accessorInfo, parentType) == null)
            {
                return true;
            }

            return IsPropertyIgnoredBySettings(accessorInfo);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="accessorInfo"></param>
        /// <returns></returns>
        public bool IsPropertyIgnoredBySettings(ContextualAccessorInfo accessorInfo)
        {
            if (Settings.IgnoreObsoleteProperties &&
                accessorInfo.IsAttributeDefined<ObsoleteAttribute>(true))
            {
                return true;
            }

            if (accessorInfo.IsAttributeDefined<JsonSchemaIgnoreAttribute>(true))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="accessorInfo"></param>
        /// <param name="parentType"></param>
        /// <returns></returns>
#pragma warning disable CA1822
        public dynamic? GetDataMemberAttribute(ContextualAccessorInfo accessorInfo, Type parentType)
#pragma warning restore CA1822
        {
            if (!HasDataContractAttribute(parentType))
            {
                return null;
            }

            return accessorInfo.GetAttributes(true).FirstAssignableToTypeNameOrDefault("DataMemberAttribute", TypeNameStyle.Name);
        }

        private static bool HasDataContractAttribute(Type parentType)
        {
            return parentType.ToCachedType()
                .GetAttributes(true)
                .FirstAssignableToTypeNameOrDefault("DataContractAttribute", TypeNameStyle.Name) != null;
        }

        private static void ApplyRangeAttribute(JsonSchema schema, IEnumerable<Attribute> parentAttributes)
        {
            dynamic? rangeAttribute = parentAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.RangeAttribute");
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

        private static void ApplyTypeExtensionDataAttributes(JsonSchema schema, ContextualType contextualType)
        {
            var extensionAttributes = contextualType
                .GetAttributes<IJsonSchemaExtensionDataAttribute>(true)
                .ToArray();

            ApplyTypeExtensionDataAttributes(schema, extensionAttributes);
        }

        private static void ApplyPropertyExtensionDataAttributes(JsonSchemaProperty propertySchema, ContextualAccessorInfo accessorInfo)
        {
            var extensionAttributes = accessorInfo
                .GetAttributes<IJsonSchemaExtensionDataAttribute>(true)
                .ToArray();

            ApplyTypeExtensionDataAttributes(propertySchema, extensionAttributes);
        }

        private static void ApplyTypeExtensionDataAttributes(JsonSchema schema, IJsonSchemaExtensionDataAttribute[] extensionAttributes)
        {
            if (extensionAttributes.Length > 0)
            {
                var extensionData = new Dictionary<string, object?>();
                foreach (var kvp in extensionAttributes
                    .SelectMany(attribute => attribute.ExtensionData))
                {
                    extensionData[kvp.Key] = kvp.Value;
                }

                schema.ExtensionData = extensionData;
            }
        }
    }
}
