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

- Keep property converters scoped to their property when they delegate to the normal serializer, including converter factories and nullable properties. Respect explicit null/default serialization contracts and `HandleNull` without changing the operation's default omission policy.
- Preserve unknown keywords that collide with ignored core schema metadata (including inherited reference bookkeeping), while retaining explicit derived/user ignores and registered property ignores.
- Forward both `JsonSchema.Validate(JsonNode)` overloads directly so ordinary error tokens retain caller identity and no source coordinates are invented. Customized string nodes use their serialized text for constraints, enum equality, and uniqueness, including converters that change a backing string.
- Restore quoted `"true"`/`"false"` handling for `additionalProperties` and `additionalItems` across schema dialects. Invalid non-null scalar values for these schema-or-boolean keywords now throw instead of silently leaving the constraint absent; literal defaults, examples, and extension strings remain unchanged.

- Preserve the cached public static `JsonSchema.ToolchainVersion` getter so existing getter calls and reflection keep working.

- Align sample schema generation from streams with lenient string input, including BOM-aware decoding and stream disposal. Parse ISO dates with the invariant Gregorian calendar so date validation and sample inference remain culture-independent, and preserve explicit null items in generated sample arrays.

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

The core `NJsonSchema` package uses System.Text.Json (STJ) for schema serialization and validation. Rebuild consumers for v12: this is a source migration with binary-incompatible signatures, not an assembly drop-in replacement. Use `JsonSchema.ToJson` / `FromJsonAsync` and the schema serialization APIs to retain dialect and reference handling.

`NJsonSchema.NewtonsoftJson` supplies Newtonsoft-aware reflection and schema generation, including `[JsonProperty]` and contract resolvers. Its settings and generator already existed in v11. Installing it does **not** restore direct `JsonConvert` serialization of STJ-annotated core schema models or the removed `JToken` validation overloads.

#### Public API and binary changes

| Area | v11 | v12 / migration |
|---|---|---|
| Indented output | `ToJson(Formatting.Indented)` | `ToJson(writeIndented: true)`; parameterless overload retained |
| Samples | `JToken ToSampleJson()` / `SampleJsonDataGenerator.Generate` | `JsonNode?` return type |
| Parsed validation | `Validate(JToken, ...)` | `Validate(JsonNode?, ...)`; string overloads retained |
| Validator subclasses | protected virtual `Validate(JToken, ...)` | Override with `JsonNode?` |
| Diagnostics | `ValidationError` constructor token and `Token` getter use `JToken?` | `object?`; constructor calls/getters require recompilation even when source converts to object |
| Child diagnostics | `ChildSchemaValidationError` / `MultiTypeValidationError` constructors take `JToken?` | `JsonNode?` |
| Formats | `IFormatValidator.IsValid(string, JTokenType)` and built-in implementations | `JsonValueKind`; Integer/Float become Number, Boolean becomes True/False; do not cast enum values |
| Resolver customization | `PropertyRenameAndIgnoreSerializerContractResolver` and its protected `CreateProperty` override | Removed; `SchemaSerializationConverter` is an STJ converter factory, not an inheritance-compatible replacement |
| Serializer factory/context | `CreateJsonSerializerContractResolver`, `CurrentSerializerSettings` | `CreateSchemaSerializationConverter`, `CurrentSerializerOptions` |
| Serialization helpers | `JsonSchemaSerialization.ToJson` resolver/Formatting parameters; string/stream `FromJson` and `FromJsonAsync` resolver parameters | `SchemaSerializationConverter?` and bool output formatting; async string overloads consolidate with optional cancellation |
| Reference APIs | Resolver arguments on `ResolveReferenceAsync`, `ResolveReferenceWithoutAppendAsync`, virtual `ResolveDocumentReference` | Removed; update overrides and callers |
| Paths/visitors | Resolver overloads on `JsonPathUtilities`, `JsonSchemaReferenceUtilities`; protected resolver constructors on both reference visitor bases | Removed or replaced by resolver-free overloads; parameterless visitor constructors remain |
| Discriminator | `IDictionary<string, JsonSchema> Mapping { get; }` | Existing getter retained; public setter added |

