# Migrate NJsonSchema Core from Newtonsoft.Json to System.Text.Json

## Goal

Remove `Newtonsoft.Json` from every project except `NJsonSchema.NewtonsoftJson`. The core library, code generation, and YAML packages use only `System.Text.Json` for schema model serialization, validation, and generation.

This is a **major breaking version** (v11).

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Target frameworks | Keep `netstandard2.0`, `net462`, `net8.0` | STJ NuGet package provides `JsonNode`/`JsonElement` on all targets. Use `#if NET8_0_OR_GREATER` for advanced features only. |
| Validation input type | `JsonNode` (replaces `JToken`) | Closest 1:1 replacement. Mutable, standalone, has `.Parent`. `JsonElement` overload can be added later. |
| Schema model attributes | Replace with STJ attributes directly | Clean break. `[JsonPropertyName]`, `[JsonIgnore]`, `[JsonConverter]`, `[JsonExtensionData]` from STJ. |
| Contract resolver replacement | Custom `SchemaSerializationConverter<T> : JsonConverter<T>` with same-shaped `IgnoreProperty()`/`RenameProperty()` API | Works on all TFMs. NSwag can add its own rules on top. |
| NSwag compatibility API | `SchemaSerializationConverter ConfigureJsonSerializerOptions(options, schemaType)` | Returns the converter so NSwag can call `IgnoreProperty()`/`RenameProperty()` on it. Replaces `CreateJsonSerializerContractResolver()`. |
| Schema generation approach | Keep attribute-based reflection (current `SystemTextJsonReflectionService`) | Already works on all TFMs. No `#if` needed. |
| `NJsonSchema.NewtonsoftJson` scope | Schema generation only | No backwards-compatible `JToken` validation overloads. Consumers use `Validate(string)` or convert themselves. |
| Serialization API | `JsonSchema.FromJsonAsync(string)` / `ToJson()` with optional `JsonSerializerOptions` parameter | Same shape as current API, STJ internally. |

## Breaking Changes

- `Validate()` accepts `JsonNode` instead of `JToken`
- `ValidationError` stores `JsonNode` instead of `JToken`
- Extension data dictionaries become `IDictionary<string, JsonNode?>` instead of `IDictionary<string, JToken?>`
- `SampleJsonDataGenerator` returns `JsonNode` instead of `JToken`
- Schema model uses STJ attributes (affects anyone serializing `JsonSchema` with Newtonsoft directly)
- `CreateJsonSerializerContractResolver()` replaced by `ConfigureJsonSerializerOptions()`
- `JsonSchemaSerialization` infrastructure uses STJ internally

## Scope

### In scope (Newtonsoft removed)

- `NJsonSchema` (core) — bulk of the work
- `NJsonSchema.CodeGeneration` — attribute swap
- `NJsonSchema.CodeGeneration.CSharp` — no runtime Newtonsoft dependency
- `NJsonSchema.CodeGeneration.TypeScript` — no runtime Newtonsoft dependency
- `NJsonSchema.Yaml` — replace `ExpandoObjectConverter` usage (1 method)

### Out of scope (keeps Newtonsoft)

- `NJsonSchema.NewtonsoftJson` — stays as opt-in for Newtonsoft-aware schema generation

### NSwag impact (separate migration)

NSwag coupling points and what NSwag must do:

| NSwag coupling point | Impact |
|---|---|
| `OpenApiParameter : JsonSchema` | Inherits attribute changes automatically |
| `OpenApiDocument : JsonExtensionObject` | Update extension data type to `JsonNode` |
| `OpenApiResponse : JsonReferenceBase<T>` | Inherits changes automatically |
| `JsonSchemaSerialization.ToJson()` / `FromJsonAsync()` | Same API, works transparently |
| `CreateJsonSerializerContractResolver()` | Migrate to `ConfigureJsonSerializerOptions()` |
| `OpenApiPathItemConverter` (Newtonsoft) | NSwag ports to STJ `JsonConverter<T>` |
| `[JsonConverter(typeof(StringEnumConverter))]` on enums | NSwag swaps to `JsonStringEnumConverter<T>` |
| `NSwagDocumentBase` uses `JObject.Parse()` | NSwag migrates to `JsonNode.Parse()` |
| `NSwag.Generation` references `NJsonSchema.NewtonsoftJson` | Still available, no change needed |

## Implementation Phases

### Phase 0 — Test hardening (prerequisite)

Add missing tests BEFORE any migration work to ensure no behavioral drift:

- Serialization round-trip tests: `JsonSchema` → JSON → `JsonSchema` → assert equality
- Property filtering/renaming tests per `SchemaType` (Swagger2, OpenAPI3, JsonSchema4)
- Extension data round-trip tests (vendor extensions survive serialization)
- Snapshot complex schema outputs with Verify
- Validation test coverage for all type-checking paths

### Phase 1 — Schema model attributes

- Replace `[JsonProperty("name")]` → `[JsonPropertyName("name")]`
- Replace `[JsonIgnore]` (Newtonsoft) → `[JsonIgnore]` (STJ)
- Replace `[JsonConverter]` (Newtonsoft) → `[JsonConverter]` (STJ)
- Replace `[JsonExtensionData]` (Newtonsoft) → `[JsonExtensionData]` (STJ)
- Change extension data type from `IDictionary<string, JToken?>` → `IDictionary<string, JsonNode?>`
- Build `SchemaSerializationConverter<T>` (replaces contract resolvers)
- Add `ConfigureJsonSerializerOptions(options, schemaType)` API
- Port custom converters to STJ

### Phase 2 — Serialization infrastructure

- Rewrite `JsonSchemaSerialization` to use `System.Text.Json`
- Port `JsonSchema.FromJsonAsync()` / `ToJson()` internals

### Phase 3 — Validation system

- Replace `JToken` → `JsonNode` throughout validator
- Update `ValidationError` and format validators
- Add `Validate(string json)` convenience overload

### Phase 4 — Sample data generation

- Replace `JObject`/`JArray`/`JValue` → `JsonObject`/`JsonArray`/`JsonValue`

### Phase 5 — Peripheral packages

- `NJsonSchema.CodeGeneration` — swap `[JsonIgnore]` attribute
- `NJsonSchema.Yaml` — replace `ExpandoObjectConverter` usage in `ToYaml()`

### Phase 6 — Remove Newtonsoft dependency

- Remove `Newtonsoft.Json` `PackageReference` from core `.csproj`
- Remove from `NJsonSchema.CodeGeneration` `.csproj`
- Remove from `NJsonSchema.Yaml` `.csproj`
- Verify clean build on all TFMs

### Phase 7 — Test updates

- Update test projects to use `JsonNode` instead of `JToken`
- Update Verify snapshots
- Full test pass

## Follow-up Work (separate from this migration)

- **Schema generation with `JsonTypeInfo`** — Enhance `SystemTextJsonReflectionService` to use `JsonTypeInfo`/`IJsonTypeInfoResolver` contract metadata on net8.0+ for more accurate schema generation. Fall back to attribute-based reflection on older TFMs.
- **`IJsonTypeInfoResolver`-based serialization** — Improve `SchemaSerializationConverter` to use `IJsonTypeInfoResolver` on net8.0+ instead of reflection-based property filtering.
- **`Validate(JsonElement)` overload** — Add a read-only validation path for performance. `JsonNode.Create(element)` makes this trivial.

## Technical Notes

### JToken → JsonNode mapping

| Newtonsoft | System.Text.Json |
|---|---|
| `JToken` | `JsonNode` |
| `JObject` | `JsonObject` |
| `JArray` | `JsonArray` |
| `JValue` | `JsonValue` |
| `JTokenType.String` | `node is JsonValue v` + type check |
| `JToken.Parse(json)` | `JsonNode.Parse(json)` |
| `jtoken.ToString()` | `node.ToJsonString()` |
| `JObject.FromObject(obj)` | `JsonSerializer.SerializeToNode(obj)` |

### Contract resolver → Custom converter mapping

| Newtonsoft pattern | STJ replacement |
|---|---|
| `DefaultContractResolver.CreateProperty()` | `SchemaSerializationConverter.Write()` with manual property filtering |
| `IgnoreProperty(type, name)` | Same API, backed by dictionary lookup in converter |
| `RenameProperty(type, old, new)` | Same API, backed by dictionary lookup in converter |
| `ShouldSerialize` callback for empty collections | Check in converter's `Write()` method |

### Multi-target conditional compilation

Use `#if NET8_0_OR_GREATER` only where needed (advanced STJ features). The core migration uses APIs available on all TFMs via the System.Text.Json NuGet package:
- `JsonNode`, `JsonObject`, `JsonArray`, `JsonValue`
- `JsonSerializer.Serialize/Deserialize`
- `JsonConverter<T>`, `Utf8JsonReader`, `Utf8JsonWriter`
- `[JsonPropertyName]`, `[JsonIgnore]`, `[JsonConverter]`, `[JsonExtensionData]`
