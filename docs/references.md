# References and schema resolution

NJsonSchema models a `JsonSchema` as a graph. A schema can reference another
schema by value (inline) or by reference (`$ref` or `allOf`/`oneOf`/`anyOf`
wrapping a single reference). Several properties on `JsonSchema` exist to help
callers (validator, code generators, NSwag) walk that graph to reach a
"real" schema. Which property to use depends on whether you want to dereference,
dereference and also look through compositions, find inheritance parents, or
treat a schema as a single structural type.

This document describes:

- The access paths: `ActualSchema`, `ActualTypeSchema`, `InheritedSchema`,
  `InheritedTypeSchema`, `HasReference`.
- When a schema counts as a "reference" (`Reference`, `HasAllOfSchemaReference`,
  `HasOneOfSchemaReference`, `HasAnyOfSchemaReference`).
- The resolution algorithm.
- Cross-spec behavior for JSON Schema drafts, Swagger 2.0, OpenAPI 3.0, and
  OpenAPI 3.1.
- NSwag's `OpenApiParameter.ActualSchema` override.
- `JsonSchemaProperty` subclassing and how properties participate in the graph.
- External references and relative-path resolution via `DocumentPath`.
- Known edge cases (sibling keywords of `$ref`, the "Case C" problem) and
  gaps (no `$id`, `$anchor`, `$dynamicRef`, `$recursiveRef` support).

All file/line citations refer to the state on `master` at time of writing;
verify against current code before relying on exact line numbers.

## Access paths at a glance

| Property | Follows `$ref` | Dives into composition | Returns self for non-references |
|----------|:-:|:-:|:-:|
| `Reference` (raw `$ref` target) | resolved during load | no | returns `null` |
| `HasReference` | n/a (predicate) | considers wrapper allOf/oneOf/anyOf | false if self-contained |
| `ActualSchema` | **yes** | no | yes |
| `ActualTypeSchema` | yes | **yes** (multi-allOf, oneOf-with-non-null) | yes |
| `InheritedSchema` | n/a | picks "base class" from allOf | null if not derived |
| `InheritedTypeSchema` | n/a | like `InheritedSchema`, but returns self for array/dict/tuple | null if not derived |

Use the shortest path that gets you what you need:

- Just want the "this schema or its `$ref` target"? → **`ActualSchema`**.
- Want the structural type, diving through allOf/oneOf compositions? →
  **`ActualTypeSchema`**.
- Want the "base class" of an inheritance chain? → **`InheritedSchema`**.
- Want to test whether the schema is a pure reference (so you shouldn't
  regenerate a type for it)? → **`HasReference`**.

## `Reference` and `HasReference`

### `Reference`
`JsonSchema.Reference` is the resolved pointer for a `$ref`. It is set by the
schema loader (`JsonSchemaReferenceUtilities` and friends) during deserialization.
A schema with a `$ref` keyword has its other keywords parsed into the schema
object, but after resolution:

```csharp
// From JsonSchema.Reference.cs, the setter:
public override JsonSchema? Reference
{
    get => base.Reference;
    set
    {
        base.Reference = value;
        if (value != null)
        {
            Type = JsonObjectType.None;
        }
    }
}
```

Setting `Reference` clears `Type`. This matches JSON Schema drafts ≤ 2019-09
where `$ref` siblings are *undefined behavior* and most implementations (including
NJsonSchema) ignore them. Note: `Type` is cleared, but other sibling keywords
such as `Default`, `Enumeration`, `Format`, `Description`, `Title` remain on the
schema — they just are **not reachable via `ActualSchema`** because `ActualSchema`
walks to the reference target.

### `HasReference`
A schema is considered a reference if any of the following hold:

```csharp
public bool HasReference =>
    Reference != null ||
    HasAllOfSchemaReference ||
    HasOneOfSchemaReference ||
    HasAnyOfSchemaReference;
```

In other words, four shapes are treated as references:

1. **Direct `$ref`**: `{ "$ref": "#/definitions/X" }`
2. **Single-item `allOf` wrapping a `$ref`**: `{ "allOf": [{ "$ref": "..." }] }`
3. **Single-item `oneOf` wrapping a `$ref`**
4. **Single-item `anyOf` wrapping a `$ref`**

