# NJsonSchema Migration Guide: Newtonsoft.Json to System.Text.Json

## Overview

Starting with this release, **NJsonSchema** core uses **System.Text.Json** instead of Newtonsoft.Json for all serialization, deserialization, and validation.

If you need Newtonsoft.Json integration (e.g., `[JsonProperty]` attributes or Newtonsoft contract resolvers), install **`NJsonSchema.NewtonsoftJson`**.

---

## Quick Start

### If you only use NJsonSchema core APIs

1. Update the `NJsonSchema` NuGet package
2. Remove your direct `Newtonsoft.Json` dependency if it was only pulled in by NJsonSchema
3. Apply the breaking changes listed below

### If you use Newtonsoft.Json attributes or settings

1. Update the `NJsonSchema` NuGet package
2. Install **`NJsonSchema.NewtonsoftJson`**
3. Replace `JsonSchemaGeneratorSettings` with `NewtonsoftJsonSchemaGeneratorSettings`
4. Replace `JsonSchemaGenerator` usage with `NewtonsoftJsonSchemaGenerator`

```csharp
// Before (implicit Newtonsoft support in core):
var settings = new JsonSchemaGeneratorSettings {
    SerializerSettings = new JsonSerializerSettings { ... }
};
var schema = JsonSchema.FromType<MyType>(settings);

// After (explicit Newtonsoft package):
var settings = new NewtonsoftJsonSchemaGeneratorSettings {
    SerializerSettings = new JsonSerializerSettings { ... }
};
var schema = NewtonsoftJsonSchemaGenerator.FromType<MyType>(settings);
```

---

## Breaking Changes

### 1. `ToJson(Formatting)` → `ToJson(bool writeIndented)`

```csharp
// Before
var json = schema.ToJson(Formatting.Indented);

// After
var json = schema.ToJson(writeIndented: true);
```

### 2. `ToSampleJson()` returns `JsonNode?` instead of `JToken`

```csharp
// Before
JToken sample = schema.ToSampleJson();

// After
JsonNode? sample = schema.ToSampleJson();
```

### 3. `Validate(JToken)` → `Validate(JsonNode?)`

```csharp
// Before
var token = JToken.Parse(json);
var errors = schema.Validate(token);

// After
var node = JsonNode.Parse(json);
var errors = schema.Validate(node);
// Or simply:
var errors = schema.Validate(json);
```

### 4. `ValidationError.Token` type: `JToken?` → `object?`

```csharp
// Token may be JsonNode, JsonPropertyToken, or null
var display = error.Token?.ToString();
```

### 5. `IFormatValidator.IsValid` parameter: `JTokenType` → `JsonValueKind`

```csharp
// Before
public bool IsValid(string value, JTokenType tokenType) { ... }

// After
public bool IsValid(string value, JsonValueKind tokenType) { ... }
```

**Type mapping:**

| `JTokenType` | `JsonValueKind` |
|---|---|
| `String` | `String` |
| `Integer` | `Number` |
| `Float` | `Number` |
| `Boolean` | `True` / `False` |
| `Null` | `Null` |
| `Object` | `Object` |
| `Array` | `Array` |

### 6. `IContractResolver` parameter removed from all public APIs

| Old | New |
|---|---|
| `JsonSchemaSerialization.FromJsonAsync(..., IContractResolver, ...)` | `FromJsonAsync(..., SchemaSerializationConverter?, ...)` |
| `JsonPathUtilities.GetJsonPath(obj, obj, IContractResolver)` | `GetJsonPath(obj, obj)` |
| `JsonSchemaReferenceUtilities.UpdateSchemaReferencesAsync(..., IContractResolver)` | `UpdateSchemaReferencesAsync(...)` |
| `JsonReferenceResolver.ResolveReferenceAsync(..., IContractResolver)` | `ResolveReferenceAsync(...)` |

### 7. `PropertyRenameAndIgnoreSerializerContractResolver` → `SchemaSerializationConverter`

```csharp
// Before
var resolver = JsonSchema.CreateJsonSerializerContractResolver(schemaType);

// After
var converter = JsonSchema.CreateSchemaSerializationConverter(schemaType);
```

### 8. Visitor base classes no longer accept `IContractResolver`

```csharp
// Before
class MyVisitor : JsonReferenceVisitorBase {
    public MyVisitor(IContractResolver resolver) : base(resolver) { }
}

// After
class MyVisitor : JsonReferenceVisitorBase {
    public MyVisitor() { }
}
```

### 9. Exception type for malformed JSON

`Validate(string)` now throws `System.Text.Json.JsonException` instead of `Newtonsoft.Json.JsonReaderException`.

### 10. `ChildSchemaValidationError` / `MultiTypeValidationError` constructors

The `token` parameter changed from `JToken?` to `JsonNode?`.

---

## Key Infrastructure Changes

