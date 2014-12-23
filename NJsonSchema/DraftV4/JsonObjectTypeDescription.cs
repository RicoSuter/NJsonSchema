using System;
using System.Collections;
using System.Reflection;

namespace NJsonSchema.DraftV4
{
    internal class JsonObjectTypeDescription
    {
        /// <summary>Creates a <see cref="JsonObjectTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="type">The type. </param>
        /// <returns>The <see cref="JsonObjectTypeDescription"/>. </returns>
        public static JsonObjectTypeDescription FromType(Type type)
        {
            if ((type == typeof(int) || type == typeof(long) || type == typeof(short)) ||
                (type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort)))
                return new JsonObjectTypeDescription(JsonObjectType.Integer, true);

            if (type == typeof(double) || type == typeof(decimal) || type == typeof(float))
                return new JsonObjectTypeDescription(JsonObjectType.Number, true);

            if (type == typeof(bool))
                return new JsonObjectTypeDescription(JsonObjectType.Boolean, true);

            if (type == typeof(string))
                return new JsonObjectTypeDescription(JsonObjectType.String, false);

            if (type == typeof(DateTime))
                return new JsonObjectTypeDescription(JsonObjectType.String, true, "date-time");

            if (type == typeof(Uri))
                return new JsonObjectTypeDescription(JsonObjectType.String, true, "uri");

            if (IsArrayType(type))
                return new JsonObjectTypeDescription(JsonObjectType.Array, false);

            if (type.Name == "Nullable`1")
            {
                var typeDescription = FromType(type.GenericTypeArguments[0]);
                typeDescription.IsAlwaysRequired = false; 
                return typeDescription;
            }

            return new JsonObjectTypeDescription(JsonObjectType.Object, false);
        }

        private JsonObjectTypeDescription(JsonObjectType type, bool isAlwaysRequired, string format = null)
        {
            Type = type;
            IsAlwaysRequired = isAlwaysRequired;
            Format = format;
        }

        /// <summary>Gets the type. </summary>
        public JsonObjectType Type { get; private set; }

        /// <summary>Gets a value indicating whether the type must always required. </summary>
        public bool IsAlwaysRequired { get; private set; }

        /// <summary>Gets the format string. </summary>
        public string Format { get; private set; }

        private static bool IsArrayType(Type type)
        {
            return typeof(ICollection).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
        }
    }
}