The wrapper-as-reference predicates are deliberately strict. Each of them
requires that the schema carry **no other structural keywords**. The `allOf`
form, for example:

```csharp
public bool HasAllOfSchemaReference =>
    Type == JsonObjectType.None &&
    _anyOf.Count == 0 &&
    _oneOf.Count == 0 &&
    _properties.Count == 0 &&
    _patternProperties.Count == 0 &&
    AdditionalPropertiesSchema == null &&
    MultipleOf == null &&
    !IsEnumeration &&
    _allOf.Count == 1 &&
    _allOf.Any(s => s.HasReference);
```

Adding e.g. an `enum` at the same level disqualifies the schema from being
a "wrapper reference" — `HasAllOfSchemaReference` returns false, `HasReference`
returns false, and `ActualSchema` returns the schema itself (so the enum is
preserved). This mitigation is specific to `enum`; other sibling keywords
(`description`, `default`, `readOnly`, `title`, etc.) have no such guard and
are silently lost when the schema is treated as a reference — see
[Sibling keywords](#sibling-keywords-of-ref) below.

## `ActualSchema`

```csharp
public virtual JsonSchema ActualSchema { get; }
```

Follows `$ref` and wrapper-references to reach a non-reference schema. Does
**not** dive into multi-item `allOf`/`oneOf` compositions.

Algorithm (from `GetActualSchema`):

1. If the schema is already in the cycle-detection set → throw.
2. If the schema has an unresolved reference path → throw.
3. If `HasReference` is true, resolve one step:
   - `HasAllOfSchemaReference` → recurse on `_allOf[0]`.
   - `HasOneOfSchemaReference` → recurse on `_oneOf[0]`.
   - `HasAnyOfSchemaReference` → recurse on `_anyOf[0]`.
   - Otherwise → recurse on `Reference`.
4. Otherwise, return `this`.

Result: the first schema reached that is not a reference.

## `ActualTypeSchema`

```csharp
public virtual JsonSchema ActualTypeSchema { get; }
```

Like `ActualSchema` but additionally drills into compositions to find the
schema that carries the *structural type*. Algorithm:

```csharp
var schema = Reference ?? this;

if (schema._allOf.Count > 1 && schema._allOf.Count(s => !s.HasReference && !s.IsDictionary) == 1)
    return schema._allOf.First(s => !s.HasReference && !s.IsDictionary).ActualSchema;

return schema._oneOf.FirstOrDefault(o => !o.IsNullable(SchemaType.JsonSchema))?.ActualSchema
       ?? ActualSchema;
```

Equivalent to `ActualSchema` in the common case (no multi-item `allOf`, no
non-null `oneOf`). Differs in two situations:

- **Multi-item `allOf`**: if exactly one `allOf` item is neither a `$ref` nor a
  dictionary, return that item's `ActualSchema`. This is the "pick the non-base
  subtype" rule — e.g. `{"allOf": [{"$ref":"Base"}, {"type":"string"}]}` yields
  the `string` schema.
- **`oneOf` with a non-nullable alternative**: return the first non-null
  alternative's `ActualSchema`. This is how OpenAPI 3.0 nullability (a `oneOf`
  with a `null` schema alongside the real schema) is peeled back to the real
  schema.

In all other cases it falls through to `ActualSchema`.

