# Code Review: NJsonSchema — Newtonsoft.Json to System.Text.Json Migration

## Scope

Branch `feature/migrate-core-to-stj` — 96 files, ~6900 insertions / ~1700 deletions, plus 5 uncommitted simplify/optimization fixes.

---

## What Was Done Well

- Comprehensive attribute migration from `[JsonProperty]` to `[JsonPropertyName]` / `[JsonIgnore]` / `[JsonPropertyOrder]`
- `SchemaSerializationConverter` (`JsonConverterFactory`) cleanly replaces Newtonsoft's `PropertyRenameAndIgnoreSerializerContractResolver`
- Careful handling of `JsonElement` in setters for polymorphic raw properties (`DiscriminatorRaw`, `ExclusiveMinimumRaw`, `AdditionalPropertiesRaw`, etc.)
- `PostProcessExtensionData` correctly replicates the old `ExtensionDataDeserializationConverter` schema detection
- Line information reconstruction via `BuildLineInfoMap` in the validator
- Substantial new regression tests covering round-trips, schema type behaviors, validation, sample data generation
- Lenient JSON fallback (`FixLenientJson`) to handle non-standard JSON that Newtonsoft tolerated
- `PopulateTypeInfoResolver` for getter-only collection population matching Newtonsoft behavior
- `WithAttemptingUnquotedStringTypeDeserialization()` in YAML deserialization fixes boolean/number type loss
- `ConvertJsonElement` made internal for reuse in `CSharpValueGenerator`

---

## Critical Issues

### 1. `ToJson` has no exception safety — `IsWriting` never reset
**File:** `JsonSchemaSerialization.cs:88-104`

`IsWriting = true` is set but never reset (not even on success path). If serialization throws, all three `[ThreadStatic]` fields leak permanently on that thread. `FromJson` correctly uses `try/finally` — `ToJson` should too.

### 2. Newtonsoft.Json NuGet reference still in NJsonSchema.csproj
**File:** `NJsonSchema.csproj` line 18

The core purpose of the migration is removing this dependency. Only string-based `FullName` comparisons remain (no `using Newtonsoft` directives). Verify this can be removed.

### 3. `ApplyReverseRenamesRecursively` renames properties globally in ALL JSON objects
**File:** `SchemaSerializationConverter.cs:223-253`

The recursive rename traverses the **entire** JSON tree and renames e.g. `deprecated` → `x-deprecated`, `example` → `x-example` in every object — including non-schema objects. The old Newtonsoft `ContractResolver` applied renames per-type, not globally.

**Impact on NSwag:** `OpenApiOperation.IsDeprecated` (mapped to `[JsonPropertyName("deprecated")]`) is never populated because the key is renamed to `x-deprecated` before deserialization. All `[System.Obsolete]` annotations are dropped from generated code.