`JsonSchema.ToolchainVersion` retains its public static get-only property and cached value, now reporting STJ. Schema constructors and virtual `ActualSchema`, `ActualTypeSchema`, `Description`, `Parent`, and `IsNullable` contracts retain their signatures. Signature preservation alone does not establish downstream behavioral compatibility.

Core model and settings annotations/converters now use STJ; applications inspecting Newtonsoft metadata or supplying Newtonsoft converters must migrate those customizations. The `NJsonSchema.Annotations` attribute package itself is unchanged. `SchemaSerializationConverter` exposes `IgnoreProperty`, `RenameProperty`, `AddConverter`, and `IsPropertyIgnored`; its optional `ignoreEmptyCollections` constructor argument defaults to true. Supply STJ converters rather than Newtonsoft converters.

#### Supported settings and validation examples

These Newtonsoft generation APIs work in **both v11 and v12**, with a reference to `NJsonSchema.NewtonsoftJson`:

```csharp
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.NewtonsoftJson.Generation;

var settings = new NewtonsoftJsonSchemaGeneratorSettings
{
    SerializerSettings = new JsonSerializerSettings()
};
var schema = NewtonsoftJsonSchemaGenerator.FromType<Person>(settings);

public class Person
{
    [JsonProperty("display_name")]
    public string Name { get; set; } = "";
}
```

`JsonSchemaGeneratorSettings` is abstract and requires a reflection service; do not instantiate it or assume it has `SerializerSettings`. For STJ-aware generation, both versions support:

```csharp
var settings = new NJsonSchema.Generation.SystemTextJsonSchemaGeneratorSettings
{
    SerializerOptions = new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    }
};
var schema = NJsonSchema.JsonSchema.FromType<MyType>(settings);
```

For v12 validation and output:

```csharp
var compact = schema.ToJson(writeIndented: false);
var indented = schema.ToJson(writeIndented: true);
var errors = schema.Validate(System.Text.Json.Nodes.JsonNode.Parse(json));
// schema.Validate(json) remains supported and additionally supplies source locations.
```

#### Discriminator mapping

Mapping values are schemas, not strings. Existing `.Add()` calls work. Assignment retains the supplied dictionary instance (no copy or normalization); keep it non-null. Serialize a complete graph to obtain reference paths:

```csharp
var dog = new JsonSchema { Type = JsonObjectType.Object };
var root = new JsonSchema { Type = JsonObjectType.Object };
root.Definitions["Dog"] = dog;
root.DiscriminatorObject = new OpenApiDiscriminator
{
    PropertyName = "kind",
    Mapping = new Dictionary<string, JsonSchema>
    {
        ["dog"] = new JsonSchema { Reference = dog }
    }
};
var json = root.ToJson(); // mapping wire value is "#/definitions/Dog"
```

#### Runtime, validation, and wire contracts

