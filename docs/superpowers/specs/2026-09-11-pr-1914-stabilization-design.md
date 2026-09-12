# PR #1914 stabilization design and roadmap

Status: approved for implementation on 2026-09-11. Repairs land directly in PR #1914; NSwag companion fixes and its full downstream gate follow later in the existing NSwag STJ migration PR.

## Objective and baseline

Finish the System.Text.Json core migration in [PR #1914](https://github.com/RicoSuter/NJsonSchema/pull/1914), targeting `v12`, without introducing accidental changes to schema values, validation results, reference resolution, or generated contracts.

At the planning checkpoint, the PR head is `d0ae5c5b774a436eef9a96238b4b9a3e0dadb3cd`, and `v12` is `2a6c48b82108f78af37cf60493306c304de10c58`. Master was merged into both histories. The last base synchronization did not change the migration tree. Windows and Ubuntu CI passed at that head, but the review found regressions outside existing test coverage. Green CI alone is therefore insufficient for merge.

The existing March migration plans describe the original implementation. This document and its linked phase plans supersede those plans for remaining stabilization work. They do not supersede the broader [v12 roadmap](../../plan_v12.md).

## Compatibility policy

- Specification behavior wins over master behavior (user clarification, 2026-09-12). Apply the relevant JSON Schema dialect or Swagger/OpenAPI version, rather than assuming modern draft semantics apply everywhere. Preserve correct baseline contracts; document and test corrections to baseline bugs.
- Record every observable difference from master in the [v12 migration guide](../../changelog_v12.md), with before/after behavior, baseline revision, affected targets, rationale/specification source, consumer action, and evidence. Separate specification corrections, intentional API/runtime changes, restored behavior, accidental regressions, and unverified comparisons. Green CI is not an exhaustive compatibility claim.
- Accept the major-version API and binary breaks required to replace Newtonsoft types and customization APIs with STJ equivalents. Inventory each actual signature change and provide a working migration example; this is not blanket approval for additional public API redesign.
- Preserve literal JSON values in defaults, examples, enumeration values, and extension payloads. An object resembling a schema is still data when it occurs in a data-valued keyword.
- Preserve supported schema and OpenAPI wire contracts, including XML metadata, property converters, ignored fields, derived schema members, keyword names, and references. Ordering and insignificant number spelling may change; semantic changes require a documented specification correction or explicit migration contract.
- Preserve correct validation and generated C#/TypeScript contracts. Existing snapshots are evidence to investigate, not authority to accept lost behavior.
- Exercise all three current `SchemaType` values with appropriate inputs. Do not expand `SchemaType`, implement new JSON Schema drafts, or absorb #1917 property discovery into this repair.
- Preserve existing Newtonsoft adapter support. Direct Newtonsoft serialization of STJ-annotated core objects is a separate compatibility limitation that must be documented accurately.

Proposed decisions to review before the affected later phase: preserve supported lenient schema inputs through targeted parsing, including quoted exclusive bounds; preserve stream/string sample-input parity and the previous stream ownership behavior; infer dates only for supported ISO forms. If a stricter policy is preferred, replace that specific compatibility repair with explicit rejection tests and a documented breaking change. Do not silently drop values or change unrelated strings.

Whole-valued JSON numbers such as `1.0` and `1e0` should validate as integers. Nonintegral values must remain nonintegral even when a double would round or underflow. This implemented numeric policy matches modern JSON Schema. Draft 4 defines integers lexically (without a fraction or exponent part); the shared implementation does not currently distinguish that rule. Record this as an open dialect compatibility issue, not universal specification conformance, alongside the observed change from master.

## Implementation approach

Use focused commits on the existing migration branch. Each commit adds a failing public-behavior regression test, implements the smallest coherent fix, and records any user-visible consequence in the v12 changelog. Keep the NSwag companion change in its own repository PR. Do not merge #1914 into `v12` until the final integration gate.

Three approaches were considered:

| Approach | Tradeoff |
| --- | --- |
| Patch each finding independently | Small individual diffs, but duplicates JSON-number logic and leaves serialization/traversal inconsistencies. |
| Replace the serialization pipeline wholesale | Could simplify internals, but combines a second architecture migration with regression repair. |
| Repair shared causes in bounded batches (recommended) | Shares value semantics and traversal contracts while retaining the existing architecture where it works. |

Use separate paths for literal JSON conversion and schema-aware extension materialization. Share exact JSON-value equality between enumeration and uniqueness validation. Keep numeric recognition consistent for parsed and programmatically constructed nodes. Carry serializer context across asynchronous resolution, retaining the active dialect and converters. Align schema traversal and serialized property names/visibility so emitted references point to emitted data.

A resolver-modifier rewrite is not a prerequisite. If a repair cannot be made without a broader change, document the concrete limitation and revise that phase before expanding it.

## Findings and acceptance criteria

The IDs below preserve the review's identifiers. The original review has 26 findings; implementation added V11, G3, A1, and the final whole-PR findings F1–F6 below. The original six P1 findings are: S1, S2, S3, V1, V2, R1. The reproductions are summarized here so execution does not depend on temporary probe directories or a local review artifact.

### Phase 1: literal values and validation semantics

Detailed execution plan: [Phase 1](../plans/2026-09-11-pr-1914-values-and-validation.md).

| ID | Required behavior / regression test |
| --- | --- |
| S1 P1 | `default`, OpenAPI `example`, and object enum values containing `type` or `properties` survive load/save unchanged. `{"default":{"type":"foo","name":"test"}}` must retain `type: "foo"`. Nested literal objects must never become schemas by heuristic. |
| V1 P1 | Matching boolean, number, object, array, and null enum values validate. `1`, `1.0`, and `1e0` compare numerically; object property order is irrelevant; array order matters; strings and numbers stay distinct. |
| V2 P1 | Direct `JsonSchemaValidator.Validate` supports CLR-backed numeric `JsonValue` instances without throwing or bypassing bounds. Include all integral CLR types, float, double, decimal, and generated sample nodes. |
| V3 P2 | `uniqueItems` accepts `[9007199254740992,9007199254740993]`, rejects numerically equal alternate spellings, and applies structural equality to objects and arrays. |
| V4 P2 | Integer validation rejects `1.00000000000000001` and `1e-1000`; accepts mathematically whole `1.0` and `1e0`. Recognition does not use lossy double truncation. |

Additional regression discovered and repaired during Phase 1:

| ID | Required behavior / regression test |
| --- | --- |
| V11 | Direct CLR-backed and customized `JsonValue` instances validate and compare according to their serialized JSON value, including non-string CLR values serialized as strings, custom objects/arrays/nulls, booleans, numbers, enums, and primitive type-info converters. Preserve caller-owned nodes. |

### Final whole-PR review: F1–F6

The final review at `13dcbd0a` identified six additional P2 compatibility defects. These are repaired and verified by the public `FinalMigrationRegressionTests` fixture (34 cases) and the full core net8/net9 suites. The scoped re-review is complete, including the subsequent N1 struct-default correction. Full Release build/tests/packages and the actual packed-assembly API audit pass at `74e71db7`; exact-head CI publication status is recorded in the Phase 5 plan and PR verification section. NSwag integration remains deferred to NSwag #5355.

| ID | Completed behavior / regression coverage |
| --- | --- |
| F1 | Direct and factory property converters delegate with the original options and property scope; bounded recursion guard, nested same-type values, sibling isolation, and nullable factory controls pass. |
| F2 | Explicit `JsonIgnore(Never)` and resolver `ShouldSerialize` contracts supersede operation null/default omission. Plain nulls, `HandleNull` true/false, factories, and all three default-ignore policies match STJ controls. Explicit parameterless struct constructors are not invoked to compute default values. |
| F3 | Ignored core metadata names survive as root/nested extension data, including inherited generic reference metadata. References into these entries resolve and retain paths; explicit user/derived ignores (including differently typed hidden members) and registered ignores remain authoritative. |
| F4 | Both public node-validation overloads forward directly with settings/dialect. Primitive/object/array/null and nested error tokens retain caller identity where adaptation is unnecessary and never acquire synthetic source coordinates. |
| F5 | Quoted boolean unions control additional-properties/items validation in all three dialects; literal strings survive. Invalid non-null recognized scalars throw instead of silently disappearing. |
| F6 | Customized string-backed nodes use serialized text for length/pattern/format, enum, and uniqueness semantics. Parsed-value fast path and unchanged ordinary-string identity remain; only individual CLR-backed values are serialized, without whole-document reparsing or mutation. |

### Phase 2: serializer context and reference graph

Detailed execution plan: [Phase 2](../plans/2026-09-11-pr-1914-references-and-context.md).

Primary files: `Infrastructure/JsonSchemaSerialization.cs`, `JsonReferenceResolver.cs`, `Visitors/JsonReferenceVisitorBase.cs`, `Visitors/AsyncJsonReferenceVisitorBase.cs`, and `JsonPathUtilities.cs` under `src/NJsonSchema/`. Add regression fixtures in core reference/serialization tests; exercise actual NSwag callback types in the deferred NSwag #5355 downstream gate.

| ID | Required behavior / regression test |
| --- | --- |
| S2 P1 | A resolver that genuinely suspends asynchronously can resolve an external reference followed by local `#/x-schema`. No thread-local options failure; concurrent calls with different dialects do not leak state. Test cancellation/exception cleanup and nested loads. |
| S3 P1 | Postprocessing visits pattern properties, tuple items, dictionary-key schemas, typed document children, and nested materialized extensions. References such as `#/patternProperties/x/x-model`, `#/items/0/x-model`, and `#/x-model/x-inner` resolve. Literal values normalize consistently at every location. |
| S7 P2 | Materialized extension schemas retain active dialect/custom converters. OpenAPI `x-model: {type: string, nullable: true}` preserves nullability. |
| R1 P1 | Both visitors traverse generic-only dictionaries before treating boxed value types as leaves. Actual `OpenApiCallback` paths resolve `#/components/schemas/Value`; cycles and repeated references terminate correctly. |
| R2 P2 | Write-side reference paths use converter-renamed serialized property names: a CLR `defs` property emitted as `definitions` yields `#/definitions/X`. The previously failing baseline read scenario is not a newly introduced regression. |
| R3 P2 | Converter-ignored properties are excluded from reference collection. An ignored dangling reference cannot make serialization fail; verify the Swagger 2 ignored-callback case. |

Gate: all six cases covered; review virtual members and NSwag overrides before accepting a traversal/context API change. Prefer an explicit per-operation context carried through awaits. If compatibility requires another mechanism, prove async isolation, nested scope restoration, and cleanup with tests.

### Phase 3: serialization and generated contracts

Detailed execution plan: [Phase 3](../plans/2026-09-11-pr-1914-serialization-contracts.md).

Primary files: `JsonXmlObject.cs`, `Infrastructure/SchemaSerializationConverter.cs`, `Infrastructure/JsonSchemaSerialization.cs`, `JsonSchema.Serialization.cs`, and `Generation/JsonSchemaGenerator.cs`; corresponding serialization and code-generation tests.

| ID | Required behavior / regression test |
| --- | --- |
| S4 P2 | XML name, namespace, prefix, wrapped, and attribute metadata survive deserialization and roundtrip despite internal setters. |
| S5 P2 | Filtered serialization honors property-level converters, including string enums and the NSwag discriminator mapping converter. Test property and type converter precedence. |
| S6 P2 | Read-side `IgnoreProperty` matches its contract: ignored input neither populates the property nor throws because its value is incompatible. |
| S8 P2 | Lenient coercion targets schema keywords only. Parsing quoted `x-nullable` must not turn a string default `"true"` into a boolean or replace nonbreaking spaces inside strings. |
| S9 P2 | Derived schema members serialize in nested definitions and other declared-base-type containers, with supported STJ attributes/customization. Test root and nested instances. |
| S10 P2 | Quoted exclusive bounds retain the chosen compatibility behavior. No silently discarded `"1.5"` or `"true"`; cover numeric and draft-4 boolean forms. |
| G1 P2 | `readOnly` is recognized under the existing case-insensitive input contract, and TypeScript dictionary properties retain `readonly`. Audit semantic snapshot changes separately from ordering. |
| G3 | Retained exact numeric values preserve generated integer enum member names, integer literals, flags, and named defaults. Cover `1e0`, `1.0`, and representable long values in compiled C# and TypeScript output; compare baseline behavior without `x-enumNames` as well as explicit names. |
| G2 P2 | Newtonsoft adapter generation for `JObject`/`JToken` retains the intended free-form object schema under Swagger 2 and other supported modes. Cover both root and property cases. |

Gate: compare parsed schema outputs and generated declarations with the pre-migration baseline. Explain every remaining semantic snapshot change. Compile representative generated C# and TypeScript using the existing test infrastructure.

### Phase 4: diagnostics and sample generation

Detailed execution plan: [Phase 4](../plans/2026-09-11-pr-1914-diagnostics-and-samples.md).

Primary files: `Validation/JsonSchemaValidator.cs`, `SampleJsonSchemaGenerator.cs`, `Generation/SampleJsonDataGenerator.cs`; corresponding validation/generation tests.

| ID | Required behavior / regression test |
| --- | --- |
| V5 P2 | Replace repeated whole-document line searches with a single scan/index or equivalent bounded lookup. Measure 20k/40k/80k-element pretty arrays on the same runtime; demonstrate near-linear scaling. Do not add flaky wall-clock unit assertions. |
| V6 P2 | Stream and string overloads agree on supported comments, trailing commas, single quotes, and unquoted names; preserve supported BOM encodings and the chosen ownership contract. |
| V7 P2 | Non-ISO strings such as `10/12/2024` do not infer an incompatible date schema depending on current culture. Validate samples against their generated schemas under multiple cultures. |
| V8 P2 | Generating an array with `minItems: 1` and null items produces `[null]`, not `[]`. Distinguish a JSON null value from absence of a generated item. |
| V9 P2 | Internal diagnostic keys distinguish literal property `a.b` from nested `a`/`b`; errors point to the correct source lines. Preserve the public `ValidationError.Path` format for this repair. |
| V10 P3 | Root null has source coordinates, and CR-only input has correct line numbers. Cover LF, CRLF, CR, and escaped property names. |

### Phase 5: migration documentation and downstream release gate

Detailed execution plan: [Phase 5](../plans/2026-09-11-pr-1914-documentation-and-final-gate.md).

| ID | Required behavior / regression test |
| --- | --- |
| A1 | Preserve the public static `JsonSchema.ToolchainVersion` getter; STJ migration does not require changing a property to a field. Verify the member contract before documenting final API differences. |
| D1 P2 | Correct `docs/changelog_v12.md`: construct the actual settings type, remove nonexistent `SerializerSettings` usage, describe discriminator mappings with their actual types, and correct date/numeric/runtime-value claims. Compile API migration examples against the intended packages. |

A1 and D1 implementation/documentation are complete at Phase 5 Task 1: getter RED/green and baseline binary binding, repaired compiled migration examples, and focused generated consumers passed. See the Phase 5 plan for the final controller evidence and the deliberately deferred NSwag gate.

Refresh the PR description using `.github/pull_request_template.md`, with explicit sections for source/binary API breaks, serialized JSON/schema contracts, validation/runtime behavior, generated client contracts, and NSwag actions. Distinguish intentional breaks from fixed regressions and unresolved decisions. Do not mark a finding fixed until its test and implementation land.

As the user-sequenced follow-up in NSwag #5355, reproduce NSwag integration in an isolated checkout using its STJ companion branch and this NJsonSchema build. Verify branch heads and project-reference wiring before running its full build/tests. A clean build of NSwag's older Newtonsoft-based branch is not the migration compatibility gate. Exercise callback references, discriminator mapping, parameter schema overrides, and client generation. Record both exact commit hashes and test results; maintain the companion PR separately.

## Checkpoints and completion

1. Review this compatibility policy and the Phase 1 plan, then implement Phase 1 as focused commits.
2. At each phase boundary, report resolved IDs, test results, remaining compatibility decisions, and any new public API changes. Write the next executable phase plan using the repaired code as its baseline.
3. Run focused regressions during each task, then the affected project's supported test targets. Run the complete NJsonSchema build/test/package gate before final review; Windows CI must cover framework-only targets.
4. Repeat the full PR review after repairs, including API comparison, semantic schema/generated-output comparison, numeric/diagnostic performance probes, and record actual NSwag integration as deferred to NSwag #5355. New findings join the same checklist.
5. Require successful Windows and Ubuntu CI for the exact final pushed head, and an updated PR description/changelog that agrees with the code. This completes the NJsonSchema verification report only; do not merge or release here, and keep downstream readiness pending the separate NSwag #5355 gate.

Non-goals: unrelated master fixes, new dialect support, #1917, a public path-format redesign, wholesale serializer replacement, and the final `v12` release/merge to master.

## Follow-up master comparison (2026-09-12)

The subsequent comparison against master `18ba2ccfd20d795033d00b5e01585a94e2b78486`, using functional migration source `74e71db7324143a4a193cf9d020c42915f2fb356` (identical production sources at documentation head `1ff8c6a4`), found substantial load/save latency and allocation regressions. Earlier diagnostic speedups compared two migration revisions, not master. See the [migration guide assessment](../../changelog_v12.md#performance-and-outstanding-readiness) and PR verification section for measurements and limitations.

The prior correctness/API/package/CI gates remain historical evidence. Readiness additionally requires resolving and remeasuring the load/save regressions, resolving or explicitly scoping the draft-4 integer distinction under the specification-first policy, and completing the separately sequenced NSwag #5355 integration. No performance fix or new dialect implementation is claimed by this documentation update.