**Use `ActualTypeSchema` when you want a type-like answer** (what is this
property's effective type, does it have an enum, is it an array/dict?). **Use
`ActualSchema` when you want reference resolution only** and want to respect
whatever the surrounding schema said (e.g. walking properties in the validator).

## `InheritedSchema` and `InheritedTypeSchema`

These are code-generation helpers; they identify the "base class" in an
inheritance chain expressed via `allOf`. Not used by the validator.

```csharp
public JsonSchema? InheritedSchema
{
    get
    {
        if (_allOf == null || _allOf.Count == 0 || HasReference) return null;
        if (_allOf.Count == 1) return _allOf[0].ActualSchema;

        var hasReference = _allOf.FirstOrDefault(s => s.HasReference);
        if (hasReference != null) return hasReference.ActualSchema;

        var objectTyped = _allOf.FirstOrDefault(s => s.Type.IsObject());
        if (objectTyped != null) return objectTyped.ActualSchema;

        return _allOf.FirstOrDefault()?.ActualSchema;
    }
}
```

Priority order for "what is my base":
1. Single-item `allOf` → that item.
2. Otherwise, the first `allOf` item that is itself a reference.
3. Otherwise, the first `allOf` item typed as `object`.
4. Otherwise, the first `allOf` item.

`InheritedTypeSchema` falls back to `ActualTypeSchema` when there is no
inheritance **and** the schema is a dictionary/array/tuple — so code generators
see the schema itself as "the type to inline" for those cases.

## How `$ref` resolution happens at load time

The static methods on `JsonSchema` (e.g. `FromJsonAsync`) parse the JSON and
run reference resolution via
`NJsonSchema.References.JsonSchemaReferenceUtilities`. During load:

1. `$ref` values are read into `IJsonReferenceBase.ReferencePath` (a string).
2. Resolver walks paths (JSON Pointer for in-document, URI for external) and
   sets `Reference` to the resolved `JsonSchema` instance.
3. External refs can be loaded via `JsonReferenceResolver` / URI resolvers;
   YAML references are handled by `NJsonSchema.Yaml`.

If `Reference` is null but `ReferencePath` is set when `ActualSchema` is called,
`GetActualSchema` throws — accessing a graph before resolution completes is a
programmer error. This is distinct from a cyclic reference, which also throws.

### `DocumentPath` and external references

The document-level base URI / file path for relative `$ref` values is held on
`IDocumentPathProvider.DocumentPath`. It is set by the loader when you call
`JsonSchema.FromFileAsync(path)` or `FromUrlAsync(url)`; for `FromJsonAsync`
you can pass it explicitly so relative external refs can be located.

Resolution order for an external `$ref`:

1. If the ref is absolute (`http://…`, `file:///…`) it is loaded verbatim.
2. Otherwise it is resolved against the owning document's `DocumentPath`.
3. The loaded document is parsed, its own refs resolved recursively, and the
   result is cached so shared refs produce the same instance.

Two concrete limitations to be aware of:

- NJsonSchema does **not** implement JSON Schema's `$id`-as-base-URI rule.
  Only the document-level `DocumentPath` participates in relative resolution;
  a nested schema with `$id: "http://other.example/"` will not re-root the
  refs inside it.
- NJsonSchema does **not** resolve `$anchor` / `#anchor` targets. Only
  JSON-Pointer-style refs (`#/definitions/X`, `#/components/schemas/X`) and
  external document URIs resolve. Anchor-style refs will produce an
  unresolved `ReferencePath` and trip the "not resolved" exception in
  `GetActualSchema`.

## `JsonSchemaProperty` and the reference graph

`JsonSchemaProperty` extends `JsonSchema`, so every property value in a
schema's `Properties` dictionary is itself a full `JsonSchema` node and
participates in reference resolution identically:

- `property.Reference` can be set (when the property is written as
  `"p": {"$ref":"#/X"}`), and `property.ActualSchema` peels off to the
  referenced schema.
- Wrapper-reference shapes apply unchanged: `"p": {"allOf":[{"$ref":"..."}]}`
  makes the property itself a wrapper reference, and `property.HasReference`
  is `true`.
- `JsonSchemaProperty` overrides `IsNullable` (spec-dependent
  nullability for Swagger 2.0 / OpenAPI 3). It does **not** override
  `ActualSchema` or `ActualTypeSchema`, so the base graph-walking logic
  applies.

Practical consequence: every gotcha discussed elsewhere in this document
(case C, sibling drops, wrapper-reference classification) applies equally
when the "schema" in question is actually a property. Codegen traversals
that walk properties should read `property.ActualTypeSchema` for type
decisions and `property` itself (or `property.ActualSchema`) for
property-level metadata like `Description`, `IsRequired`, `IsReadOnly` —
these live on the property node, not the target.

## Cross-spec behavior

NJsonSchema models three schema dialects via `NJsonSchema.SchemaType`:

```csharp
public enum SchemaType
{
    JsonSchema,  // standalone JSON Schema
    Swagger2,    // Swagger / OpenAPI 2.0
    OpenApi3     // OpenAPI 3.0 and 3.1
}
```

Note that **3.0 and 3.1 share a single enum value**; differences between them
are currently handled by extension properties (`x-nullable`, the `nullable`
keyword for 3.0, arbitrary `$ref` siblings for 3.1) rather than by a dedicated
schema-type value.

### Keyword availability by spec

| Spec | `$ref` | `allOf` wrapping `$ref` | `$ref` siblings allowed | `const` |
|------|:-:|:-:|:-:|:-:|
| JSON Schema draft 4 | ✓ | ✓ | no (ignored) | ❌ |
| JSON Schema draft 6 | ✓ | ✓ | no (ignored) | ✓ |
| JSON Schema draft 7 | ✓ | ✓ | no (ignored) | ✓ |
| JSON Schema 2019-09 | ✓ (`$ref` + `$recursiveRef`) | ✓ | **yes** | ✓ |
| JSON Schema 2020-12 | ✓ (`$ref` + `$dynamicRef`) | ✓ | **yes** | ✓ |
| Swagger 2.0 | ✓ (JSON Pointer to `definitions`) | ✓ | no | ❌ (use `enum: [x]`) |
| OpenAPI 3.0 | ✓ (JSON Pointer to `components/schemas`) | ✓ | no | ❌ (use `enum: [x]`) |
| OpenAPI 3.1 | ✓ (aligned with JSON Schema 2020-12) | ✓ | **yes** | ✓ |

NJsonSchema does **not** model the 2019-09 `$recursiveRef` / `$recursiveAnchor` pair,
the 2020-12 `$dynamicRef` / `$dynamicAnchor` pair, or `$anchor` (draft-6+). These
keywords are parsed generically as unknown keywords and preserved in extension data,
but reference resolution only walks `$ref`. If a schema relies on dynamic/recursive
references for polymorphism, NJsonSchema will not resolve them.

The "`$ref` siblings allowed" column is what introduces the edge cases
described below. Older drafts treat any sibling of `$ref` as undefined behavior;
implementations typically drop them.

### What NJsonSchema actually does with siblings

NJsonSchema's behavior matches the older drafts: setting `Reference` clears
`Type`, and `ActualSchema`/`ActualTypeSchema` unconditionally resolve through
references, bypassing siblings. This is correct for drafts ≤ 2019-09 and for
Swagger 2.0 / OpenAPI 3.0. For **OpenAPI 3.1 and JSON Schema 2020-12**, which
allow sibling keywords to apply alongside `$ref`, NJsonSchema currently drops
some siblings on resolution — see below.

## NSwag's `OpenApiParameter` override

`NSwag.Core.OpenApiParameter` extends `JsonSchema` and overrides `ActualSchema`:

```csharp
public override JsonSchema ActualSchema
{
    get
    {
        if (Reference is OpenApiParameter parameter)
            return parameter.ActualSchema;
        return Schema?.ActualSchema
            ?? CustomSchema?.ActualSchema
            ?? base.ActualSchema;
    }
}
```

The override models OpenAPI 3.x's parameter wrapping: a parameter is *not* the
schema — it has a nested `schema` property. So `parameter.ActualSchema` unwraps
to `parameter.Schema.ActualSchema` for 3.x-style parameters, and falls back to
the parameter-itself-as-schema for Swagger 2.0 inline parameters (path/query/
header, where `Schema` is null).

`ActualTypeSchema` is **not** overridden. Its base implementation ends with
`?? ActualSchema`, so in the common case (no multi-item `allOf`/qualifying
`oneOf` on the parameter wrapper) it dispatches virtually to the override —
same result as `ActualSchema`. If a parameter wrapper itself had multi-item
`allOf` (unusual in practice), `ActualTypeSchema` would dive into those items
before reaching the override, yielding a different result.

## Sibling keywords of `$ref`

The most common surprise in NJsonSchema's reference handling: **a schema with
`$ref` plus non-type sibling keywords loses the siblings when resolved.** This
matters for `enum`, `description`, `default`, `readOnly`, `title`, etc.

Consider the four shapes of "property carries both a reference and an extra
constraint" (using `description` as a stand-in — the same shapes apply to any
sibling keyword):

