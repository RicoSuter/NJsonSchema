//-----------------------------------------------------------------------
// <copyright file="JsonInheritanceConverter.cs" company="NJsonSchema">
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
using NJsonSchema.Generation;

namespace NJsonSchema.Converters
{
    /// <summary>Defines external <see cref="JsonInheritanceConverter"/>.</summary>
    public class GlobalJsonInheritanceConverter : JsonConverter
    {
        private readonly Dictionary<DiscriminatorDefinition, JsonInheritanceConverter> _definitions;

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter"/> class.</summary>
        /// <param name="definitions">The discriminator definitions.</param>
        public GlobalJsonInheritanceConverter(IEnumerable<DiscriminatorDefinition> definitions)
        {
            _definitions = definitions.ToDictionary(d => d, d => new JsonInheritanceConverter(d.PropertyName));
        }

        /// <summary>Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can read JSON.</summary>
        public override bool CanRead => _definitions.All(d => d.Value.CanRead);

        /// <summary>Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON.</summary>
        public override bool CanWrite => _definitions.All(d => d.Value.CanWrite);

        /// <summary>Determines whether this instance can convert the specified object type.</summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns><c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.</returns>
        public override bool CanConvert(Type objectType)
        {
            var definition = TryGetConverter(objectType);
            return definition != null;
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var definition = TryGetConverter(objectType);
            return definition.ReadJson(reader, objectType, existingValue, serializer);
        }

        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var definition = TryGetConverter(value?.GetType());
            definition.WriteJson(writer, value, serializer);
        }

        private JsonInheritanceConverter TryGetConverter(Type objectType)
        {
            var type = objectType;
            while (type != null)
            {
                var definition = _definitions.SingleOrDefault(d => d.Key.BaseType == type).Value;
                if (definition != null)
                {
                    return definition;
                }

#if NET40
                type = type.BaseType;
#else
                type = type.GetTypeInfo().BaseType;
#endif
            }

            return null;
        }
    }
}