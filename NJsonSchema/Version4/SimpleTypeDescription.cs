using System;
using System.Collections;
using System.Reflection;

namespace NJsonSchema.Version4
{
    internal class SimpleTypeDescription
    {
        /// <summary>Creates a <see cref="SimpleTypeDescription"/> from a <see cref="Type"/>. </summary>
        /// <param name="type">The type. </param>
        /// <returns>The <see cref="SimpleTypeDescription"/>. </returns>
        public static SimpleTypeDescription FromType(Type type)
        {
            if ((type == typeof(int) || type == typeof(long) || type == typeof(short)) ||
                (type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort)))
                return new SimpleTypeDescription(SimpleType.Integer, true);

            if (type == typeof(double) || type == typeof(decimal) || type == typeof(float))
                return new SimpleTypeDescription(SimpleType.Number, true);

            if (type == typeof(bool))
                return new SimpleTypeDescription(SimpleType.Boolean, true);

            if (type == typeof(string))
                return new SimpleTypeDescription(SimpleType.String, false);

            if (type == typeof(DateTime))
                return new SimpleTypeDescription(SimpleType.String, true, "date-time");

            if (type == typeof(Uri))
                return new SimpleTypeDescription(SimpleType.String, true, "uri");

            if (IsArrayType(type))
                return new SimpleTypeDescription(SimpleType.Array, false);

            if (type.Name == "Nullable`1")
            {
                var typeDescription = FromType(type.GenericTypeArguments[0]);
                typeDescription.IsAlwaysRequired = false; 
                return typeDescription;
            }

            return new SimpleTypeDescription(SimpleType.Object, false);
        }

        private SimpleTypeDescription(SimpleType type, bool isAlwaysRequired, string format = null)
        {
            Type = type;
            IsAlwaysRequired = isAlwaysRequired;
            Format = format;
        }

        /// <summary>Gets the type. </summary>
        public SimpleType Type { get; private set; }

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