| # | Shape | Example | Where is the sibling reachable? |
|---|-------|---------|---|
| A | Direct keyword | `"p": {"description":"x"}` | self, `ActualSchema`, `ActualTypeSchema` |
| B | `$ref` to a schema that carries the keyword | `"p": {"$ref":"#/C"}`, `"C": {"description":"x"}` | `ActualSchema`, `ActualTypeSchema` (self carries nothing) |
| C | Direct `$ref` + sibling keyword | `"p": {"$ref":"#/C", "description":"x"}` | **self only** — both `ActualSchema` and `ActualTypeSchema` resolve away |
| D | Multi-item `allOf` with a sibling-carrying item | `"p": {"allOf": [{"$ref":"B"}, {"description":"x"}]}` | `ActualTypeSchema` only (via the multi-`allOf` rule) |

Case C is valid in JSON Schema 2019-09+ and OpenAPI 3.1, but NJsonSchema's
`HasReference` logic drops the sibling — the `Reference` setter clears `Type`,
and `ActualSchema` follows the reference. Pre-2020-12 drafts and Swagger 2.0 /
OpenAPI 3.0 officially treat these siblings as undefined behavior, so the loss
is conformant there — it only bites under 2019-09+ / 3.1.

### The `!IsEnumeration` partial mitigation