**Fix:** Make the rename type-aware or scope to the current level only (don't recurse into children that will be deserialized by their own converters).

---

## Major Issues

### 4. `FromJson<T>(Stream)` has no lenient JSON fallback
**File:** `JsonSchemaSerialization.cs:225-238`

The string overload catches `JsonException` and retries with `FixLenientJson`. The stream overload does not.

### 5. `BuildLineStartOffsets` uses character offsets vs byte offsets
**File:** `JsonSchemaValidator.cs:254-265`

Character-based indices from `text[i]` are compared against `Utf8JsonReader` byte offsets. Incorrect for multi-byte UTF-8 characters. Low practical impact (JSON Schema names are typically ASCII).

### 6. `IFormatValidator` breaking change — `JTokenType` → `JsonValueKind`
**File:** `IFormatValidator.cs:20`

Public interface method signature changed. External consumers with custom format validators get a compile-breaking change. Expected but should be documented.

### 7. `CurrentSchemaType` setter changed from `private` to `public`
**File:** `JsonSchemaSerialization.cs:65`

Exposes a `[ThreadStatic]` field to external consumers. Should remain `private set` or `internal set`.

---

## Minor Issues

- `DefaultSerializerOptions` fallback lacks full configuration (no `AllowTrailingCommas`, `NumberHandling`, etc.)
- `FixLenientJson` regex can match inside string values (mitigated by being fallback-only)
- TypeScript `readonly` property lost when using `readOnly` (camelCase) without explicit OpenApi3 schema type
- `Validate(JsonNode?)` re-serializes to string — performance concern for large documents
- `SampleJsonDataGenerator.JsonNullSentinelConverter.Read` over-advances reader (dead code but incorrect)
- `SampleJsonDataGenerator.Generate` returns C# null instead of JSON null sentinel for fallback case
- `ConvertJsonElement` returns `double` for non-integer numbers, losing precision vs `decimal`
- `ChildSchemaValidationError`/`MultiTypeValidationError` constructors accept `JsonNode?` but base `ValidationError` accepts `object?` — type mismatch prevents passing `JsonPropertyToken`

---

## Test Coverage Gaps

- **No tests for `FixLenientJson`** — 4 regex transformations with zero unit tests
- **No tests for `SchemaSerializationConverter` edge cases** — nested renames, conflicting renames, extension data with renamed properties
- **No tests for `Validate(JsonNode)` with custom settings** — case-insensitive test changed to string overload
- **No tests for `PostProcessExtensionData` false positives** — objects with `type`/`properties` keys that aren't schemas

---

## Resolved / Non-Issues

| Finding | Status | Notes |
|---------|--------|-------|
| `DeepClone()` in reverse rename | **REQUIRED** | Removing it breaks `PathItem_With_External_Ref` due to JsonNode parent tracking |
| `IsWriting` flag semantics | **CORRECT** | `true` during serialization, `false` during deserialization |
| `[ThreadStatic]` async hazard | **KNOWN** | Pre-existing from Newtonsoft era, out of scope |
| ThreadStatic stripped options cache | **ACCEPTABLE** | Per-generic-type cache is correct, slightly less cache-efficient |
| YAML `WithAttemptingUnquotedStringTypeDeserialization` | **NECESSARY** | Both NSwag and NJsonSchema need it |

---

## Priority Action Items

### Must Fix Before Merge
1. `ToJson` exception safety — wrap in `try/finally`, reset `IsWriting`
2. Global reverse rename — make type-aware or scope to current level
3. Remove Newtonsoft.Json PackageReference from core csproj

### Should Fix Before Release
4. `CurrentSchemaType` setter visibility
5. Document `IFormatValidator` breaking change

### Nice to Have
6. Tests for `FixLenientJson`
7. Tests for `SchemaSerializationConverter` nested renames
8. Stream overload lenient JSON fallback

### Future Schema Generation Improvements (post-merge)
9. `JsonIgnore(Condition = WhenWritingNull/WhenWritingDefault)` — currently only `Always` is handled; `WhenWritingNull` could map to nullability or optional status
10. `JsonNumberHandling` attribute support — `WriteAsString`/`AllowReadingFromString` not reflected in schema
11. `JsonDerivedType` / `JsonPolymorphic` support (net7.0+) — not used for discriminator/oneOf schema generation

---

## New / Deleted Files

| File | Status | Purpose |
|------|--------|---------|
| `Infrastructure/SchemaSerializationConverter.cs` | NEW | STJ converter factory replacing Newtonsoft contract resolver |
| `Validation/JsonPropertyToken.cs` | NEW | Wrapper for property name+value with JProperty-like ToString() |
| `Infrastructure/IgnoreEmptyCollectionsContractResolver.cs` | DELETED | Newtonsoft-specific |
| `Infrastructure/PropertyRenameAndIgnoreSerializerContractResolver.cs` | DELETED | Replaced by `SchemaSerializationConverter` |

---

## Test Changes

| Test | Change | Reason |
|------|--------|--------|
| `TypeToSchemaTests.When_converting_in_round_trip` | `JsonConvert` → `ToJson()`/`FromJsonAsync()` | Newtonsoft can't serialize `JsonSchema` without its converter |
| `LineInformationTest` (2nd test) | Uses `JsonNode.Parse` for no-line-info path | Matches original intent |
| `ObjectValidationTests.When_case_sensitive` | Assertion unchanged | `JsonPropertyToken` preserves format |
| `ArrayValidationTests` | Added `When_unique_items_has_mathematically_equal_numbers` | Tests number normalization |

Demo: `NJsonSchema.Demo/Program.cs` fully migrated from `JArray`/`JObject`/`JToken` to `JsonNode`/`JsonObject`/`JsonArray`.

---

## Known Remaining Limitations

1. **Indexer property guards:** `GetIndexParameters().Length == 0` guards in visitors/path utilities prevent `Namotion.Reflection.PropertyReader` crashes on indexer properties (e.g., `this[string key]` on dictionary types).

2. **Null collection property removal:** `RemoveNullCollectionProperties` strips `null` for getter-only collections before STJ deserialization (YAML `paths:` → `"paths": null`).

3. **YAML type preservation:** `WithAttemptingUnquotedStringTypeDeserialization()` in YamlDotNet prevents booleans/numbers becoming strings.

4. **Property ordering in path finder:** Settable properties processed before getter-only to ensure canonical ref paths.

5. **ThreadStatic cache:** `PropertyFilterConverter` uses single-slot `[ThreadStatic]` cache instead of `ConcurrentDictionary`. Cache misses on async thread switches cause re-creation but no correctness issues.

6. **`PopulateTypeInfoResolver`:** Extracted as `static readonly` to avoid recreation per serialization call.

---

## Future Work (out of scope for this PR)

These items were identified during review and should be investigated separately:

- **`JsonIgnore(Condition = WhenWritingNull/WhenWritingDefault)` mapping** — currently only `Always` is handled; `WhenWritingNull` could influence nullability or optional status in schema generation
- **`JsonNumberHandling` attribute support** — properties with `[JsonNumberHandling(WriteAsString)]` are not reflected in schema
- **`JsonDerivedType` / `JsonPolymorphic` support** — net7.0+ attributes not used for discriminator/oneOf schema generation

---

## Already Fixed During Review

| Item | Fix |
|------|-----|
| Newtonsoft.Json PackageReference in csproj | Removed (also cleaned up leftover `using Newtonsoft` in Yaml/CodeGeneration) |
| Indexer property crashes in visitors | Added `GetIndexParameters().Length == 0` guards |
| Value type property traversal | Added `IsValueType` skips in visitors |
| Null collection properties from YAML | Added `RemoveNullCollectionProperties` in converter |
| YAML boolean/number type loss | Added `WithAttemptingUnquotedStringTypeDeserialization()` |
| `PopulateTypeInfoResolver` recreated per call | Extracted as `static readonly` field |
| `ConvertJsonElement` duplication | Made `internal`, reused in `CSharpValueGenerator` |
| Unbounded `ConcurrentDictionary` cache | Replaced with `[ThreadStatic]` single-slot cache |
| ICollection empty check | Added fast-path `Count == 0` before enumerator fallback |
| Namotion.Reflection version | Bumped to 3.5.0 |

---

*Last updated: 2026-04-07*
*Branch: `feature/migrate-core-to-stj`*
*Tests: 1485 passed, 0 failed, 17 skipped*
