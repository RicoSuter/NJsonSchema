# NJsonSchema: System.Text.Json Migration Summary

## Overview

Migration of NJsonSchema core library from Newtonsoft.Json to System.Text.Json (STJ). The core `NJsonSchema` package no longer depends on Newtonsoft.Json for serialization. Newtonsoft.Json support is preserved in the `NJsonSchema.NewtonsoftJson` package.

**Branch:** `feature/migrate-core-to-stj`
**Baseline:** 519 passing tests on `master`
**Result:** 520 passing tests (519 original + 1 new), 0 failing, 7 skipped (same as baseline)
**Demo:** Exact match with master (248 passes, 10 fails, 8 exceptions)

---

## Key Infrastructure Changes

### 1. `$ref` Serialization (`JsonReferenceBase<T>`)
- `ReferencePath` changed from explicit interface implementation to `[JsonInclude] internal` property
- STJ cannot serialize explicit interface implementations (unlike Newtonsoft's `[JsonProperty]`)
- The `IJsonReferenceBase.ReferencePath` interface now delegates to the internal property

### 2. `SchemaSerializationConverter` (new, replaces `PropertyRenameAndIgnoreSerializerContractResolver`)
- STJ `JsonConverterFactory` that handles property renaming and ignoring during serialization
- Supports base type inheritance for renames/ignores (child types inherit parent's renames)
- Replaces the deleted Newtonsoft `IContractResolver` implementations

### 3. `[JsonInclude]` on Internal Properties
- All ~20 internal serialization properties in `JsonSchema.Serialization.cs` require `[JsonInclude]`
- STJ ignores non-public properties by default (Newtonsoft serialized them with `[JsonProperty]`)

### 4. `JsonPropertyName` Resolution in Reference Resolver
- `JsonReferenceResolver` now uses `[JsonPropertyName]` attribute to match JSON path segments
- Previously matched by C# property name; now matches by JSON name (e.g., `"definitions"` not `"DefinitionsRaw"`)

### 5. Extension Data Path Resolution
- `PostProcessExtensionData` moved before `UpdateSchemaReferencesAsync` in deserialization
- Extension data values (stored as `JsonElement` during deserialization) must be converted before `$ref` resolution can traverse them

### 6. `OpenApiDiscriminator.Mapping` Property
- Changed from `{ get; }` to `{ get; set; }`
- STJ's custom converter needs a setter to assign deserialized values (Newtonsoft could populate in-place)

---

## Behavioral Differences & Workarounds

### Lenient JSON Parsing
- **Problem:** Tests and users pass non-standard JSON (single quotes, unquoted property names)
- **Solution:** `FixLenientJson()` helper in `JsonSchemaSerialization` applies regex fixes on parse failure
- Single quotes → double quotes, unquoted identifiers → quoted property names

### DateTime Auto-Parsing in Extension Data
- **Problem:** Newtonsoft auto-converted ISO 8601 strings to `DateTime`; STJ keeps them as strings
- **Solution:** `DateTime.TryParse` with `RoundtripKind` in `ConvertJsonElement` preserves original behavior

### JSON Null Values in `JsonObject`
- **Problem:** `JsonObject["key"] = null` stores C# null; no STJ API for a non-null "JSON null" node
- **Solution:** `JsonNullSentinel` struct with custom `JsonConverter` that writes `null` — used in `SampleJsonDataGenerator` for max-recursion returns

### Validation Error Token Format
- **Problem:** Newtonsoft's `JProperty.ToString()` produced `"name": value`; STJ has no `JProperty` equivalent
- **Solution:** `JsonPropertyToken` wrapper class with matching `ToString()` format; `ValidationError.Token` changed from `JsonNode?` to `object?`

### Line Information in Validation
- **Problem:** `JsonNode` has no line/column tracking (Newtonsoft's `IJsonLineInfo` did)
- **Solution:** Full `Utf8JsonReader`-based line tracking implementation that pre-scans JSON and builds a path-to-position map

### Number Equality in `uniqueItems`
- **Problem:** STJ preserves textual representation (`1.0` vs `1`); Newtonsoft normalized numbers
- **Solution:** `NormalizeJsonValue()` converts numbers to `double` with round-trip format for comparison

### Newtonsoft Linq Type Handling (JObject/JArray/JToken)
- **Problem:** Migration replaced Newtonsoft type names with STJ equivalents in `ReflectionServiceBase`, breaking schema generation for Newtonsoft types used via `NJsonSchema.NewtonsoftJson`
- **Solution:** Added Newtonsoft type names back alongside STJ ones in type detection

### Duplicate JSON Property Names (JObject/JArray)
- **Problem:** `JObject`/`JArray` have multiple indexer properties all named `"Item"`, causing duplicate key errors
- **Solution:** Skip duplicates with `return;` instead of throwing in `NewtonsoftJsonReflectionService`

---

## Breaking API Changes

| Change | Old | New |
|--------|-----|-----|
| `ValidationError.Token` type | `JsonNode?` | `object?` |
| `ValidationError.HasLineInfo/LineNumber/LinePosition` | `private set` | `internal set` |
| `OpenApiDiscriminator.Mapping` | `{ get; }` | `{ get; set; }` |
| `IJsonReferenceBase.ReferencePath` | interface-only | interface + internal property |
| Deleted `IgnoreEmptyCollectionsContractResolver` | Newtonsoft `IContractResolver` | Removed |
| Deleted `PropertyRenameAndIgnoreSerializerContractResolver` | Newtonsoft `IContractResolver` | `SchemaSerializationConverter` |
| `JsonSchemaSerialization.FromJsonAsync` arg 5 | `IContractResolver` | `SchemaSerializationConverter?` |

---

## New Files

| File | Purpose |
|------|---------|
| `Infrastructure/SchemaSerializationConverter.cs` | STJ converter factory replacing Newtonsoft contract resolver |
| `Validation/JsonPropertyToken.cs` | Wrapper for property name+value with JProperty-like ToString() |

## Deleted Files

| File | Reason |
|------|--------|
| `Infrastructure/IgnoreEmptyCollectionsContractResolver.cs` | Newtonsoft-specific |
| `Infrastructure/PropertyRenameAndIgnoreSerializerContractResolver.cs` | Replaced by `SchemaSerializationConverter` |

---

## Test Changes

| Test | Change | Reason |
|------|--------|--------|
| `TypeToSchemaTests.When_converting_in_round_trip` | `JsonConvert` → `ToJson()`/`FromJsonAsync()` | Newtonsoft can't serialize `JsonSchema` without its converter |
| `LineInformationTest` (2nd test) | Uses `JsonNode.Parse` for no-line-info path | Matches original intent of testing with/without line info |
| `ObjectValidationTests.When_case_sensitive` | Assertion unchanged | `JsonPropertyToken` preserves original format |
| `ArrayValidationTests` | Added `When_unique_items_has_mathematically_equal_numbers` | Tests number normalization for uniqueItems |

## Demo Migration
- `NJsonSchema.Demo/Program.cs` fully migrated from `JArray`/`JObject`/`JToken` to `JsonNode`/`JsonObject`/`JsonArray`
- Uses `value.ToJsonString()` instead of `value.ToString(Formatting.None)` for proper JSON output

---

## Known Remaining Limitations

1. **Demo expected fails/exceptions:** The demo reports `expectedFails: 11, expectedExceptions: 14` but actual is `10/8`. This was already the case on master — the expected values in the demo code are outdated.

2. **Value type traversal guards:** Added `type.IsValueType` early returns in `JsonPathUtilities.FindJsonPaths` and both visitor bases to prevent `Namotion.Reflection.PropertyReader` crashes on value types like `Decimal`.

3. **Property ordering in path finder:** `JsonPathUtilities.FindJsonPaths` now orders settable properties before getter-only properties to ensure canonical ref paths when multiple properties return the same object.