`enum` is the one sibling keyword that gets special handling in the
wrapper-reference predicates. `HasAllOfSchemaReference`, `HasOneOfSchemaReference`,
and `HasAnyOfSchemaReference` each include a `!IsEnumeration` guard, so the
wrapper form `{"allOf":[{"$ref":"..."}], "enum":[...]}` correctly falls out of
the reference classification and keeps the enum reachable via self /
`ActualSchema`. No analogous guard exists for any other sibling keyword, so the
wrapper-with-description / wrapper-with-default forms lose their siblings.

### Summary of current coverage (using `enum` vs. other siblings)

| Case | `enum` handled | Other siblings (`description`, `default`, …) |
|------|:-:|:-:|
| A Direct | ✓ | ✓ |
| B `$ref` to sibling-carrying schema | ✓ | ✓ |
| C Direct `$ref` with sibling | ✗ | ✗ |
| D `allOf` wrapper with sibling item (size 1) | ✓ (via `!IsEnumeration` guard) | ✗ |
| D' Multi-item `allOf` with sibling-only item | ✓ | ✓ |

Case C is a cross-keyword platform limitation — the fix is in `HasReference`
behavior or the `Reference` setter, not keyword-specific code. Expanding case D
coverage to other siblings would require mirroring the `!IsEnumeration` guard
for each of them (or generalising it), but no downstream codegen currently
exercises those shapes.

## Decision matrix for consumers

Quick heuristic for picking the right access path in new code:

| Intent | Use |
|--------|-----|
| Validate a value against a schema | `schema.ActualSchema` (validator recurses on it) |
| "Does this property carry an `enum`/structural constraint?" | `schema.ActualTypeSchema.IsEnumeration` (symmetric with `Type`, `IsArray`, `IsDictionary`) |
| Generate a C# / TS type for a property | `schema.ActualTypeSchema` |
| Find the base class in inheritance | `schema.InheritedSchema` |
| Generate an inline type for arrays/dicts/tuples | `schema.InheritedTypeSchema` |
| Test whether a schema is a pure reference | `schema.HasReference` |
| Get the raw `$ref` target | `schema.Reference` |

Keep access paths consistent within a single feature. For example, the
codegen side reads `IsEnumeration`, `IsArray`, `IsDictionary`, and `Type` off
`ActualTypeSchema` in several files (`CSharpTypeResolver`,
`TypeScriptTypeResolver`, `PropertyModel`, `ValueGeneratorBase` for
`JsonSchemaProperty`). Mixing `ActualSchema` for one check and `ActualTypeSchema`
for another on the same property leads to incoherent output in Case D
(property gets a setter despite having an initializer, or vice versa).

## Gotchas

- **`ActualSchema` throws** on cyclic references or when a reference path was
  never resolved. If you're traversing untrusted schemas, guard with try/catch.
- **`Reference` setter clears `Type`.** If you programmatically assign
  `schema.Reference = other`, the schema's `Type` is wiped. This is by design
  (pre-2020-12 semantics) but can surprise code that sets both.
- **`ActualSchema` is virtual.** Subclasses (e.g. `OpenApiParameter`) can
  override. `ActualTypeSchema` is also virtual but less commonly overridden;
  its base implementation ends with a virtual call to `ActualSchema`, so
  overrides of `ActualSchema` still apply for callers that go through
  `ActualTypeSchema` in the common case.