### `$ref` Serialization (`JsonReferenceBase<T>`)
- `ReferencePath` changed from explicit interface implementation to `[JsonInclude] internal` property
- STJ cannot serialize explicit interface implementations (unlike Newtonsoft's `[JsonProperty]`)

### `SchemaSerializationConverter` (replaces `PropertyRenameAndIgnoreSerializerContractResolver`)
- New STJ `JsonConverterFactory` that handles property renaming and ignoring during serialization
- Supports base type inheritance for renames/ignores

### `[JsonInclude]` on Internal Properties
- All ~20 internal serialization properties in `JsonSchema.Serialization.cs` require `[JsonInclude]`
- STJ ignores non-public properties by default (Newtonsoft serialized them with `[JsonProperty]`)

### `JsonPropertyName` Resolution in Reference Resolver
- `JsonReferenceResolver` now uses `[JsonPropertyName]` attribute to match JSON path segments
- Previously matched by C# property name; now matches by JSON name (e.g., `"definitions"` not `"DefinitionsRaw"`)

### Extension Data Processing Order
- `PostProcessExtensionData` moved before `UpdateSchemaReferencesAsync` in deserialization
- Extension data values (stored as `JsonElement` during deserialization) must be converted before `$ref` resolution

### `OpenApiDiscriminator.Mapping`
- Changed from `{ get; }` to `{ get; set; }` — STJ needs a setter to assign deserialized values

---

## Behavioral Differences

### Lenient JSON Parsing
Newtonsoft tolerated non-standard JSON. The migration handles this via:
- `AllowTrailingCommas = true`
- `ReadCommentHandling = JsonCommentHandling.Skip`
- `FixLenientJson()` fallback for single quotes and unquoted property names

### DateTime in Extension Data
Newtonsoft auto-parsed ISO 8601 strings to `DateTime`. The migration preserves this via explicit `DateTime.TryParse` with `RoundtripKind` in `ConvertJsonElement`.

### Integer Types in Extension Data
Newtonsoft deserialized JSON integers as `long`. System.Text.Json produces `int` (if in range) or `long`. Code that cast extension data values to `long` may need `Convert.ToInt64()`.

### Number Precision
Extension data numeric values use `int` → `long` → `double` progression. No `decimal` support in extension data (schema properties like `Minimum`/`Maximum` still use `decimal`).

### JSON Null in `JsonObject`
`JsonObject["key"] = null` stores C# null; no STJ API for a non-null "JSON null" node. A `JsonNullSentinel` struct with custom converter is used in `SampleJsonDataGenerator` for max-recursion returns.

### Validation Error Token Format
Newtonsoft's `JProperty.ToString()` produced `"name": value`; STJ has no equivalent. A `JsonPropertyToken` wrapper class provides matching `ToString()` format.

### Number Equality in `uniqueItems`
STJ preserves textual representation (`1.0` vs `1`); Newtonsoft normalized numbers. `NormalizeJsonValue()` converts numbers to `double` with round-trip format for comparison.

### Line Information in Validation
Line/position reporting uses `Utf8JsonReader` byte offsets instead of Newtonsoft's `IJsonLineInfo`. Results should be equivalent for ASCII JSON.

### Property Ordering
JSON property ordering may differ slightly due to different serializer defaults. `[JsonPropertyOrder]` is used for schema properties.

### Newtonsoft Linq Type Handling
Newtonsoft type names (`JObject`/`JArray`/`JToken`) are kept alongside STJ equivalents in `ReflectionServiceBase` type detection, so `NJsonSchema.NewtonsoftJson` schema generation still works.

---

## Namespace Quick Reference

| Old (Newtonsoft) | New (System.Text.Json) |
|---|---|
| `using Newtonsoft.Json` | `using System.Text.Json` |
| `using Newtonsoft.Json.Linq` | `using System.Text.Json.Nodes` |
| `using Newtonsoft.Json.Serialization` | `using System.Text.Json.Serialization` |
| `JToken` | `JsonNode` |
| `JObject` | `JsonObject` |
| `JArray` | `JsonArray` |
| `JValue` | `JsonValue` |
| `JTokenType` | `JsonValueKind` |
| `Formatting.Indented` | `true` |
| `Formatting.None` | `false` |
| `IContractResolver` | `SchemaSerializationConverter` |

---

## NJsonSchema.NewtonsoftJson Package

Provides backward-compatible Newtonsoft.Json support:

- **`NewtonsoftJsonSchemaGenerator`** — generates schemas from types with `[JsonProperty]` attributes
- **`NewtonsoftJsonSchemaGeneratorSettings`** — accepts `JsonSerializerSettings` and Newtonsoft contract resolvers
- **`NewtonsoftJsonReflectionService`** — reflection service understanding Newtonsoft contracts

---

*Last updated: 2026-04-07*
*Branch: `feature/migrate-core-to-stj`*
