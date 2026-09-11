using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NJsonSchema.Infrastructure;

internal static class JsonObjectGraphUtilities
{
    internal static bool TryGetSerializedPropertyName(Type runtimeType, string originalJsonName, out string serializedName)
    {
        var converter = JsonSchemaSerialization.CurrentSerializerOptions?.Converters
            .OfType<SchemaSerializationConverter>().FirstOrDefault();
        serializedName = originalJsonName;
        if (converter?.IsPropertyIgnored(runtimeType, originalJsonName) == true)
        {
            return false;
        }

        var renames = converter?.GetMergedRenames(runtimeType);
        if (renames?.TryGetValue(originalJsonName, out var renamed) == true)
        {
            serializedName = renamed;
        }
        return true;
    }

    internal static bool TryGetDictionaryEntries(object value, out IReadOnlyList<DictionaryEntryAccessor> entries)
    {
        var snapshot = new List<DictionaryEntryAccessor>();
        if (value is IDictionary dictionary)
        {
            foreach (var key in dictionary.Keys.Cast<object>().ToArray())
            {
                snapshot.Add(new DictionaryEntryAccessor(key.ToString()!, dictionary[key], replacement =>
                {
                    if (replacement == null) dictionary.Remove(key);
                    else dictionary[key] = replacement;
                }));
            }
        }
        else
        {
            var dictionaryType = value.GetType().GetInterfaces().FirstOrDefault(type =>
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>) &&
                type.GetGenericArguments()[0] == typeof(string));
            if (dictionaryType == null)
            {
                entries = snapshot;
                return false;
            }

            // Reflect the interface so explicit implementations work too.
            var indexer = dictionaryType.GetProperty("Item")!;
            var remove = dictionaryType.GetMethod("Remove", new[] { typeof(string) })!;
            var keys = (IEnumerable)dictionaryType.GetProperty("Keys")!.GetValue(value)!;
            foreach (var key in keys.Cast<string>().ToArray())
            {
                snapshot.Add(new DictionaryEntryAccessor(key, indexer.GetValue(value, new object[] { key }), replacement =>
                {
                    if (replacement == null) remove.Invoke(value, new object[] { key });
                    else indexer.SetValue(value, replacement, new object[] { key });
                }));
            }
        }

        entries = snapshot;
        return true;
    }

    internal sealed class DictionaryEntryAccessor(string key, object? value, Action<object?> replaceOrRemove)
    {
        internal string Key { get; } = key;
        internal object? Value { get; } = value;
        internal Action<object?> ReplaceOrRemove { get; } = replaceOrRemove;
    }

    internal sealed class ReferenceIdentityComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceIdentityComparer Instance = new();
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