- **`IsNullable` on `oneOf` peek-through.** `ActualTypeSchema` uses
  `IsNullable(SchemaType.JsonSchema)` to filter out the null alternative in
  `oneOf`. That's hardcoded to `JsonSchema` type, but in practice it is fine
  for two reasons: (a) the base `JsonSchema.IsNullable` implementation does
  not actually branch on `schemaType` — it checks `IsNullableRaw`, enum-with-null,
  `Type.IsNull()`, and `ExtensionData["nullable"]`; and (b) the call is virtual,
  so overriding subclasses (e.g. `OpenApiParameter`, `JsonSchemaProperty`) can
  apply their own spec-specific rules even when called with `SchemaType.JsonSchema`.
  The hardcoding would only bite if a subclass expected the argument value to
  drive its behavior, and no such subclass exists today.

## Known limitations and problems

These are not blockers but should be addressed when someone works on the
relevant area. All are cross-cutting platform issues unless otherwise noted.

### Reference and sibling-keyword semantics

1. **Siblings of `$ref` are silently dropped.** Any non-type keyword placed
   alongside a `$ref` (`enum`, `description`, `default`, `readOnly`, `title`,
   etc.) is unreachable via `ActualSchema`/`ActualTypeSchema` because the
   `Reference` setter clears `Type` and resolution walks straight to the
   target. Valid in JSON Schema 2019-09+ and OpenAPI 3.1; undefined behaviour
   in earlier drafts (so the loss is conformant below 2019-09). The fix is
   either (a) teach `HasReference` / the resolver to preserve sibling
   keywords, or (b) model `$ref` as a true composition at load time. Either
   is a cross-cutting change touching validation and codegen.

2. **`Reference` setter clears `Type` but not other siblings.** Setting
   `schema.Reference = other` wipes `Type`. Other sibling keywords
   (`Default`, `Enumeration`, `Format`, `Description`, `Title`) remain on the
   schema but are not observable through the standard access paths.
   Intentional for pre-2020-12 drafts; surprising for programmatic API
   consumers who expect setting `Reference` to be additive.

3. **`SchemaType.OpenApi3` conflates 3.0 and 3.1.** OpenAPI 3.0 follows a
   JSON Schema draft 4/5 subset (no `$ref` siblings). 3.1 follows 2020-12
   (siblings allowed). NJsonSchema uses a single enum value and therefore
   cannot make draft-specific decisions without a separate flag. Splitting
   this enum is technically breaking for consumers (incl. NSwag).

### Unsupported reference keywords

4. **No `$id` support.** `JsonSchema.Id` is mapped to the draft-4 `id`
   keyword. The draft-6+ `$id` keyword (which can also set a base URI for
   child references) is not parsed as an identifier, and there is no
   per-schema base URI tracking — only a document-level `DocumentPath`.
   Schemas that use nested `$id` to re-root relative references will not
   resolve correctly.

5. **No `$anchor` support.** Draft-6's `$anchor` (and the deprecated
   `#anchor` form in draft-4 `id`) is not indexed by the resolver. Only
   JSON-Pointer-style `$ref` values and explicit `definitions` /
   `components/schemas` targets resolve.

6. **No `$dynamicRef` / `$recursiveRef` support.** The 2020-12 and 2019-09
   dynamic/recursive reference mechanisms are not modelled. Dynamic-scope
   polymorphism (commonly used for recursive schema extensions) falls back
   to ordinary `$ref` semantics or is silently ignored.

### Minor

7. **`ActualTypeSchema` hardcodes `SchemaType.JsonSchema` in its `oneOf`
   peek-through.** `schema._oneOf.FirstOrDefault(o => !o.IsNullable(SchemaType.JsonSchema))`.
   Works in practice because the base `IsNullable` ignores the `schemaType`
   argument (it checks `IsNullableRaw`, enum-with-null, `Type.IsNull()`, and
   `ExtensionData["nullable"]`), and because any override in a subclass can
   still decide based on its own state. The hardcoding would only bite a
   future subclass that expects the argument value to drive behaviour.

8. **`ActualSchema` throws on unresolved references.** If a `$ref` was parsed
   but resolution hasn't run (programmatic construction without a resolver),
   any access throws. Documented, but easy to trip over.