- **Literal values:** ordinary objects materialize as `Dictionary<string, object?>`, arrays as `object?[]`, booleans as `bool`, null as null, and strings as strings, including ISO date strings. Numbers prefer `int`, then `long`, then a finite `double` only if its serialized spelling matches the input; otherwise they retain an independent `JsonElement`. There is no automatic decimal mapping. Support `JsonElement` when inspecting defaults, examples, enums, and extension values. Integer materialization does not promise lexical preservation. Schema-valued extensions follow separate schema inference; literal defaults/examples/enums remain literal even when they contain `type` or `properties`.
- **Exact equality and integers:** enum and uniqueItems comparisons use exact mathematical number equality, ignore object member order, and preserve array order and JSON type distinctions. `1`, `1.0`, and `1e0` compare equal; distinct large integers do not collapse through double rounding. Whole-valued exponent/decimal forms count as integers, while precise nonzero fractions do not. Arithmetic minimum/maximum/multipleOf checks still use constrained decimal/double arithmetic; this is not arbitrary-precision constraint validation. Extreme constrained exponents can still throw on .NET Framework. Unconstrained exact type checks avoid that conversion.
- **Dates:** literal dates remain strings. Sample-schema inference accepts supported ISO forms invariantly; culture-specific strings such as `10/12/2024` remain ordinary strings. Date validation uses the invariant Gregorian calendar, also repairing a preexisting non-Gregorian leap-date bug.
- **Input tolerance:** comments and trailing commas are supported. The syntax-aware lenient fallback handles supported single-quoted strings, unquoted keys, and non-breaking whitespace outside literals without rewriting literal contents. Quoted booleans are coerced only by typed contracts; custom converter precedence is preserved. This is not a promise to accept every malformed Newtonsoft input. Malformed JSON now generally raises `System.Text.Json.JsonException`; callback, reference, converter, and I/O failures retain their own contracts. Exception messages/positions can differ.
- **Streams:** schema and sample-schema stream entry points buffer through a BOM-aware reader and dispose the supplied stream, including failure paths. String and stream inputs share leniency. Account for ownership when reusing a caller stream.
- **Diagnostics:** `Validate(string)` supplies one-based line/UTF-16 character positions, including CRLF, bare CR, multibyte text, root null, and special property names. Caller-supplied nodes have no original source text and do not gain source coordinates. Public paths retain their existing display format and can be ambiguous for special keys; internal location identity disambiguates them. Property error display retains `"name": value` formatting.
- **Context behavior:** `JsonSchemaSerialization.IsWriting` keeps its getter signature but now reports true while writing and false while reading. The v11 baseline reported false in ToJson and true in FromJson. Custom callbacks relying on that inversion must change. Options, dialect, and state flow across awaits and nested operations restore their caller state after success, failure, or cancellation.
- **Preserved schema semantics:** dialect-specific `readOnly` (Swagger/OpenAPI) and `readonly` (JSON Schema) output, case-insensitive typed input, XML metadata, converter precedence, derived members, and enum-description aliases are restored contracts, not intentional data loss. Mixed/object vendor enum-description data retains its original alias and values. Property ordering alone is not a semantic break.

#### Public diagnostic access

`ValidationError.Token` is `object?`. Library errors may hold a `JsonNode`, null, or an internal property wrapper; constructor callers can supply other objects. `JsonPropertyToken` is internal and has no supported public Name/Value accessor. Use public error metadata and display output, inspecting caller-owned input when a property value is needed:

```csharp
Console.WriteLine($"{error.Property}: {error.Path}: {error.Token}");
if (error.Token is System.Text.Json.Nodes.JsonNode node)
{
    Console.WriteLine(node.ToJsonString());
}
```

#### Generated code, packages, and downstream migration

Generated C# still defaults to Newtonsoft.Json serialization; the core migration does not silently switch generated clients to STJ. Exact integral enum constants/defaults and custom enum naming are preserved, including large Int64 values; TypeScript still uses JavaScript Number and cannot represent every integer beyond 2^53 exactly. Restored readonly dictionary declarations retain each generator's existing accessor/initialization policy. Enum-description and readonly snapshot differences were audited semantically against v11 rather than accepted solely as reordered text.

Production target frameworks remain `netstandard2.0;net462;net8.0`; Annotations remains `netstandard2.0;net462`. Core removes its direct Newtonsoft.Json dependency, while the adapter retains it. STJ is a package dependency for older targets and comes from the framework on net8.0. Final package artifact/dependency auditing remains part of the release gate.

NSwag source migration and its full downstream build/tests are deliberately deferred to companion [NSwag #5355](https://github.com/RicoSuter/NSwag/pull/5355). That follow-up must cover callback references, discriminator mappings, parameter schema overrides, and generated clients against the exact NJsonSchema build. NJsonSchema-only verification does not establish downstream merge or release readiness.

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
