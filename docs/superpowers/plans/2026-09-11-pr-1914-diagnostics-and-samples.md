# PR #1914 Phase 4: Diagnostics and Samples Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Resolve V5–V10 while preserving the existing public diagnostic-path format and supported sample-input behavior.

**Architecture:** Use unambiguous internal location identity and bounded source-coordinate lookup. Reuse the sample string parser for stream input. Infer supported date formats consistently and retain JSON null array items.

**Tech Stack:** Existing C#/STJ/xUnit projects and supported TFMs.

**Spec:** [Stabilization design](../specs/2026-09-11-pr-1914-stabilization-design.md).

## Global Constraints

- Execute after earlier serialization repairs so lenient sample parsing reuses their syntax normalization without schema-keyword coercion.
- Preserve public API signatures and `ValidationError.Path` spelling. Internal identity must distinguish object names from array indices, including dots, brackets, slashes, tildes, empty names, quotes, and null values.
- No NSwag edits or full downstream suite in this phase; those follow in the companion PR per the user.
- Tests use AAA and public behavior. Demonstrate reported failures before production edits, then run focused tests and the core suite before commit.
- Preserve supported BOM decoding and prior stream-closing behavior. Restrict inferred dates to formats the validator accepts; culture-dependent human dates remain ordinary strings.
- Record actual behavior changes in `docs/changelog_v12.md`. Do not redesign public source-coordinate semantics.

## Task 1: bounded and unambiguous source coordinates (V5, V9, V10)

Files: `src/NJsonSchema/Validation/JsonSchemaValidator.cs`, `JsonPropertyToken.cs` and internal location metadata on `ValidationError.cs` if needed; `src/NJsonSchema.Tests/Validation/LineInformationTest.cs` and `LineInformationMultiByteTest.cs`. A focused internal location helper is allowed if it reduces the existing validator's responsibilities.

- [x] Add two failing errors in one document: a literal `"a.b"` property and nested `a`/`b` on different lines. Assert each token's actual line, plus property-level forbidden-property errors and null-valued properties.
- [x] Add root `null` with a non-null schema: expected `HasLineInfo = true`, line 1, position 4. Cover LF, CRLF, and CR-only documents and multibyte names/values. Include nested child/multi-type errors.
- [x] Add collision controls for bracket-looking object keys versus array indices, empty names, escaped quotes, backslashes, and `/`/`~`. Assert existing public `Path` output remains unchanged while source coordinates are correct.
- [x] Run the line-information tests before fixing the code and retain expected failing output.
- [x] Replace ambiguous display-path map keys with structured/escaped internal keys or node/container identity derived from the actual tree. Do not parse unescaped display paths to reconstruct property identity. Internal property tokens may retain owner/name for null-valued property locations without exposing a new public type.
- [x] For null child values, retain their actual parent container and property/index when collecting errors; a null token has no node identity and the display path alone cannot distinguish colliding names. Carry this internally, including nested child/multi-type errors, without adding public members. Property-error tokens currently clone values, so retain original owner identity separately if using node-based lookup.
- [x] Preserve token-end character coordinates. Count CRLF as one newline and support bare CR. Root null must map to the root source token despite lacking a `JsonNode` instance.
- [x] Avoid building a location map when validation produced no errors. For documents with errors, avoid reverse linear line scans and repeated prefix decoding per token: use a monotonic source scan or a line index plus bounded character-position computation. Avoid replacing one quadratic path with another on single-line arrays or wide objects.
- [x] Use a temporary benchmark program (not a timing assertion in unit tests) to warm up and measure median runs for valid pretty arrays of 20k/40k/80k values, invalid arrays with an error near the end, and a long single-line input. Record exact commits, runtime, payload, repetitions, and before/after durations. Baseline can be a temporary source snapshot at `d0ae5c5b`; do not alter the active worktree to benchmark old code.
- [x] Run all line tests and core tests, update changelog, review diff, and commit `Preserve validation source locations with bounded lookup`.

Focused command:

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -c Release -f net8.0 --filter 'FullyQualifiedName~LineInformation'
```

## Task 2: sample input and JSON null contracts (V6, V7, V8)

Files: `src/NJsonSchema/SampleJsonSchemaGenerator.cs`, `src/NJsonSchema/Generation/SampleJsonDataGenerator.cs`, existing `Generation/SampleJsonSchemaGeneratorTests.cs`, `SampleJsonDataGeneratorTests.cs`, and `SampleJsonDataGeneratorRegressionTests.cs` under the core tests project.

- [x] Compare string and stream schema generation for comments, trailing commas, single quotes, unquoted names, UTF-8 BOM, and UTF-16 BOM. Assert the same schema semantics and previous stream disposal on success/failure. Do not merely compare two outputs without asserting the expected properties/types.
- [x] Under at least two different current cultures, generate a schema from `"10/12/2024"`; it must remain an ordinary string and validate its input. Add supported ISO date/date-time controls, including midnight timestamps and timezone offsets, to avoid confusing date-time with date based on its clock value. Restore culture in `finally`.
- [x] Generate sample data for `{"type":"array","minItems":1,"items":{"type":"null"}}`: assert one null entry and validate the serialized generated sample. Preserve existing recursion termination for cyclic schemas; do not turn absent recursive samples into unintended infinite expansion.
- [x] Run new regressions and capture red output before production edits.
- [x] Implement stream generation through BOM-aware `StreamReader` and the existing string path; retain disposal semantics.
- [x] Replace broad culture-dependent `DateTime.TryParse` detection with supported format checks that agree with validators. Keep unrelated Guid/URI inference intact.
- [x] Retain an intentionally generated null in array output; distinguish it from recursion/absence where necessary. Reuse existing null sentinel behavior only where correct and test serialized output, not internal representation.
- [x] Run all sample generation tests, then core tests on net8.0/net9.0, update changelog, and commit `Preserve sample input and null value contracts`.

Focused command:

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -c Release -f net8.0 --filter 'FullyQualifiedName~SampleJson'
```

## Phase verification

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -c Release -f net8.0
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -c Release -f net9.0
dotnet build src/NJsonSchema/NJsonSchema.csproj -c Release
git -c core.whitespace=cr-at-eol diff --check
```

- [x] Review each finding against its regression and benchmark evidence.
- [ ] Include benchmark outcomes and remaining limitations in the final PR description; do not report performance from unrelated old probes as current results.
- [x] Require Windows framework execution in final CI. Proceed to documentation/API comparison and whole-PR review; the NSwag companion gate remains explicitly pending.

Execution checkpoint: completed and independently reviewed through `1d31c332`. Core net8: 889 passed/7 skipped; net9: 892 passed/7 skipped. All production core TFMs build cleanly. Source-location benchmark uses the same .NET 8.0.11/Arm64 harness for baseline `d0ae5c5b` and repair `5fe8afa9`: 80k pretty arrays improved from 1255/1262 ms (valid/invalid-last) to 26.7/29.6 ms. Smaller measurements are noisy; comparison includes intervening repairs. Windows net472 and final package/review/CI gates remain pending, with NSwag explicitly deferred.
