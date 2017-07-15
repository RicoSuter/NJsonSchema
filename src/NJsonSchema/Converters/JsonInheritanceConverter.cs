//-----------------------------------------------------------------------
// <copyright file="JsonInheritanceConverter.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Converters
{
    // TODO: Add caching

    /// <summary>Defines the class as inheritance base class and adds a discriminator property to the serialized object.</summary>
    public class JsonInheritanceConverter : JsonConverter
    {
        internal static readonly string DefaultDiscriminatorName = "discriminator";

        private readonly string _discriminator;
        private readonly bool _readTypeProperty;

        [ThreadStatic]
        private static bool _isReading;

        [ThreadStatic]
        private static bool _isWriting;

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter"/> class.</summary>
        public JsonInheritanceConverter()
            : this(DefaultDiscriminatorName, false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter"/> class.</summary>
        /// <param name="discriminator">The discriminator.</param>
        public JsonInheritanceConverter(string discriminator)
            : this(discriminator, false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter"/> class.</summary>
        /// <param name="discriminator">The discriminator.</param>
        /// <param name="readTypeProperty">Read the $type property to determine the type (fallback).</param>
        public JsonInheritanceConverter(string discriminator, bool readTypeProperty)
        {
            _discriminator = discriminator;
            _readTypeProperty = readTypeProperty;
        }

        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            try
            {
                _isWriting = true;

                var jObject = JObject.FromObject(value, serializer);
                jObject.AddFirst(new JProperty(_discriminator, value.GetType().Name));
                writer.WriteToken(jObject.CreateReader());
            }
            finally
            {
                _isWriting = false;
            }
        }

        /// <summary>Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON.</summary>
        public override bool CanWrite
        {
            get
            {
                if (_isWriting)
                {
                    _isWriting = false;
                    return false;
                }
                return true;
            }
        }

        /// <summary>Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can read JSON.</summary>
        public override bool CanRead
        {
            get
            {
                if (_isReading)
                {
                    _isReading = false;
                    return false;
                }
                return true;
            }
        }

        /// <summary>Determines whether this instance can convert the specified object type.</summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns><c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.</returns>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = serializer.Deserialize<JObject>(reader);
            if (jObject == null)
                return null;

            var discriminator = jObject.GetValue(_discriminator).Value<string>();
            var subtype = GetObjectSubtype(jObject, objectType, discriminator);

            try
            {
                _isReading = true;
                return serializer.Deserialize(jObject.CreateReader(), subtype);
            }
            finally
            {
                _isReading = false;
            }
        }

        private Type GetObjectSubtype(JObject jObject, Type objectType, string discriminator)
        {
            if (objectType.Name == discriminator)
                return objectType;

            var knownTypeAttributesSubtype = GetSubtypeFromKnownTypeAttributes(objectType, discriminator);
            if (knownTypeAttributesSubtype != null)
                return knownTypeAttributesSubtype;

            var typeName = objectType.Namespace + "." + discriminator;
            var subtype = objectType.GetTypeInfo().Assembly.GetType(typeName);
            if (subtype != null)
                return subtype;

            if (_readTypeProperty)
            {
                var typeInfo = jObject.GetValue("$type");
                if (typeInfo != null)
                    return Type.GetType(typeInfo.Value<string>());
            }

            throw new InvalidOperationException("Could not find subtype of '" + objectType.Name + "' with discriminator '" + discriminator + "'.");
        }

        private Type GetSubtypeFromKnownTypeAttributes(Type objectType, string discriminator)
        {
            var type = objectType;
            do
            {
                var knownTypeAttributes = type.GetTypeInfo().GetCustomAttributes(false)
                    .Where(a => a.GetType().Name == "KnownTypeAttribute");
                foreach (dynamic attribute in knownTypeAttributes)
                {
                    if (attribute.Type != null && attribute.Type.Name == discriminator)
                        return attribute.Type;
                    else if (attribute.MethodName != null)
                    {
                        var method = type.GetRuntimeMethod((string)attribute.MethodName, new Type[0]);
                        if (method != null)
                        {
                            var types = (System.Collections.Generic.IEnumerable<Type>)method.Invoke(null, new object[0]);
                            foreach (var knownType in types)
                            {
                                if (knownType.Name == discriminator)
                                    return knownType;
                            }
                            return null;
                        }
                    }
                }
                type = type.GetTypeInfo().BaseType;
            } while (type != null);
            return null;
        }
    }
}