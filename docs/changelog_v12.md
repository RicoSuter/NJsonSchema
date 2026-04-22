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

*(to be filled as PRs merge)*

---

## Migration guide (v11 → v12)

Intended as a running "how do I upgrade" companion. Each section is added as breaking changes land on the `v12` branch.

### System.Text.Json replaces Newtonsoft.Json in the core

The `NJsonSchema` core package no longer depends on `Newtonsoft.Json`. All serialization, deserialization, and validation go through `System.Text.Json` (STJ). If you rely on Newtonsoft.Json attributes (`[JsonProperty]`), contract resolvers, or `JToken`-based APIs, install the **`NJsonSchema.NewtonsoftJson`** package — it restores the legacy behavior by replacing the reflection/serialization services.

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
