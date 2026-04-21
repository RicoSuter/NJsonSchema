// intentionally root name space to light up faster helpers

using System.Runtime.CompilerServices;

namespace NJsonSchema
{
    internal static class EnumExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull(this JsonObjectType type) => (type & JsonObjectType.Null) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumber(this JsonObjectType type) => (type & JsonObjectType.Number) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsObject(this JsonObjectType type) => (type & JsonObjectType.Object) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsArray(this JsonObjectType type) => (type & JsonObjectType.Array) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInteger(this JsonObjectType type) => (type & JsonObjectType.Integer) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsString(this JsonObjectType type) => (type & JsonObjectType.String) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBoolean(this JsonObjectType type) => (type & JsonObjectType.Boolean) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFile(this JsonObjectType type) => (type & JsonObjectType.File) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNone(this JsonObjectType type) => type == JsonObjectType.None;
    }
}