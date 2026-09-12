# PR #1914 References and Serializer Context Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Execution is already authorized directly in PR #1914; no further design approval is required.

**Goal:** Repair S2, S3, S7, R1, R2, and R3 without adding public context APIs or replacing the serializer architecture.

**Architecture:** Retain the existing converters, resolver entry points, and schema visitor fast paths. Carry immutable operation state in a private `AsyncLocal` scope, preserve the exact options used by the loader, and introduce internal dictionary/property-contract helpers shared by postprocessing, visitors, and path discovery. Keep Phase 1 literal conversion separate from extension schema materialization.

**Tech Stack:** C#, System.Text.Json, SDK selected by global.json, xUnit v3; core tests target net8.0/net9.0 plus net472 on Windows.

**Spec:** [PR #1914 stabilization design, Phase 2](../specs/2026-09-11-pr-1914-stabilization-design.md).

## Global Constraints

- Accept the major-version API and binary breaks required to replace Newtonsoft types and customization APIs with STJ equivalents. Inventory each actual signature change and provide a working migration example; this is not blanket approval for additional public API redesign.
- Preserve literal JSON values in defaults, examples, enumeration values, and extension payloads. An object resembling a schema is still data when it occurs in a data-valued keyword.
- Preserve supported schema and OpenAPI wire contracts, including XML metadata, property converters, ignored fields, derived schema members, keyword names, and references. Ordering and insignificant number spelling may change; semantic values may not.
- Preserve correct validation and generated C#/TypeScript contracts. Existing snapshots are evidence to investigate, not authority to accept lost behavior.
- Exercise all three current `SchemaType` values with appropriate inputs. Do not expand `SchemaType`, implement new JSON Schema drafts, or absorb #1917 property discovery into this repair.
- Preserve existing Newtonsoft adapter support. Direct Newtonsoft serialization of STJ-annotated core objects is a separate compatibility limitation that must be documented accurately.
- Repairs land directly in PR #1914; NSwag companion fixes and its full downstream gate follow later in the existing NSwag STJ migration PR.
- Do not change `ActualSchema`, `ActualTypeSchema`, `Reference` sibling-keyword semantics, resolver virtual signatures, visitor constructors, or protected visitor signatures. Do not change public diagnostic/visitor path formats incidentally.
- Complete Phase 1 first. Preserve its literal conversion and numeric representation decisions when editing `JsonSchemaSerialization.cs`; do not reintroduce schema inference into default/example/enum conversion.
- Use AAA comments in tests, nullable-compatible code, no new dependencies, and the repository's warnings-as-errors and line-ending conventions. Add each user-visible fix to `docs/changelog_v12.md`.

## Source evidence and file map

Current `JsonSchemaSerialization` has three `[ThreadStatic]` properties. `FromJsonWithLoaderAsync` restores only options correctly, resets dialect to JsonSchema, does not restore writing mode, and recreates options with `converter: null` after the loader clears them. This loses custom converters even without a thread switch. `ResolveDocumentReferenceWithoutDereferencing` needs active options to materialize dictionary targets.

The postprocessing walk handles selected `JsonSchema` members only; it omits pattern properties, tuple items, dictionary keys, typed document children, and recursion into newly materialized extension schemas. Both visitors dispatch generic-only dictionaries as `IEnumerable`, then discard boxed `KeyValuePair` values. `JsonPathUtilities` filters converter ignores but does not apply `GetMergedRenames`; visitor reflection branches do neither. Converter writing checks ignores against the original JSON name before applying renames, including inherited configurations.

| File | Responsibility in this plan |
| --- | --- |
| `src/NJsonSchema/Infrastructure/JsonSchemaSerialization.cs` | Private operation context and complete scoped lifetime; postprocessing orchestration and literal normalization |
| `src/NJsonSchema/Infrastructure/JsonObjectGraphUtilities.cs` (new, internal) | Generic dictionary entry adapter and serialized property visibility/name helpers; no public types |
| `src/NJsonSchema/Infrastructure/SchemaSerializationConverter.cs` | Reuse existing `IsPropertyIgnored`/`GetMergedRenames`; only add an internal helper if needed to avoid duplicate precedence logic |
| `src/NJsonSchema/Visitors/JsonReferenceVisitorBase.cs` | Generic dictionary traversal/replacement, write-visible property filtering |
| `src/NJsonSchema/Visitors/AsyncJsonReferenceVisitorBase.cs` | Equivalent async dictionary traversal and configured ignore filtering |
| `src/NJsonSchema/JsonReferenceResolver.cs` | Generic dictionary lookup/materialization with active options; retain virtual contracts |
| `src/NJsonSchema/JsonPathUtilities.cs` | Generic dictionary keys and converter-aware serialized property paths |
| `src/NJsonSchema/JsonSchemaReferenceUtilities.cs` | Check unchanged two-pass replacement and collection behavior; change only if required to use internal helpers |
| `src/NJsonSchema.Tests/Infrastructure/JsonSchemaSerializationTests.cs` | Existing exception restoration and null-options tests; retain their contract |
| `src/NJsonSchema.Tests/Infrastructure/SerializationContextTests.cs` (new) | Forced async suspension, exact options identity, nested scope and cleanup tests |
| `src/NJsonSchema.Tests/References/ReferenceGraphTraversalTests.cs` (new) | Complete graph materialization and generic-only dictionary public regressions |
| `src/NJsonSchema.Tests/References/ReferencePathContractTests.cs` (new) | Write-side renamed paths and ignored dangling references |
| `src/NJsonSchema.Tests/Schema/JsonPathUtilitiesGetJsonPathTests.cs` | Existing path behavior coverage to retain |
| `docs/changelog_v12.md` | Context, embedded schema, dictionary, and reference-path fixes |

Read `CLAUDE.md`, `docs/references.md`, and the Phase 1 plan before execution. Planning-time downstream inspection found `/Users/ricosuter/Projects/GitHub/NSwag`; the expected `../NSwag` does not exist from this worktree. That checkout is Newtonsoft-based, so it is usage evidence, not the STJ integration gate: `OpenApiCallback` implements only `IDictionary<string, OpenApiPathItem>` and also implements `IJsonReference`; `OpenApiDocument.Serialization.cs` ignores `OpenApiOperation.callbacks` for Swagger2; `OpenApiParameter` overrides `ActualSchema`. Read-only `git show origin/feature/migrate-core-to-stj` at `edd1a7c6d7883240d212788a9f0f64ed0a2da554` confirms the same generic-only callback and Swagger2 ignore rules on the actual STJ companion branch. Its document converter also registers `OpenApiParameterJsonConverter` via `AddConverter`, making preservation of the exact options operationally necessary. Do not build the Newtonsoft checkout and call it downstream migration validation; leave the user checkout unchanged.

## Task 1: Keep exact serialization context alive across resolution (S2, S7)

**Files:** Modify `JsonSchemaSerialization.cs`, existing `JsonSchemaSerializationTests.cs`, changelog. Create `SerializationContextTests.cs`.

**Interfaces:** Preserve public `CurrentSchemaType`, `CurrentSerializerOptions`, `IsWriting`, `ToJson`, both `FromJson` and both `FromJsonAsync` signatures. Change only private loader plumbing. Later tasks consume these public getters internally; no context parameters are added to resolver/visitor APIs.

- [x] Add a genuinely suspended resolver test using an incomplete `TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)` and a separate entered signal. Do not use a completed task or rely on `Task.Delay`/thread IDs. Use this fixture input so the second reference still requires dictionary target materialization (it intentionally has no `type` or `properties`):

```csharp
const string json = """
{
  "allOf": [
    { "$ref": "https://example.test/external.json" },
    { "$ref": "#/x-schema" }
  ],
  "x-schema": { "title": "local" }
}
""";
```

Define a nested `SuspendingResolver : JsonReferenceResolver` with constructor `(JsonSchema root, TaskCompletionSource<bool> entered, Task release)` calling `base(new JsonSchemaAppender(root, new DefaultTypeNameGenerator()))`. Override the existing `ResolveUrlReferenceAsync(string url, CancellationToken cancellationToken = default)`:

```csharp
var expectedOptions = JsonSchemaSerialization.CurrentSerializerOptions;
var expectedDialect = JsonSchemaSerialization.CurrentSchemaType;
entered.TrySetResult(true);
await release.ConfigureAwait(false);
cancellationToken.ThrowIfCancellationRequested();
Assert.Same(expectedOptions, JsonSchemaSerialization.CurrentSerializerOptions);
Assert.Equal(expectedDialect, JsonSchemaSerialization.CurrentSchemaType);
Assert.False(JsonSchemaSerialization.IsWriting);
return new JsonSchema { Type = JsonObjectType.String };
```

Start via `JsonSchemaSerialization.FromJsonAsync<JsonSchema>(json, dialect, null, root => new SuspendingResolver(root, entered, release.Task), converter)`. Await entered, assert load incomplete, release, await load; assert `schema.AllOf.Last().ActualSchema.Title == "local"`. Bound signal waits with `Task.WhenAny(signal, Task.Delay(TimeSpan.FromSeconds(10)))` and assert the returned task is `signal`; release gates in `finally` so failures do not strand tasks. This works on net472 too.

- [x] Add S7 regression with active `JsonSchema.CreateSchemaSerializationConverter(SchemaType.OpenApi3)` and assert model identity/type and `IsNullableRaw`, not only `IsNullable` (the latter can hide lost conversion by consulting extension data):

```csharp
var schema = await JsonSchemaSerialization.FromJsonAsync<JsonSchema>(
    """{"x-model":{"type":"string","nullable":true}}""",
    SchemaType.OpenApi3, null,
    JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator()),
    JsonSchema.CreateSchemaSerializationConverter(SchemaType.OpenApi3));
var model = Assert.IsType<JsonSchema>(schema.ExtensionData["x-model"]);
Assert.True(model.IsNullableRaw);
Assert.False(model.ExtensionData?.ContainsKey("nullable") == true);
```

Also register an additional `JsonConverter<string>` whose `Read` returns `"converted:" + reader.GetString()` and whose `Write` writes the string unchanged. Put `"title":"nested"` inside `x-model`; assert `Title == "converted:nested"`. Repeat with schema/Swagger2 inputs using `x-nullable`, avoiding OpenAPI-only keyword assumptions.

- [x] Add isolation/cleanup cases: start two independently gated loads with different dialects/converters, resume in reverse order, assert each resolver's options identity before/after await and each embedded title/nullability; perform a nested OpenApi3 load inside a Swagger2 resolver and assert outer options/dialect/writing mode are restored; nest synchronous `FromJson` and `ToJson` while inside that resolver and assert restoration. Cover success, malformed input, throwing converter, throwing resolver, cancellation before load, and cancellation after suspension. Existing external resolver wraps exceptions: accept its current wrapper and inspect the inner cancellation exception rather than broadening cancellation behavior here. After each call assert all three getters equal their captured caller values. Rename the existing thread-static test/comment to describe operation state while retaining its assertions.

- [x] Run red (both new fixture and existing state tests):

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -f net9.0 --filter 'FullyQualifiedName~SerializationContextTests|FullyQualifiedName~JsonSchemaSerializationTests'
```

Expected: deterministic context/options or materialization assertion failures; compilation/setup failures do not establish the regression.

- [x] Implement a private immutable context and balanced push/restore. Use a sealed class with get-only properties (compatible with all library targets), not a mutable holder shared across execution contexts. The core shape is:

```csharp
private static readonly AsyncLocal<SerializationContext?> CurrentContext = new();
public static SchemaType CurrentSchemaType => CurrentContext.Value?.SchemaType ?? SchemaType.JsonSchema;
public static JsonSerializerOptions? CurrentSerializerOptions => CurrentContext.Value?.Options;
public static bool IsWriting => CurrentContext.Value?.IsWriting ?? false;
```

`SerializationContext` has constructor `(SchemaType schemaType, JsonSerializerOptions options, bool isWriting)` and the corresponding get-only properties. In each owning method save `var previous = CurrentContext.Value;`, assign a newly constructed context, execute inside `try`, and restore `CurrentContext.Value = previous` in `finally`. Never mutate a context's members. Remove `[ThreadStatic]`; these properties had private setters, so external read APIs remain unchanged.

- [x] Create options once in each async public entry and pass them to a private loader that deserializes with those options without pushing another scope. Change private `FromJsonWithLoaderAsync` to accept the exact options; establish its scope before loading, and keep it through document-path assignment, resolver factory, postprocessing, and the awaited reference update. Extract the existing string deserialize/lenient fallback into a private helper accepting options; preserve its behavior. Synchronous public `FromJson` pushes a read scope using the current dialect, and restores all state. Stream buffering/ownership stays unchanged. `ToJson` uses the same balanced pattern around path collection and writing. Do not recreate default options after loading.

- [x] Run the red command again, expecting all green. Add the context/nullability fixes under Unreleased/Fixes and commit the task's actual files with `fix: preserve serialization context across asynchronous resolution`.

**Tradeoff:** Explicit context plumbing is architecturally clear, but getters on existing model types and virtual resolver overrides already depend on ambient state; replacing that contract here would require API or behavioral changes throughout NSwag. A private immutable `AsyncLocal` scope is a bounded compatibility repair. It flows across `ConfigureAwait(false)` and supports nested restoration; it does not make the same mutable schema/resolver/options safe for concurrent mutation. Tests use separate operations and preconfigured converters. Do not suppress execution-context flow or introduce new public overloads.

## Task 2: Traverse and normalize the complete reference graph (S3, R1)

**Files:** Create internal `JsonObjectGraphUtilities.cs` and `ReferenceGraphTraversalTests.cs`; modify postprocessing, both visitors, resolver, path utilities, and changelog.

**Interfaces:** Internal helper contract: `TryGetDictionaryEntries(object value, out IReadOnlyList<DictionaryEntryAccessor> entries)`; internal entry class with `string Key`, `object? Value`, and `Action<object?> ReplaceOrRemove`. Entries are snapshots. Keep these types internal and use existing visitor replacement delegates. Null replacement means removal, matching current visitors; literal normalization assigns null through its own dictionary setter and must not accidentally delete literal null data. Task 3 adds property-contract helpers in the same internal utility.

- [x] Add parameterized public-load regression inputs with a property reference `"properties":{"use":{"$ref":"TARGET"}}`, replacing TARGET with each row. Assert `Properties["use"].ActualSchema.Type == JsonObjectType.String` and the referenced object's `Default` has the same Phase 1 CLR representation as a root `default:7`.

| Additional root JSON members | TARGET |
| --- | --- |
| `"patternProperties":{"x":{"x-model":{"type":"string","default":7}}}` | `#/patternProperties/x/x-model` |
| `"items":[{"x-model":{"type":"string","default":7}}]` | `#/items/0/x-model` |
| `"x-dictionaryKey":{"x-model":{"type":"string","default":7}}` | `#/x-dictionaryKey/x-model` |
| `"x-model":{"type":"object","x-inner":{"type":"string","default":7}}` | `#/x-model/x-inner` |

Also put `default:{"type":"foo","properties":{"a":1}}`, matching `x-example`/OpenAPI `example`, and an object enum at these positions; assert parsed serialized payload equality using `JsonNode.DeepEquals`. This ensures broader traversal does not undo S1. Exercise all three `SchemaType` values with proper keyword names.

- [x] Add a typed non-schema document root using the following fixture and loader; do not gate postprocessing on the root implementing `IJsonExtensionObject`:

```csharp
public sealed class TypedDocument
{
    [JsonPropertyName("child")]
    public JsonSchema Child { get; set; }
}
var document = await JsonSchemaSerialization.FromJsonAsync<TypedDocument>(
    """{"child":{"x-model":{"type":"string"},"properties":{"use":{"$ref":"#/child/x-model"}}}}""",
    SchemaType.JsonSchema, null,
    root => new JsonReferenceResolver(new JsonSchemaAppender(root, new DefaultTypeNameGenerator())),
    JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema));
Assert.Equal(JsonObjectType.String, document.Child.Properties["use"].ActualSchema.Type);
```

Add extension arrays/dictionaries containing materialized schemas and a programmatically cyclic/repeated graph; traversal terminates and shared schema objects remain shared. A graph node with equal `Equals` values but different references must not hide the second node.

- [x] Add a generic-only dictionary fixture using `Dictionary<string,T>` composition implementing every `IDictionary<string,T>` member, forwarding to the backing dictionary; explicitly forward nongeneric `IEnumerable.GetEnumerator()` to the generic enumerator. Do not inherit `Dictionary` (it also implements nongeneric `IDictionary` and would miss R1). Assert `fixture is System.Collections.IDictionary` is false. Test both public utilities:

```csharp
var target = new JsonSchema { Type = JsonObjectType.String };
var reference = new JsonSchema();
((IJsonReference)reference).ReferencePath = "#/components/schemas/Value";
var callbacks = new GenericOnlyDictionary<JsonSchema> { ["onEvent"] = reference };
var root = new
{
    components = new { schemas = new Dictionary<string, JsonSchema> { ["Value"] = target } },
    callbacks
};
var resolver = new JsonReferenceResolver(new JsonSchemaAppender(root, new DefaultTypeNameGenerator()));
await JsonSchemaReferenceUtilities.UpdateSchemaReferencesAsync(root, resolver);
Assert.Same(target, reference.Reference);
JsonSchemaReferenceUtilities.UpdateSchemaReferencePaths(root);
Assert.Equal("#/components/schemas/Value", ((IJsonReference)reference).ReferencePath);
```

Test lookup into a generic-only dictionary (`#/callbacks/onEvent`), direct path discovery by key (not numeric index), and replacement/removal through small derived sync/async visitors returning a replacement or null from `VisitJsonReference[Async]`. Include non-JsonSchema values implementing `IJsonReference` so replacement does not cast everything to `JsonSchema`. Preserve callback-like objects that are both a reference and dictionary: invoke the reference callback before enumerating dictionary children. Repeated references and cycles terminate in both visitors.

- [x] Run red:

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -f net9.0 --filter 'FullyQualifiedName~ReferenceGraphTraversalTests'
```

- [x] Implement the internal dictionary adapter: handle nongeneric `IDictionary` first; otherwise find an implemented `IDictionary<,>` with string key, inspect its interface indexer/`Keys`/`Remove(string)` (not concrete public members, which may be explicitly implemented), and snapshot keys/values. Capture each key in setter/removal delegates using the dictionary's declared value type; avoid the current unconditional `(JsonSchema)o` cast. Dispatch dictionary adapters before generic `IEnumerable` in both visitors, paths, resolver, and postprocessing. Retain value-type leaves; do not solve R1 by reflecting arbitrary boxed structs. Preserve custom dictionary extra-property traversal, with exactly the same property exclusions as before until Task 3.

- [x] Replace the incomplete postprocessing member list with a bounded graph walk: normalize the current schema's Phase 1 data-valued fields; materialize extension values with active options; recursively visit the newly assigned values; recurse through known schema children including `PatternProperties`, `Items`, `DictionaryKey`, and existing children; reflect serializable typed children for non-schema roots and derived schema members, excluding static/indexer/ignored/computed schema properties. Treat literal JSON nodes/elements and primitive values as leaves except when explicitly invoking Phase 1 literal conversion or extension materialization. Preserve the existing schema-detection eligibility rule; this task broadens traversal, not heuristics. Use reference-identity visited sets for traversed objects, including targets with custom equality (a local internal comparer using `ReferenceEquals`/`RuntimeHelpers.GetHashCode` works on netstandard2.0).

- [x] Make resolver generic dictionary lookup consume string keys and materialize dictionary targets with active options, preserving its missing-options exception outside an operation. Ensure collection traversal preserves actual indices when values are null. Do not change existing protected visitor path syntax (`items[0]`) as part of this fix; JSON Pointer resolution/path discovery must continue to use `/items/0`.

- [x] Re-run the focused fixture plus neighboring paths/reference tests and Task 1:

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -f net9.0 --filter 'FullyQualifiedName~ReferenceGraphTraversalTests|FullyQualifiedName~LocalReferencesTests|FullyQualifiedName~JsonPathUtilities|FullyQualifiedName~SerializationContextTests'
```

Record fixes in changelog and commit the task's actual files with `fix: traverse embedded schemas and generic reference dictionaries`.

## Task 3: Match reference collection and paths to converter output (R2, R3)

**Files:** Modify internal graph utility, both visitors, `JsonPathUtilities.cs`, changelog; create `ReferencePathContractTests.cs`. Consult converter writer without absorbing Phase 3 property converter/read-ignore work.

**Interfaces:** Internal `TryGetSerializedPropertyName(Type runtimeType, string originalJsonName, out string serializedName)` returns false for converter-ignored properties, otherwise applies existing `GetMergedRenames` using the active `SchemaSerializationConverter`. Original name comes from `JsonPropertyNameAttribute` or the existing fallback property name. Preserve public APIs and existing configuration precedence.

- [x] Add the write-side rename test using this fixture and assertions:

```csharp
public sealed class RenamedRoot
{
    [JsonPropertyName("defs")]
    public Dictionary<string, JsonSchema> Definitions { get; set; } = new();
    [JsonPropertyName("use")]
    public JsonSchema Use { get; set; }
}
var target = new JsonSchema { Type = JsonObjectType.String };
var root = new RenamedRoot { Use = new JsonSchema { Reference = target } };
root.Definitions["X"] = target;
var converter = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);
converter.RenameProperty(typeof(RenamedRoot), "defs", "definitions");
var json = JsonSchemaSerialization.ToJson(root, SchemaType.JsonSchema, converter, false);
using var parsed = JsonDocument.Parse(json);
Assert.True(parsed.RootElement.TryGetProperty("definitions", out _));
Assert.Equal("#/definitions/X", parsed.RootElement.GetProperty("use").GetProperty("$ref").GetString());
```

Repeat with inherited rename configuration and a derived override, and with an ignored original name that is also renamed; omission wins. Verify the emitted pointer by navigating the emitted JSON object rather than using custom-rename deserialization, whose baseline failure is explicitly outside R2. Keep existing getter-only alias/settable precedence tests passing.

- [x] Add ignored dangling reference fixtures. A root has `[JsonPropertyName("callbacks")] public JsonSchema Callbacks { get; set; }`; initialize it with `new JsonSchema { Reference = new JsonSchema() }`, configure `IgnoreProperty(typeof(rootType), "callbacks")`, and serialize successfully with no callbacks member. Control: without the ignore, serialization still throws because the target is missing from the root graph. Repeat under Swagger2 and OpenApi3 with explicit custom ignore configuration, on a derived schema member, and on an additional property of a converter-decorated dictionary. Test callback-shaped generic dictionary containment from Task 2. An ignored property's getter may throw: reference collection must not evaluate it.

- [x] Run red:

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -f net9.0 --filter 'FullyQualifiedName~ReferencePathContractTests'
```

- [x] Implement the helper using the existing converter configuration:

```csharp
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
```

Use it after static/indexer/attribute filtering and before reading property values in path discovery and visitor reflection branches, including custom dictionary additional properties. Preserve flattening of `[JsonExtensionData]`, which uses dictionary keys rather than the CLR `ExtensionData` property name. Check known-schema fast-path properties against converter ignores too, so an ignored `properties`/`definitions` subtree cannot introduce references collected nowhere in emitted JSON. Keep reference edge traversal and special union keyword behavior intact. Apply renames where constructing serialized paths; do not replace the resolver's read-side name lookup with an untested reverse-mapping redesign. Do not bypass attribute converter precedence or evaluate computed `ActualSchema` properties. Postprocessing should share visibility filtering without interpreting literal objects as properties.

- [x] Re-run focused regressions and all core supported local targets:

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -f net9.0 --filter 'FullyQualifiedName~ReferencePathContractTests|FullyQualifiedName~ReferenceGraphTraversalTests|FullyQualifiedName~SerializationContextTests|FullyQualifiedName~JsonPathUtilities|FullyQualifiedName~SchemaSerializationConverterTests'
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -f net8.0
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -f net9.0
```

Windows CI must additionally run net472. Investigate semantic snapshot differences instead of accepting them automatically. Update Unreleased/Fixes and commit with `fix: align reference paths with serialized property contracts`.

## Phase boundary evidence and deferred downstream gate

- [x] Review the diff for accidental public API changes and unchanged virtual contracts; record actual test counts/targets and resolved IDs. Check Phase 1 literal regressions still pass. Run `git diff --check` before each commit.
- [x] Record S2/S7 as Task 1, S3/R1 as Task 2, R2/R3 as Task 3 only after implementation and regressions pass; writing this plan alone resolves none of them.
- [ ] Keep actual NSwag callback integration pending for the authorized later companion PR. On that STJ branch, use an OpenAPI 3 document whose `paths./subscribe.post.callbacks.onEvent.{$request.body#/url}.post.responses.200.content.application/json.schema.$ref` points to `#/components/schemas/Value`, with Value type string. Assert the callback response `ActualSchema.Type` is String after load and its serialized pointer targets emitted components. Also serialize a Swagger2 operation with an ignored dangling callback reference; no callback output or reference-collection failure is allowed. Verify the parameter `ActualSchema` override and client generation there. Record both exact repo heads and `UseLocalNJsonSchemaProjects=true`; run its full downstream suite only in that later gate.
- [x] Controller updates the `CLAUDE.md` document index and reports any newly discovered limitation. Broader serializer property conversion, read-side ignores (S6), derived member serialization (S9), and custom-rename read behavior stay in their assigned phases/scope; no wholesale resolver or serializer rewrite is required here.

Phase 2 implementation checkpoint: `3f420f22`. All task and integration reviews passed. Full core net8:807 passed/7 skipped; net9:810 passed/7 skipped; core production TFMs build without warnings/errors. PayPal vendor metadata and lazy reference target fixes retain all source references; snapshot changes are ordering only. Windows/Ubuntu CI for the published checkpoint and the later NSwag integration gate remain separate.
