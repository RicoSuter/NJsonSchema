//-----------------------------------------------------------------------
// <copyright file="JsonInheritanceConverter.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NJsonSchema.Converters
{
    /// <summary>
    /// The JSON inheritance converter attribute.
    /// </summary>
    public class JsonInheritanceConverterAttribute : JsonConverterAttribute
    {
        /// <summary>Gets the default discriminator name.</summary>
        public static string DefaultDiscriminatorName { get; } = "discriminator";

        /// <summary>
        /// Gets the discriminator name.
        /// </summary>
        public string DiscriminatorName { get; }

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverterAttribute"/> class.</summary>
        /// <param name="baseType">The base type.</param>
        /// <param name="discriminatorName">The discriminator name.</param>
        public JsonInheritanceConverterAttribute(Type baseType, string discriminatorName = "discriminator")
            : base(typeof(JsonInheritanceConverter<>).MakeGenericType(baseType))
        {
            DiscriminatorName = discriminatorName;
        }
    }

    /// <summary>Defines the class as inheritance base class and adds a discriminator property to the serialized object.</summary>
    public class JsonInheritanceConverter<TBase> : JsonConverter<TBase?>
    {
        /// <summary>Gets the list of additional known types.</summary>
#pragma warning disable CA1000
        public static IDictionary<string, Type> AdditionalKnownTypes { get; } = new Dictionary<string, Type>();
#pragma warning restore CA1000

        private readonly string _discriminatorName;

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter{TBase}"/> class.</summary>
        public JsonInheritanceConverter()
        {
            var attribute = typeof(TBase).GetCustomAttribute<JsonInheritanceConverterAttribute>();
            _discriminatorName = attribute?.DiscriminatorName ?? "discriminator";
        }

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter{TBase}"/> class.</summary>
        /// <param name="discriminatorName">The discriminator name.</param>
        public JsonInheritanceConverter(string discriminatorName)
        {
            _discriminatorName = discriminatorName;
        }

        /// <summary>Gets the discriminator property name.</summary>
        public virtual string DiscriminatorName => _discriminatorName;

        /// <inheritdoc />
        public override TBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var document = JsonDocument.ParseValue(ref reader);
            var hasDiscriminator = document.RootElement.TryGetProperty(_discriminatorName, out var discriminator);
            var subtype = GetDiscriminatorType(document.RootElement, typeToConvert, hasDiscriminator ? discriminator.GetString() : null);

            var bufferWriter = new MemoryStream();
            using (var writer = new Utf8JsonWriter(bufferWriter))
            {
                document.RootElement.WriteTo(writer);
            }

            return (TBase?)JsonSerializer.Deserialize(bufferWriter.ToArray(), subtype, options)!;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TBase? value, JsonSerializerOptions options)
        {
            if (value is not null)
            {
                writer.WriteStartObject();
                writer.WriteString(_discriminatorName, GetDiscriminatorValue(value.GetType()));

                var bytes = JsonSerializer.SerializeToUtf8Bytes((object)value, options);
                var document = JsonDocument.Parse(bytes);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    property.WriteTo(writer);
                }

                writer.WriteEndObject();
            }
            else
            {
                writer.WriteNullValue();
            }
        }

        /// <summary>Gets the discriminator value for the given type.</summary>
        /// <param name="type">The object type.</param>
        /// <returns>The discriminator value.</returns>
        public virtual string GetDiscriminatorValue(Type type)
        {
            var knownType = AdditionalKnownTypes.SingleOrDefault(p => p.Value == type);
            if (knownType.Key != null)
            {
                return knownType.Key;
            }

            var jsonInheritanceAttributeDiscriminator = GetSubtypeDiscriminator(type);
            if (jsonInheritanceAttributeDiscriminator != null)
            {
                return jsonInheritanceAttributeDiscriminator;
            }

            return type.Name;
        }

        /// <summary>Gets the type for the given discriminator value.</summary>
        /// <param name="jObject">The JSON object.</param>
        /// <param name="objectType">The object (base) type.</param>
        /// <param name="discriminatorValue">The discriminator value.</param>
        /// <returns></returns>
        protected virtual Type GetDiscriminatorType(JsonElement jObject, Type objectType, string? discriminatorValue)
        {
            if (discriminatorValue != null)
            {
                if (AdditionalKnownTypes.TryGetValue(discriminatorValue, out Type? value))
                {
                    return value;
                }

                var jsonInheritanceAttributeSubtype = GetObjectSubtype(objectType, discriminatorValue);
                if (jsonInheritanceAttributeSubtype != null)
                {
                    return jsonInheritanceAttributeSubtype;
                }

                if (objectType.Name == discriminatorValue)
                {
                    return objectType;
                }

                var knownTypeAttributesSubtype = GetSubtypeFromKnownTypeAttributes(objectType, discriminatorValue);
                if (knownTypeAttributesSubtype != null)
                {
                    return knownTypeAttributesSubtype;
                }

                var typeName = objectType.Namespace + "." + discriminatorValue;
                var subtype = objectType.GetTypeInfo().Assembly.GetType(typeName);
                if (subtype != null)
                {
                    return subtype;
                }
            }

            throw new InvalidOperationException("Could not find subtype of '" + objectType.Name + "' with discriminator '" + discriminatorValue + "'.");
        }

        private static Type? GetSubtypeFromKnownTypeAttributes(Type objectType, string discriminatorValue)
        {
            var type = objectType;
            do
            {
                var knownTypeAttributes = type
                    .GetTypeInfo()
                    .GetCustomAttributes(false)
                    .Where(a => a.GetType().Name == "KnownTypeAttribute");

                foreach (dynamic attribute in knownTypeAttributes)
                {
                    if (attribute.Type != null && attribute.Type.Name == discriminatorValue)
                    {
                        return attribute.Type;
                    }
                    else if (attribute.MethodName != null)
                    {
                        var method = type.GetRuntimeMethod((string)attribute.MethodName, Type.EmptyTypes);
                        if (method != null)
                        {
                            if (method.Invoke(null, Array.Empty<object>()) is IEnumerable<Type> types)
                            {
                                foreach (var knownType in types)
                                {
                                    if (knownType.Name == discriminatorValue)
                                    {
                                        return knownType;
                                    }
                                }
                            }
                            return null;
                        }
                    }
                }

                type = type.GetTypeInfo().BaseType;
            } while (type != null);

            return null;
        }

        private static Type? GetObjectSubtype(Type baseType, string discriminatorValue)
        {
            var jsonInheritanceAttributes = baseType
                .GetTypeInfo()
                .GetCustomAttributes(true)
                .OfType<JsonInheritanceAttribute>();

            return jsonInheritanceAttributes.SingleOrDefault(a => a.Key == discriminatorValue)?.Type;
        }

        private static string? GetSubtypeDiscriminator(Type objectType)
        {
            var jsonInheritanceAttributes = objectType
                .GetTypeInfo()
                .GetCustomAttributes(true)
                .OfType<JsonInheritanceAttribute>();

            return jsonInheritanceAttributes.SingleOrDefault(a => a.Type == objectType)?.Key;
        }
    }
}
