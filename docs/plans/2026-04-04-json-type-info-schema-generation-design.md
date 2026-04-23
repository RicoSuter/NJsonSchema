# Design: JsonTypeInfo-Based Schema Generation

## Goal

Enhance `SystemTextJsonReflectionService` to use `JsonTypeInfo`/`JsonPropertyInfo` contract metadata on net8.0+ for schema generation, enabling:

1. **Correctness with custom STJ configurations** -- Schemas reflect runtime contract customizations (renamed/removed/added properties via `IJsonTypeInfoResolver` modifiers)
2. **AOT compatibility** -- Source-generated `JsonSerializerContext` can drive schema generation without runtime reflection

On older TFMs (`netstandard2.0`, `net462`), the current attribute-based reflection implementation is used as-is.

---

## API Surface

### Settings

```csharp
public class SystemTextJsonSchemaGeneratorSettings : JsonSchemaGeneratorSettings
{
    public JsonSerializerOptions SerializerOptions { get; set; } = new();

#if NET8_0_OR_GREATER
    /// <summary>
    /// When true (default), falls back to reflection-based property discovery
    /// if JsonTypeInfo resolution fails. Set to false for strict AOT mode.
    /// </summary>
    public bool FallbackToReflection { get; set; } = true;
#endif
}
```

### Convenience Overloads (net8.0+ only)

```csharp
#if NET8_0_OR_GREATER
public static JsonSchema FromType<T>(JsonSerializerContext context) { ... }
public static JsonSchema FromType(Type type, JsonSerializerContext context) { ... }
#endif
```

These internally create `SystemTextJsonSchemaGeneratorSettings` with `SerializerOptions.TypeInfoResolver = context` and `FallbackToReflection = false`.

Existing overloads remain unchanged and work on all TFMs. On net8.0+, they try the `JsonTypeInfo` path first.

---

## Architecture

### Compile-Time Split

```csharp
public override void GenerateProperties(...)
{
#if NET8_0_OR_GREATER
    if (TryGeneratePropertiesFromTypeInfo(schema, contextualType, settings, schemaGenerator, schemaResolver))
        return;

    if (!settings.FallbackToReflection)
        throw new NotSupportedException($"Type '{contextualType.Type}' could not be resolved via JsonSerializerOptions. Configure a TypeInfoResolver or set FallbackToReflection = true.");
#endif

    GeneratePropertiesFromReflection(schema, contextualType, settings, schemaGenerator, schemaResolver);
}
```

- **net8.0 build**: Compiles both `JsonTypeInfo` path and reflection fallback
- **netstandard2.0 / net462 builds**: Only compiles the reflection path

### JsonTypeInfo Property Discovery (net8.0+)

```csharp
#if NET8_0_OR_GREATER
private bool TryGeneratePropertiesFromTypeInfo(
    JsonSchema schema, ContextualType contextualType,
    SystemTextJsonSchemaGeneratorSettings settings,
    JsonSchemaGenerator schemaGenerator, JsonSchemaResolver schemaResolver)
{
    JsonTypeInfo typeInfo;
    try
    {
        typeInfo = settings.SerializerOptions.GetTypeInfo(contextualType.Type);
    }
    catch (NotSupportedException)
    {
        return false;
    }

    if (typeInfo.Kind != JsonTypeInfoKind.Object)
        return false;

    foreach (var jsonProperty in typeInfo.Properties)
    {
        if (jsonProperty.IsExtensionData)
            continue;

        if (jsonProperty.ShouldSerialize != null && /* always excluded */)
            continue;

        var propertyName = jsonProperty.Name;
        var propertyType = jsonProperty.PropertyType;
        var isRequired = jsonProperty.IsRequired;
        var order = jsonProperty.Order;

        // Bridge to CLR member for NJsonSchema-specific attributes
        var accessorInfo = FindAccessorForJsonProperty(contextualType, jsonProperty);

        // ... build schema property using STJ metadata + reflection attributes
    }
    return true;
}
#endif
```

