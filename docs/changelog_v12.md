# NJsonSchema v12 Changelog

Running record of changes on the `v12` branch and migration guidance for users upgrading from v11.

See [`plan_v12.md`](./plan_v12.md) for the v12 scope, branch model, and release plan.

---

## Unreleased

### Breaking changes

- **Set up v12 integration branch and CI triggers** (PR [#1924](https://github.com/RicoSuter/NJsonSchema/pull/1924)) — no user-facing impact; infrastructure only.
- **System.Text.Json replaces Newtonsoft.Json in the core** (PR [#1914](https://github.com/RicoSuter/NJsonSchema/pull/1914)). Multiple public-API changes — see [System.Text.Json replaces Newtonsoft.Json in the core](#systemtextjson-replaces-newtonsoftjson-in-the-core) in the migration guide below.

Planned (not yet merged — track via linked PRs):

- **JsonTypeInfo-based property discovery** for net8.0+ — PR [#1917](https://github.com/RicoSuter/NJsonSchema/pull/1917).
- **`SchemaType` enum expansion** — differentiate OpenAPI 3.0 from 3.1 (currently both map to `SchemaType.OpenApi3`).
- **Reference resolution fixes** — close the `$ref` sibling-keyword gaps documented in [`references.md`](./references.md) by generalizing the current `!IsEnumeration` wrapper-reference guard so other sibling keywords (`description`, `default`, `readOnly`, `title`, and `const` once it lands) are preserved on resolution.

### New features

*(to be filled as PRs merge)*

### Fixes

- Preserve validation source coordinates for colliding or escaped property names, null values, and nested errors without changing public diagnostic paths. Count CRLF and bare CR correctly, retain character positions for multibyte text, skip source lookup for valid documents, and scan error locations in linear time.

- Restore case-insensitive schema member input (including readonly declarations) without folding dictionary or vendor-extension keys. Restore free-form Newtonsoft `JObject`/`JToken` generation, preserving explicit type mappers and array handling.

- Generate integral enum constants and default member names from exact JSON numeric values, including decimal and exponent spellings and metadata flags. Match enum defaults by JSON equality and reuse declaration inputs for custom enum-name generators. This also repairs preexisting mixed-numeric default lookup mismatches; large C# integer values remain exact, while TypeScript retains JavaScript Number semantics.

- Preserve supported single-quoted and unquoted-key schema inputs without changing literal strings, escaped text, or non-breaking spaces within values. Coerce quoted booleans only through typed serialization contracts, preserve custom converter precedence, and parse quoted exclusive bounds invariantly instead of silently discarding them.

- Restore XML metadata deserialization, property converter precedence (including discriminator mappings), ignored-input filtering, and derived schema members in base-typed containers. Filtered serialization preserves runtime metadata customizations and explicit converter contracts.

- Preserve object-valued or mixed vendor enum-description metadata under its original alias while retaining the string-array API. Keep lazily materialized reference targets attached to their original containers, preserving shared child schemas, nested references, and literal normalization.

- Align collected references and emitted reference paths with configured property ignores and inherited renames. Ignored members are skipped before their getters are evaluated, including schema keywords and additional properties on converter-decorated dictionaries.

- Avoid unnecessary decimal/double conversion when numeric validation has no arithmetic constraints, so exact integer and number checks accept extreme JSON exponents on .NET Framework as well as modern .NET.

- Normalize embedded schemas throughout typed document graphs, including pattern properties, tuple items, dictionary keys, and nested extensions, while preserving literal defaults, examples, and enum values. Traverse and resolve generic-only reference dictionaries by key, support non-schema reference replacements, retain collection indices across nulls, and distinguish shared or equal-valued objects by reference identity.

- Preserve serialization options, dialect, and read/write state throughout asynchronous reference resolution and restore the caller context after nested operations, failures, and cancellation. Embedded schemas retain configured converters, including OpenAPI nullable conversion; independent concurrent loads keep their own operation context.

- Validate all CLR primitive numeric `JsonValue` backings consistently, including unsigned integers and single-precision values, and enforce numeric constraints on direct and generated nodes. Custom converter boolean and numeric scalars (including numeric enums and primitive converters overriding the serialized number) also obey JSON type and numeric constraints. Direct GUID, date/time, duration, URI, character, and customized object/array/null values validate using their serialized JSON shape, including nested values and string format/length constraints, without mutating caller nodes or reparsing whole documents.

- Compare `enum` and `uniqueItems` as JSON values: numbers use exact mathematical equality, objects ignore member order, arrays retain element order, and strings remain distinct from numbers and booleans. This fixes boolean and structured enum validation and prevents distinct large integers from being treated as duplicate items. CLR-backed `JsonValue` strings (including characters, GUIDs and dates) and customized object/array values compare using their serialized JSON representation without changing caller-owned nodes.

- Recognize integer JSON values exactly: whole-valued forms such as `1.0`, `1e0`, and large positive exponents are accepted; precise fractions and tiny nonzero fractions are rejected without floating point rounding. Non-finite CLR numeric nodes are rejected using System.Text.Json serialization rules.

- Preserve literal `default`, `example`, and `enum` objects when they contain schema-shaped keys such as `type` or `properties`. Preserve precise JSON numbers during deserialization and roundtrip serialization, including defaults consumed by C# numeric code generation.

---

## Migration guide (v11 → v12)

Intended as a running "how do I upgrade" companion. Each section is added as breaking changes land on the `v12` branch.

### System.Text.Json replaces Newtonsoft.Json in the core

The `NJsonSchema` core package no longer depends on `Newtonsoft.Json`. All serialization, deserialization, and validation go through `System.Text.Json` (STJ). If you rely on Newtonsoft.Json attributes (`[JsonProperty]`), contract resolvers, or `JToken`-based APIs, install the **`NJsonSchema.NewtonsoftJson`** package — it restores the legacy behavior by replacing the reflection/serialization services.

Numeric values in object-valued schema data retain `int`/`long` and lossless roundtripping `double` values where possible. Numbers whose original spelling cannot roundtrip through those mappings are retained as independent `JsonElement` values, including high-precision decimals and some exponent spellings. Consumers inspecting runtime types should support `JsonElement` rather than assuming every nonintegral number is a `double`.

#### Breaking API changes at a glance

| Area | v11 (Newtonsoft) | v12 (STJ) |
|---|---|---|
| Indented output | `schema.ToJson(Formatting.Indented)` | `schema.ToJson(writeIndented: true)` |
| Sample JSON output | `JToken ToSampleJson()` | `JsonNode? ToSampleJson()` |
| Validate a parsed document | `schema.Validate(JToken)` | `schema.Validate(JsonNode?)` |
| `ValidationError.Token` | `JToken?` | `object?` (can be `JsonNode`, `JsonPropertyToken`, or null) |
| `IFormatValidator.IsValid` parameter | `JTokenType` | `JsonValueKind` |
| Malformed-JSON exception | `Newtonsoft.Json.JsonReaderException` | `System.Text.Json.JsonException` |
| Property rename / ignore | `PropertyRenameAndIgnoreSerializerContractResolver` | `SchemaSerializationConverter` |
| Contract resolver parameter | `IContractResolver` on public APIs (`FromJsonAsync`, `JsonPathUtilities.GetJsonPath`, `JsonReferenceResolver.ResolveReferenceAsync`, `JsonReferenceVisitorBase` constructors, etc.) | Removed — replaced by `SchemaSerializationConverter?` where applicable |
| Visitor base constructors | `: base(IContractResolver)` | parameterless |
| Schema generator (Newtonsoft-aware) | `JsonSchemaGenerator` / `JsonSchemaGeneratorSettings` (with `SerializerSettings`) | `NewtonsoftJsonSchemaGenerator` / `NewtonsoftJsonSchemaGeneratorSettings` in the `NJsonSchema.NewtonsoftJson` package |
| `OpenApiDiscriminator.Mapping` | `{ get; }` | `{ get; set; }` — STJ needs a setter to deserialize |
| `ChildSchemaValidationError` / `MultiTypeValidationError` constructors | `JToken?` parameter | `JsonNode?` parameter |

#### Common migrations

**Indented `ToJson`:**

```csharp
// Before (v11)
var json = schema.ToJson(Formatting.Indented);

// After (v12)
var json = schema.ToJson(writeIndented: true);
```

**`Validate(JToken)`:**

```csharp
// Before (v11)
var token = JToken.Parse(json);
var errors = schema.Validate(token);

// After (v12) — pass the raw string or a System.Text.Json.Nodes.JsonNode
var errors = schema.Validate(json);
// or
var node = JsonNode.Parse(json);
var errors = schema.Validate(node);
```

**Removing `IContractResolver` parameters:**

```csharp
// Before (v11)
var schema = await JsonSchemaSerialization.FromJsonAsync<JsonSchema>(
    json, schemaType, documentPath, referenceResolverFactory, contractResolver);

// After (v12) — pass a SchemaSerializationConverter, or null for defaults
var converter = JsonSchema.CreateSchemaSerializationConverter(schemaType);
var schema = await JsonSchemaSerialization.FromJsonAsync<JsonSchema>(
    json, schemaType, documentPath, referenceResolverFactory, converter);
```

**Keeping Newtonsoft.Json generator behavior (`[JsonProperty]`, custom contract resolvers):**

```csharp
// Before (v11) — core package could consume Newtonsoft settings directly
var settings = new JsonSchemaGeneratorSettings {
    SerializerSettings = new JsonSerializerSettings { /* ... */ }
};
var schema = JsonSchema.FromType<MyType>(settings);

// After (v12) — install NJsonSchema.NewtonsoftJson and use the Newtonsoft-aware generator
var settings = new NewtonsoftJsonSchemaGeneratorSettings {
    SerializerSettings = new JsonSerializerSettings { /* ... */ }
};
var schema = NewtonsoftJsonSchemaGenerator.FromType<MyType>(settings);
```

#### Namespace quick reference

| Newtonsoft | System.Text.Json |
|---|---|
| `Newtonsoft.Json` | `System.Text.Json` |
| `Newtonsoft.Json.Linq` | `System.Text.Json.Nodes` |
| `Newtonsoft.Json.Serialization` | `System.Text.Json.Serialization` |
| `JToken` | `JsonNode` |
| `JObject` | `JsonObject` |
| `JArray` | `JsonArray` |
| `JValue` | `JsonValue` |
| `JTokenType` | `JsonValueKind` |
| `Formatting.Indented` | `true` (the `writeIndented` bool) |
| `Formatting.None` | `false` |
| `IContractResolver` | `SchemaSerializationConverter` |

#### Behavioral notes

- **Lenient JSON recovery.** Newtonsoft tolerated single-quoted strings, unquoted property names, non-breaking spaces, and stringified booleans. `FromJson` retries with `FixLenientJson` on the first `JsonException`, so the usual real-world dirty inputs still parse. `FromJson(Stream)` buffers to string before delegating, so this fallback applies to both overloads.
- **Extension-data types.** Extension-data values deserialize as `JsonElement` and are converted lazily: JSON integers prefer `int`, falling back to `long`, then `double` (no `decimal`). ISO-8601 date strings are auto-parsed to `DateTime` via `DateTime.TryParse` with `RoundtripKind`.
- **`uniqueItems` comparison.** STJ preserves the textual form (`1` and `1.0` are different nodes). NJsonSchema normalizes numeric values to `double` (round-trip format) before comparing, keeping v11 semantics.
- **Validation-error token format.** Newtonsoft's `JProperty.ToString()` emitted `"name": value`. Property-level errors now carry a `JsonPropertyToken` whose `ToString()` preserves that format, so error messages remain stable.
- **Line information.** `Utf8JsonReader` reports byte offsets; the public `ValidationError.LinePosition` is still a character count, converted via `Encoding.UTF8.GetCharCount` from the line start.

### `ValidationError.Token` type change

`ValidationError.Token` is now `object?` (v11: `JToken?`). It can hold a `JsonNode`, a `JsonPropertyToken` (for property-level errors like `NoAdditionalPropertiesAllowed`), or `null`. Use `Token?.ToString()` for display; for typed access, pattern-match:

```csharp
if (error.Token is JsonNode node)
{
    // inspect node.GetPath(), node.GetValueKind(), etc.
}
else if (error.Token is JsonPropertyToken property)
{
    // property.Name and property.Value
}
```

### `OpenApiDiscriminator.Mapping` now has a setter

`Mapping` changed from `{ get; }` to `{ get; set; }` because STJ needs a setter on properties it deserializes. Callers that read the collection are unaffected. Callers that previously wrote into it via `.Add()` still work; direct assignment (`discriminator.Mapping = new Dictionary<string, string>()`) is now also possible.

### `IFormatValidator.IsValid` parameter change

The `tokenType` parameter was retyped from Newtonsoft's `JTokenType` to STJ's `JsonValueKind`:

```csharp
// Before (v11)
public bool IsValid(string value, JTokenType tokenType) { /* ... */ }

// After (v12)
public bool IsValid(string value, JsonValueKind tokenType) { /* ... */ }
```

The values map directly:

| `JTokenType` | `JsonValueKind` |
|---|---|
| `String` | `String` |
| `Integer` / `Float` | `Number` |
| `Boolean` | `True` or `False` |
| `Null` | `Null` |
| `Object` | `Object` |
| `Array` | `Array` |

Note that STJ splits boolean into two distinct kinds; format validators that branched on "boolean" should check both `True` and `False` (or use `!= JsonValueKind.Undefined && != JsonValueKind.Null` patterns where applicable).

### `SchemaType` enum expansion

*(placeholder — to be filled when the enum change lands)*

### Reference resolution: siblings of `$ref` are now preserved

*(placeholder — to be filled when the reference-resolution fixes land)*

---

## Contributing

When merging a v12 PR that includes a user-visible change:

1. Add an entry under `Unreleased → Breaking changes` / `New features` / `Fixes`.
2. If it breaks v11 consumers, also add a section under **Migration guide** with a before/after example.
3. Keep entries concise; link to the merged PR for full detail.
