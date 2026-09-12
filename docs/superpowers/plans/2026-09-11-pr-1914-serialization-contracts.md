# PR #1914 Serialization Contracts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Repair S4, S5, S6, S8, S9, S10, G1, G2, and the additional G3 enum regression in PR #1914 while preserving supported schema wire contracts and generated declarations.

**Architecture:** Repair the existing property-filter converter and raw keyword readers, without replacing the serialization pipeline. Keep syntax recovery separate from typed keyword coercion, and retain the existing special-type generation branch for both STJ and Newtonsoft DOM types. Complete these three tasks sequentially after the value/context repair batches; re-read their changes before editing shared files.

**Tech Stack:** C#, System.Text.Json, Newtonsoft adapter, xUnit v3, Verify, existing CSharpCompiler and TypeScriptCompiler test helpers, `global.json` selects SDK 10.0.100 with `latestMinor` roll-forward (installed SDK 10.0.203). Use Release/net8.0 for the focused commands below; core also targets net9.0, while C#/TypeScript tests target net8.0 only on non-Windows. Windows additionally covers net472.

**Spec:** [PR #1914 stabilization design, Phase 3](../specs/2026-09-11-pr-1914-stabilization-design.md).

## Global Constraints

- Accept the major-version API and binary breaks required to replace Newtonsoft types and customization APIs with STJ equivalents. Inventory each actual signature change and provide a working migration example; this is not blanket approval for additional public API redesign.
- Preserve supported schema and OpenAPI wire contracts, including XML metadata, property converters, ignored fields, derived schema members, keyword names, and references. Ordering and insignificant number spelling may change; semantic values may not.
- Preserve correct validation and generated C#/TypeScript contracts. Existing snapshots are evidence to investigate, not authority to accept lost behavior.
- Exercise all three current `SchemaType` values with appropriate inputs. Do not expand `SchemaType`, implement new JSON Schema drafts, or absorb #1917 property discovery into this repair.
- Preserve existing Newtonsoft adapter support. Direct Newtonsoft serialization of STJ-annotated core objects is a separate compatibility limitation that must be documented accurately.
- Repairs land in PR #1914. NSwag source fixes and its full downstream suite belong to the existing NSwag STJ migration PR; read-only consumer inspection remains required here. Do not claim that substitute fixtures prove actual NSwag integration.
- Preserve supported leniency, including quoted exclusive bounds. Never silently discard recognized bounds or mutate literal strings. No global string-boolean regular expression, broad resolver rewrite, new public customization API, or additional Newtonsoft dependency in core.
- Use AAA comments, nullable-aware fixtures, and the repository's formatting. Update `docs/changelog_v12.md` with each user-visible repair. Execution commits are scoped to each task; this planning change does not execute code, tests, or commits.

## File map and execution baseline

| File | Responsibility / planned change |
| --- | --- |
| `src/NJsonSchema/JsonXmlObject.cs` | Add explicit STJ inclusion for five public properties with internal setters. |
| `src/NJsonSchema/Infrastructure/SchemaSerializationConverter.cs` | Honor property converters and runtime schema contracts; remove ignored input through the existing typed walk; use options-aware name matching. |
| `src/NJsonSchema/JsonSchema.Serialization.cs` | Register schema runtime dispatch if needed; parse string-valued exclusive bounds in their existing raw setters. Preserve dialect renames. |
| `src/NJsonSchema/Infrastructure/JsonSchemaSerialization.cs` | Case-insensitive schema input, bounded syntax recovery, typed boolean handling; retain earlier value/context repairs. |
| `src/NJsonSchema/Infrastructure/LenientJsonSyntaxNormalizer.cs` (new only if extracting the syntax scanner) | Internal syntax-only normalization; no schema semantics or public API. |
| `src/NJsonSchema.Tests/Serialization/SerializationContractRegressionTests.cs` (new) | S4/S5/S6/S9 public behavior and converter precedence fixtures. |
| `src/NJsonSchema.Tests/Serialization/LenientSchemaInputRegressionTests.cs` (new) | S8/S10 targeted coercion and literal-preservation tests. |
| `src/NJsonSchema.Tests/Serialization/SchemaSerializationConverterTests.cs` (existing, retain) | Existing typed rename/non-schema/vendor-data guards. |
| `src/NJsonSchema/Generation/JsonSchemaGenerator.cs` | Restore Newtonsoft DOM special case in `TryHandleSpecialTypes<TSchemaType>`. |
| `src/NJsonSchema/Generation/ReflectionServiceBase.cs` (inspect) | Existing exact full-name recognition of JToken/JObject/JArray; use same distinction. |
| `src/NJsonSchema.NewtonsoftJson/Generation/NewtonsoftJsonSchemaGenerator.cs` (inspect) | Adapter entry point for root/property generation regressions. |
| `src/NJsonSchema.Tests/Generation/NewtonsoftTokenContractTests.cs` (new) | G2 root/property/dialect and mapper-precedence tests. |
| `src/NJsonSchema.CodeGeneration.TypeScript.Tests/DictionaryTests.cs` | G1 parsed readonly dictionary regression and compilation. |
| `src/NJsonSchema.CodeGeneration.CSharp.Tests/DictionaryTests.cs` | Compile representative repaired dictionary contract. |
| `src/NJsonSchema.CodeGeneration.TypeScript.Tests/Snapshots/` | Only individually justified readonly snapshot corrections, if affected. |
| `docs/changelog_v12.md` | Fixes and any actual migration implications. |

Use symbol names rather than the review's old line numbers. Before execution inspect `git diff`, the approved spec, and the current implementations of `CreateSerializerOptions`, `PropertyFilterConverter<T>.Read/Write`, `TryHandleSpecialTypes`, and both exclusive-bound setters. The present filter writes declared property types and bypasses `JsonPropertyInfo.CustomConverter`; its read walk only reverses names. XML properties lack `JsonInclude`. `FixLenientJson` replaces NBSP and quoted booleans globally. The generator recognizes JsonNode but has lost the analogous JToken exclusion of JArray. These are starting observations, not permission to overwrite subsequent phase changes.

Read-only downstream check: locate the NSwag sibling (if `../NSwag` is absent, record that limitation), then search `src/NSwag.Core` for `SchemaSerializationConverter`, `IgnoreProperty`, `RenameProperty`, `JsonConverter`, `Mapping`, and `JsonSchema` subclasses. Inspect the actual discriminator-mapping property converter before finalizing the S5 fixture. Record the branch/head inspected in the task report. Do not alter NSwag or run its full suite here.

---

### Task 1: Restore XML, filtered-property, and derived-schema contracts (S4, S5, S6, S9)

**Files:** Modify `JsonXmlObject.cs`, `Infrastructure/SchemaSerializationConverter.cs`, and, if runtime dispatch needs schema registration, `JsonSchema.Serialization.cs`; create `Serialization/SerializationContractRegressionTests.cs` under core tests; update changelog.

**Interfaces:** Consume the existing `JsonSchemaSerialization.ToJson(object, SchemaType, SchemaSerializationConverter?, bool)` and `FromJson<T>(string, SchemaSerializationConverter?)`, plus `JsonSchema.CreateSchemaSerializationConverter(SchemaType)`. Preserve their signatures, `IgnoreProperty`, `RenameProperty`, and `AddConverter`. Produce corrected existing serialization behavior, without a new public interface.

- [x] Add the new test class and a theory for every dialect using the existing async resolver pattern from `SchemaSerializationConverterTests`. The essential XML input/assertion is:

```csharp
const string json = """{"xml":{"name":"record","namespace":"urn:records","prefix":"r","wrapped":true,"attribute":true}}""";
var converter = JsonSchema.CreateSchemaSerializationConverter(schemaType);
var schema = JsonSchemaSerialization.FromJson<JsonSchema>(json, converter)!;
Assert.Equal("record", schema.Xml!.Name);
Assert.Equal("urn:records", schema.Xml.Namespace);
Assert.Equal("r", schema.Xml.Prefix);
Assert.True(schema.Xml.Wrapped);
Assert.True(schema.Xml.Attribute);
var output = JsonSchemaSerialization.ToJson(schema, schemaType, converter, false);
Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json)!["xml"], JsonNode.Parse(output)!["xml"]));
```

Also read false values and absent metadata; ensure `ParentSchema` is never emitted. Keep internal setters internal.

- [x] Add property-converter fixtures in that class. A registered holder has `[JsonConverter(typeof(JsonStringEnumConverter<Mode>))] public Mode Mode { get; set; }`, where `Mode.Active` is nonzero. Register it with `converter.IgnoreProperty(typeof(Holder))`, serialize via `JsonSchemaSerialization.ToJson`, and assert `Mode` is the string `"Active"`, not its integer. Add a property converter over `IDictionary<string, JsonSchema>` that writes `{"pet":"#/definitions/Pet"}` from entries whose schemas have `ReferencePath`; verify the property writes reference strings rather than schema objects. Its `Read` uses `JsonDocument.ParseValue(ref reader)` and constructs entries with those `ReferencePath` strings. Compare property output to ordinary STJ serialization of the same fixture with the same explicit property converter. This is the NJsonSchema-level reproduction of the NSwag mapping pattern.

- [x] Add explicit converter precedence tests: property converter wins over an options converter and type attribute; `AddConverter` wins over the filter for the type it handles; a type attribute still handles an unoverridden type; a converter factory attached to a property is honored. Use distinct marker strings (`"property"`, `"options"`, `"type"`) from small `JsonConverter<T>` fixtures whose `Write` calls `writer.WriteStringValue(marker)`, and whose `Read` reads that string. Include a property-level converter returning JSON null and a renamed converted property. Verify the emitted name is renamed exactly once.

- [x] Add read-ignore tests with a holder containing integer `secret` initialized to 7 and integer `kept`. Register `IgnoreProperty(typeof(Holder), "secret")` and read `{"secret":{"incompatible":true},"kept":9}`. Assert no exception, `secret == 7`, and `kept == 9`. Repeat in a nested holder, list, and dictionary; cover inherited ignore rules and a property both renamed and ignored. Assert ignored keys do not spill into extension data. Keep a vendor extension containing a literal `secret` key intact.

- [x] Add a `DerivedSchema : JsonSchema` fixture with `[JsonPropertyName("custom_name")] public string Label { get; set; } = "kept"`, `[JsonIgnore] public string Hidden => "hidden"`, and the enum property converter. Serialize at root, `Definitions["Child"]`, `AllOf`, `Item`, and a custom declared-`JsonSchema` property. Assert each intended location contains `custom_name == "kept"`, no `Hidden`, and string enum output. Add a `[JsonInclude]` property with a nonpublic setter. Test resolver customization separately through the supported direct `JsonSerializer.Serialize` API with explicitly constructed options and `SchemaSerializationConverter`; `JsonSchemaSerialization.ToJson` has no options parameter. Use a generic schema fixture without dialect-dependent members for this direct-serializer test:

```csharp
var converter = new SchemaSerializationConverter();
converter.IgnoreProperty(typeof(JsonSchema));
var resolver = new DefaultJsonTypeInfoResolver();
resolver.Modifiers.Add(typeInfo =>
{
    if (typeInfo.Type == typeof(DerivedSchema))
    {
        var label = typeInfo.Properties.Single(property => property.Name == "custom_name");
        label.Name = "resolver_name";
        label.ShouldSerialize = (_, value) => Equals(value, "kept");
    }
});
var options = new JsonSerializerOptions { TypeInfoResolver = resolver };
options.Converters.Add(converter);
var root = new JsonSchema();
root.Definitions["Child"] = new DerivedSchema();
var output = JsonSerializer.Serialize(root, options);
var child = JsonNode.Parse(output)!["definitions"]!["Child"]!;
Assert.Equal("kept", child["resolver_name"]!.GetValue<string>());
Assert.Null(child["custom_name"]);
```

Repeat after setting `Label = "omit"` and assert `resolver_name` is absent, proving the modifier's `ShouldSerialize` is honored. Import `System.Text.Json.Serialization.Metadata` and `System.Linq` in the fixture; do not mutate internal defaults or introduce a new options overload. A custom converter on the declared base property must still win over runtime dispatch. Do not promise automatic deserialization into arbitrary subclasses without a discriminator/custom converter.

- [x] Run the new regressions red (retain failure output showing the actual missing contract):

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -c Release -f net8.0 --filter FullyQualifiedName~SerializationContractRegressionTests
```

- [x] Add `[JsonInclude]` to Name, Namespace, Prefix, Wrapped, and Attribute. Extend the existing typed reverse-rename walk to remove ignored property input before deserialization and before descending into its value. Match the effective wire name and its original name; inherit ignore rules, skip extension payloads, and use the same case policy as serializer options. Do not deserialize a value to discover it is ignored.

- [x] Repair filtered writing in place. Use runtime schema metadata for a value encountered through a declared schema type, while keeping the current getter, ShouldSerialize, ignore, rename, empty-collection, and extension-data rules. Ensure the filter actually receives nested schemas in all dialects (register `JsonSchema` with no ignored names if necessary). The key dispatch ordering is:

```csharp
private static void WriteConvertedProperty(
    Utf8JsonWriter writer, object? value, JsonPropertyInfo property,
    JsonSerializerOptions options)
{
    var propertyOptions = new JsonSerializerOptions(options);
    propertyOptions.Converters.Insert(0, property.CustomConverter!);
    JsonSerializer.Serialize(writer, value, property.PropertyType, propertyOptions);
}
```

Call this helper when `property.CustomConverter != null`, after writing the effective property name. Without a property converter, preserve explicit declared-type converter semantics before choosing runtime schema metadata. This initial helper deliberately favors correct behavior over caching; optimize only after proving isolation and precedence.

Implement a private property-write helper in the existing converter rather than a second serializer framework. Avoid casting `JsonTypeInfo<DerivedSchema>` to `JsonTypeInfo<JsonSchema>`: use nongeneric `JsonTypeInfo` when inspecting runtime metadata. Resolve a converter factory against the declared property type; never globally install a property converter for sibling properties. Cache per active options/property only if needed, with no mutable cross-operation options. Preserve recursion guards from the stripped-options implementation. If a declared type has a converter, invoke that converter before considering runtime schema metadata; type converters own their wire contract.

- [x] Run the red command green and existing converter/roundtrip/reference coverage:

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -c Release -f net8.0 --filter 'FullyQualifiedName~Serialization|FullyQualifiedName~Reference'
```

Investigate reference-path or snapshot differences introduced by registering the base schema. Keep Phase 2's name/visibility/reference agreement intact. Update changelog with the four restored contracts; review the diff and commit only Task 1 files as `fix: preserve filtered schema serialization contracts`.

### Task 2: Preserve lenient syntax without changing literal values (S8, S10)

**Files:** Modify `Infrastructure/JsonSchemaSerialization.cs` and `JsonSchema.Serialization.cs`; optionally extract internal `Infrastructure/LenientJsonSyntaxNormalizer.cs`; create `Serialization/LenientSchemaInputRegressionTests.cs`; update changelog.

**Interfaces:** Keep `FromJson<T>` string/stream signatures and `FixLenientJson(string)` callers stable. Consume Task 1's typed property rules. Keyword coercion is private to schema serialization; it must not change Phase 1's literal converter.

- [x] Add a failing theory for schema types and their nullable wire names (`x-nullable` for JsonSchema/Swagger2, `nullable` for OpenApi3). Use this semantic payload, substituting the nullable name:

```json
{"type":"string","x-nullable":"true","default":"true","x-example":"false","description":"a b","x-vendor":{"readOnly":"true","text":"a b"}}
```

Assert nullable becomes true, default/example remain strings, and the actual U+00A0 between `a` and `b` survives. Repeat with a literal default object containing `type`, `properties`, `readOnly`, nested arrays and the strings `"true"`/`"false"`; assert deep JSON equality on roundtrip. Use `example` for the OpenApi3 case. Include `"description":"literal {key: 'value'} and : \\\"true\\\""` as a negative control for syntax-like text inside strings.

- [x] Add syntax-only fixtures for `{type:'string', default:'true'}`, external NBSP whitespace, escaped apostrophes in single-quoted values, escaped double quotes/backslashes in ordinary strings, comments, and trailing commas. Assert their parsed values match equivalent strict JSON. Include nested arrays and malformed/unclosed strings; invalid syntax must throw rather than truncate. These tests cover schema parsing only; sample-input stream ownership/encoding work stays in Phase 4.

- [x] Add exclusive-minimum and exclusive-maximum theories with `"1.5"`, `"1e1"`, `"true"`, and `"false"` plus unquoted controls. Numeric forms assert decimal `ExclusiveMinimum/Maximum` equals 1.5m or 10m, emitted token is numeric, and validation at/beyond the bound agrees with its unquoted counterpart. Draft-4 boolean forms pair with `minimum:1` or `maximum:2`; assert `IsExclusiveMinimum/Maximum`, emitted semantics, and equality behavior. Run numeric forms as JSON Schema and OpenAPI 3.1-style schema contents; boolean forms in draft-4/Swagger2/OpenAPI 3.0 contexts. Invalid `"not-a-bound"` must throw instead of silently disappearing. Use invariant parsing even under `de-CH` culture and restore culture in finally.

- [x] Run red:

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -c Release -f net8.0 --filter FullyQualifiedName~LenientSchemaInputRegressionTests
```

- [x] Remove document-wide boolean coercion. Use narrowly installed bool/bool? read converters within schema serializer options, or the existing typed property walk for known bool members; do not coerce object/string-valued keywords. A bool converter accepts True/False tokens and the supported quoted strings via `bool.TryParse`, throws `JsonException` for anything else, and writes normal boolean tokens. Verify it does not supersede a property converter. Do not put this converter into general literal conversion or code-generator JSON options.

- [x] Extend both exclusive-bound raw setters to explicitly inspect JsonElement.String and apply the same bool/decimal branches already used for CLR strings:

```csharp
else if (element.ValueKind == JsonValueKind.String)
{
    var text = element.GetString();
    if (bool.TryParse(text, out var boolean))
    {
        IsExclusiveMinimum = boolean; // Maximum setter uses IsExclusiveMaximum.
    }
    else if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
    {
        ExclusiveMinimum = number; // Maximum setter uses ExclusiveMaximum.
    }
    else
    {
        throw new JsonException("Invalid exclusive bound.");
    }
}
```

Keep existing numeric precision/range boundaries; this repair does not redesign numeric storage. Factor only the duplicated bound parsing if it improves clarity. No string may be silently dropped.

- [x] Replace regex-based syntax replacement only as far as necessary with a small token-aware scanner. Track normal/double-quoted/single-quoted/comment state, escape state, and whether an object expects a property name. Outside strings, normalize NBSP and quote supported unquoted identifiers; inside a single-quoted token decode its escapes and re-encode through `JsonSerializer.Serialize(string)`; copy double-quoted token contents unchanged. Leave comments/trailing commas to existing STJ options. Reject incomplete tokens. Do not walk every object and infer a schema from its keys. Retain fallback-on-JsonException rather than eagerly rewriting strict JSON. A whole-document buffer already exists; a wholesale serializer/parser replacement is unnecessary.

- [x] Run green, then Task 1 and existing deserialization coverage:

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -c Release -f net8.0 --filter 'FullyQualifiedName~LenientSchemaInputRegressionTests|FullyQualifiedName~Serialization|FullyQualifiedName~Deserialization'
```

Update changelog to state preserved leniency and literal values, not stricter input policy. Review/commit only Task 2 files as `fix: scope lenient schema parsing to supported tokens`.

### Task 3: Restore readonly, enum, and Newtonsoft token contracts (G1, G2, G3)

**Files:** Modify `Infrastructure/JsonSchemaSerialization.cs`, `Infrastructure/SchemaSerializationConverter.cs`, `Generation/JsonSchemaGenerator.cs`; create `Generation/NewtonsoftTokenContractTests.cs`; extend TypeScript and CSharp `DictionaryTests.cs`; update affected individual snapshots and changelog.

**Interfaces:** Existing `JsonSchema.FromJsonAsync(string)`, `NewtonsoftJsonSchemaGenerator.FromType(Type, NewtonsoftJsonSchemaGeneratorSettings)`, `TypeScriptGenerator.GenerateFile(string)`, and `CSharpGenerator.GenerateFile(string)` remain unchanged. Consume Tasks 1–2's visibility, name, and coercion behavior. No new generator hook or adapter settings API.

- [x] Add readonly input theories (`readOnly`, `readonly`, `READONLY`) using string and dictionary properties and all dialects through the existing serializer entry points. Assert `schema.Properties["values"].IsReadOnly`, and that accepted spellings do not appear as stray extension data. Assert dictionary entry names `Foo` and `foo` stay distinct; case-insensitive matching is for CLR schema members, not JSON dictionary keys or vendor payloads.

- [x] Add this TypeScript regression in `DictionaryTests`, parameterizing the property spelling; copy existing imports and AAA layout:

```csharp
var schema = await JsonSchema.FromJsonAsync("""
{"type":"object","properties":{"values":{"type":"object","readOnly":true,"additionalProperties":{"type":"string"}}}}
""");
Assert.True(schema.Properties["values"].IsReadOnly);
var output = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
{
    TypeStyle = TypeScriptTypeStyle.Interface
}).GenerateFile("Container");
Assert.Contains("readonly values", output);
Assert.Contains("[key: string]: string", output);
TypeScriptCompiler.AssertCompile(output);
```

Cover class output where readonly is supported, and retain existing constructor/initialization behavior. Generate the same repaired schema using `new CSharpGenerator(schema).GenerateFile("Container")` in CSharp `DictionaryTests`; use `CSharpCompiler.AssertCompile(output)`. Do not impose a new C# setter policy; compare the existing baseline contract.

- [x] Reproduce the additional numeric consumer risk found during Phase 1: C# integer enum `InternalValue` uses `ToString()`, flag parsing omits retained `JsonElement`, and enum default lookup uses CLR `IndexOf`. Add compiled C#/TypeScript enum fixtures with `1e0`/`1.0`, named default members, flags, and representable long values. Verify independently specified initialized values. Normalize integral enum literals and default numeric member names using the shared exact helper (preserve names generated before the migration, including when `x-enumNames` is absent), and match defaults using JSON value equality where needed, without rounding or expanding huge exponents. Files additionally in scope: `src/NJsonSchema.CodeGeneration.CSharp/Models/EnumTemplateModel.cs`, `src/NJsonSchema.CodeGeneration.TypeScript/Models/EnumTemplateModel.cs`, `src/NJsonSchema.CodeGeneration/ValueGeneratorBase.cs`, and their enum tests. Record this as G3 if reproduced; it must be resolved before final verification.

- [x] Add adapter regressions with `typeof(Newtonsoft.Json.Linq.JObject)` and `typeof(Newtonsoft.Json.Linq.JToken)` as roots and as properties of a fixture. For each SchemaType create `new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = schemaType }`, call adapter `FromType`, and inspect parsed `ToJson` output plus in-memory `ActualSchema`. Assert no inferred token title, no reflected token members/definitions, no unintended array type; assert Swagger2 `AllowAdditionalProperties == false` in memory and the precise pre-migration wire representation. The review's `additionalProperties:false` observation must be checked separately from the dialect serializer, which may omit false on Swagger2 output. JSONSchema/OpenApi3 preserve the corresponding permissive baseline. Use JArray and JsonArray controls to retain array handling, JsonObject/JsonNode controls to retain the new STJ behavior, and a user TypeMapper for JObject to prove explicit mappers still win.

- [x] Run the new tests red:

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -c Release -f net8.0 --filter 'FullyQualifiedName~NewtonsoftTokenContractTests|FullyQualifiedName~SerializationContractRegressionTests'
dotnet test src/NJsonSchema.CodeGeneration.TypeScript.Tests/NJsonSchema.CodeGeneration.TypeScript.Tests.csproj -c Release -f net8.0 --filter FullyQualifiedName~DictionaryTests
```

- [x] Set `PropertyNameCaseInsensitive = true` in the existing schema options. Update typed reverse renames, ignore matching, and child-property lookup to consult that option with ordinal case-insensitive comparison. Resolve exact name matches first, then case-insensitive aliases, while preserving the existing original-versus-renamed collision behavior. Keep dictionary keys untouched. Setting the STJ option alone is insufficient because reverse renames execute before STJ deserialization.

- [x] Restore the Newtonsoft special-type branch after TypeMapper handling, alongside the current JsonNode check. Follow `ReflectionServiceBase`'s full-name approach so core has no Newtonsoft assembly reference:

```csharp
var originalType = contextualType.OriginalType;
var isNewtonsoftToken = originalType.IsAssignableToTypeName(
    "Newtonsoft.Json.Linq.JToken", TypeNameStyle.FullName);
var isNewtonsoftArray = originalType.IsAssignableToTypeName(
    "Newtonsoft.Json.Linq.JArray", TypeNameStyle.FullName);
// Include (isNewtonsoftToken && !isNewtonsoftArray) in the existing
// non-array special-type condition; retain Swagger2's false assignment
// and the early return that avoids ordinary title/property generation.
```

The repository already uses `TypeNameStyle.FullName` with `IsAssignableToTypeName`; this preserves subclass recognition without a core Newtonsoft dependency. Preserve all existing STJ and object cases. No special case belongs in the TypeScript template to compensate for an incorrectly loaded IsReadOnly value.

- [x] Run green and affected projects on the shared supported target:

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -c Release -f net8.0
dotnet test src/NJsonSchema.CodeGeneration.TypeScript.Tests/NJsonSchema.CodeGeneration.TypeScript.Tests.csproj -c Release -f net8.0
dotnet test src/NJsonSchema.CodeGeneration.CSharp.Tests/NJsonSchema.CodeGeneration.CSharp.Tests.csproj -c Release -f net8.0
```

At the phase/project gate also run core with `-c Release -f net9.0`; framework-only net472 coverage belongs to Windows CI. Review any changed Verify output structurally. Restore lost readonly declarations, and account for every remaining semantic change separately from ordering/numeric spelling. Do not bulk-accept snapshots. Update changelog and commit scoped files as `fix: restore readonly and Newtonsoft token generation contracts`.

## Phase acceptance and handoff

- [x] Record S4/S5/S6/S9 against Task 1 regressions, S8/S10 against Task 2, and G1/G2 against Task 3; report exact test commands and outcomes, not merely a green build.
- [x] Compare repaired JSON/XML and generated declarations with pre-migration `v12`/master behavior using the same fixtures. Record the exact baseline commit used; the review checkpoint is evidence, not an immutable execution baseline. Explain any intentional semantic difference and add a test/changelog note rather than blessing a snapshot.
- [x] Confirm no new public API signatures and no core Newtonsoft package reference; inspect downstream usages read-only and list the real discriminator-mapping/derived-type scenarios still pending NSwag integration.
- [x] Keep NSwag companion implementation/full suite deferred to its existing STJ PR. Phase 3 completion does not satisfy the final downstream merge gate. Complete the remaining roadmap phases and final NJsonSchema build/test/package/CI gates before declaring PR #1914 ready to merge.

Execution checkpoint: completed and independently reviewed through `7f4deec4`. Full core suites: net8 862 passed/7 skipped; net9 865 passed/7 skipped. C# 257 passed; TypeScript 134 passed. Baseline `18ba2ccf` contract probes and exact semantic snapshot audit completed. Windows execution and the final package/whole-PR gates follow; NSwag integration remains deferred.