### CLR Member Bridge

```csharp
#if NET8_0_OR_GREATER
private ContextualAccessorInfo? FindAccessorForJsonProperty(
    ContextualType contextualType, JsonPropertyInfo jsonProperty)
{
    // AttributeProvider (net8.0+) gives us the underlying MemberInfo
    if (jsonProperty.AttributeProvider is MemberInfo memberInfo)
    {
        return contextualType.Properties
            .OfType<ContextualAccessorInfo>()
            .Concat(contextualType.Fields)
            .FirstOrDefault(a => a.MemberInfo == memberInfo);
    }

    // No CLR member (synthetic property from modifier) — no annotation enrichment
    return null;
}
#endif
```

When `AttributeProvider` is null (programmatically added properties):
- Property still appears in the schema (name, type, required from `JsonPropertyInfo`)
- No validation annotations, no nullability info, no `[JsonSchemaType]` overrides

### Enum / Converter Handling

```csharp
#if NET8_0_OR_GREATER
private bool IsStringEnumFromTypeInfo(JsonPropertyInfo jsonProperty,
    SystemTextJsonSchemaGeneratorSettings settings)
{
    var converter = jsonProperty.CustomConverter;
    if (converter != null)
    {
        var converterType = converter.GetType();
        return converterType.IsAssignableToTypeName(
            "JsonStringEnumConverter", TypeNameStyle.Name);
    }
    return false;
}
#endif
```

---

## Reflection Path Improvements (Both Paths)

These fixes apply to the reflection fallback and should also be checked on the `JsonTypeInfo` path:

1. **`JsonSerializerOptions.DefaultIgnoreCondition`** -- Respect the global setting. If `WhenWritingNull` is set, influence schema generation accordingly.

2. **`JsonSerializerOptions.IncludeFields` + `[JsonInclude]`** -- Fix interaction with private fields marked `[JsonInclude]`.

3. **Generic `JsonStringEnumConverter<TEnum>`** -- Ensure the AOT-friendly generic variant is detected alongside the non-generic `JsonStringEnumConverter`.

---

## Metadata Split

| Source | Provides |
|--------|----------|
| `JsonTypeInfo.Properties` (net8.0+) | Property discovery, names, ordering, required, ignore, custom converters |
| CLR reflection via `AttributeProvider` | Nullability, `[Range]`, `[StringLength]`, `[RegularExpression]`, `[JsonSchemaType]`, `[JsonSchema]`, `[DataMember]` |

---

## Testing Strategy

### Shared Tests (Both Paths)

Parameterized xUnit tests with `[Theory]` using a `useJsonTypeInfo` bool:

- Basic property discovery (names, types, ordering)
- `[JsonPropertyName]` rename
- `[JsonIgnore]` / `[JsonIgnore(Condition = ...)]`
- `[JsonRequired]` and `required` keyword members
- `[JsonPropertyOrder]` ordering
- `PropertyNamingPolicy` (camelCase, snake_case, custom)
- `JsonStringEnumConverter` and `JsonStringEnumConverter<T>` (global + attribute)
- `IncludeFields` / `[JsonInclude]` on fields
- `DefaultIgnoreCondition = WhenWritingNull`
- Nullable reference types / value types
- Validation attributes (`[Range]`, `[StringLength]`, `[RegularExpression]`)
- Inheritance / `FlattenInheritanceHierarchy`

### JsonTypeInfo-Only Tests (net8.0+)

- `DefaultJsonTypeInfoResolver` modifier that renames a property
- Modifier that removes/ignores a property
- Modifier that adds a synthetic property (no CLR member)
- Modifier that sets `IsRequired = true` at runtime
- Modifier that assigns `JsonStringEnumConverter` via `CustomConverter`
- Source-generated `JsonSerializerContext` (AOT path)
- `FallbackToReflection = false` with unknown type throws `NotSupportedException`
- `FallbackToReflection = true` with unknown type falls back silently
