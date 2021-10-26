//-----------------------------------------------------------------------
// <copyright file="JsonInheritanceConverter.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NJsonSchema.Converters2
{
    internal class JsonInheritanceConverterAttribute : JsonConverterAttribute
    {
        public string DiscriminatorName { get; }

        public JsonInheritanceConverterAttribute(Type baseType, string discriminatorName = "discriminator")
            : base(typeof(JsonInheritanceConverter<>).MakeGenericType(baseType))
        {
            DiscriminatorName = discriminatorName;
        }
    }

    internal class JsonInheritanceConverter<TBase> : JsonConverter<TBase>
    {
        private readonly string _discriminatorName;

        public JsonInheritanceConverter()
        {
            var attribute = CustomAttributeExtensions.GetCustomAttribute<JsonInheritanceConverterAttribute>(typeof(TBase));
            _discriminatorName = attribute?.DiscriminatorName ?? "discriminator";
        }

        public JsonInheritanceConverter(string discriminatorName)
        {
            _discriminatorName = discriminatorName;
        }

        public virtual string DiscriminatorName => _discriminatorName;

        public override TBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var document = JsonDocument.ParseValue(ref reader);
            var hasDiscriminator = document.RootElement.TryGetProperty(_discriminatorName, out var discriminator);
            var subtype = GetDiscriminatorType(document.RootElement, typeToConvert, hasDiscriminator ? discriminator.GetString() : null);

            var bufferWriter = new System.IO.MemoryStream();
            using (var writer = new Utf8JsonWriter(bufferWriter))
            {
                document.RootElement.WriteTo(writer);
            }

            return (TBase)JsonSerializer.Deserialize(bufferWriter.ToArray(), subtype, options);
        }

        public override void Write(Utf8JsonWriter writer, TBase value, JsonSerializerOptions options)
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

        public virtual string GetDiscriminatorValue(Type type)
        {
            var jsonInheritanceAttributeDiscriminator = GetSubtypeDiscriminator(type);
            if (jsonInheritanceAttributeDiscriminator != null)
            {
                return jsonInheritanceAttributeDiscriminator;
            }

            return type.Name;
        }

        protected virtual Type GetDiscriminatorType(JsonElement jObject, Type objectType, string discriminatorValue)
        {
            var jsonInheritanceAttributeSubtype = GetObjectSubtype(objectType, discriminatorValue);
            if (jsonInheritanceAttributeSubtype != null)
            {
                return jsonInheritanceAttributeSubtype;
            }

            if (objectType.Name == discriminatorValue)
            {
                return objectType;
            }

            var typeName = objectType.Namespace + "." + discriminatorValue;
            var subtype = IntrospectionExtensions.GetTypeInfo(objectType).Assembly.GetType(typeName);
            if (subtype != null)
            {
                return subtype;
            }

            throw new InvalidOperationException("Could not find subtype of '" + objectType.Name + "' with discriminator '" + discriminatorValue + "'.");
        }

        private Type GetObjectSubtype(Type objectType, string discriminator)
        {
            foreach (var attribute in CustomAttributeExtensions.GetCustomAttributes<JsonInheritanceAttribute>(IntrospectionExtensions.GetTypeInfo(objectType), true))
            {
                if (attribute.Key == discriminator)
                    return attribute.Type;
            }

            return objectType;
        }

        private string GetSubtypeDiscriminator(Type objectType)
        {
            foreach (var attribute in CustomAttributeExtensions.GetCustomAttributes<JsonInheritanceAttribute>(IntrospectionExtensions.GetTypeInfo(objectType), true))
            {
                if (attribute.Type == objectType)
                    return attribute.Key;
            }

            return objectType.Name;
        }
    }
}
