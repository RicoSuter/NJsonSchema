# PR #1914 Phase 1: Values and Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Resolve S1, V1, V2, V3, and V4 without losing JSON values or introducing a new public API.

**Architecture:** Separate literal-value materialization from extension-schema detection. Use one internal exact JSON-number representation for equality and integer recognition, and one structural value comparer for enum and uniqueness validation. Recognize CLR-backed numeric nodes consistently when applying existing numeric constraints.

**Tech Stack:** C#, System.Text.Json/JsonNode, xUnit v3, existing .NET target frameworks and build scripts.

**Spec:** [Stabilization design and full roadmap](../specs/2026-09-11-pr-1914-stabilization-design.md).

Status: implementation and task reviews complete through `a8f6b86d` on 2026-09-12. S1, V1–V4, and the additional direct-node finding V11 are repaired. Local core/code-generation checks passed; final whole-PR verification and Windows CI remain at the final gate. NSwag companion fixes follow later in PR #5355.

## Global constraints

- Work on the existing PR #1914 migration branch targeting `v12`. Do not merge or release as part of this plan.
- Preserve all current public signatures and supported TFMs. Do not add a third-party dependency or reintroduce Newtonsoft into core.
- Use AAA tests through public loading/serialization/validation APIs. Run each regression before fixing it and confirm it fails for the stated behavior.
- Keep literal JSON distinct from schemas, null distinct from missing, and number equality distinct from string equality.
- Existing primitive CLR mappings should remain where lossless. Preserve precise values that cannot use those mappings as a supported exact JSON representation, and document observable runtime-type consequences. Never silently round through double.
- Equality and integer recognition need exact lexical-number handling. This phase does not replace all numeric-bound/multiple-of arithmetic or change public decimal-valued schema limits. Preserve existing behavior for parsed numbers outside decimal range; record a new finding if probing reveals another regression.
- Use CRLF for new C# source and the existing snapshot encoding conventions. Never accept snapshots wholesale to make tests pass.
- Before commits, update the v12 changelog with the actual user-visible fixes and run the listed checks. No speculative completion claims in documentation.

## File map

| File | Responsibility |
| --- | --- |
| `src/NJsonSchema/Infrastructure/JsonSchemaSerialization.cs` | Split literal conversion from schema-aware extension materialization; retain exact literal numbers. |
| `src/NJsonSchema/Infrastructure/JsonNumber.cs` (new, internal) | Canonical finite JSON number and exact equality/integrality. |
| `src/NJsonSchema/Infrastructure/JsonValueComparer.cs` (new, internal) | JSON structural equality and matching hash semantics. |
| `src/NJsonSchema/Validation/JsonSchemaValidator.cs` | Enum/uniqueItems integration and CLR-backed numeric-node handling. |
| `src/NJsonSchema.Tests/Serialization/LiteralJsonValueTests.cs` (new) | S1 roundtrip regressions. |
| `src/NJsonSchema.Tests/Validation/JsonValueValidationTests.cs` (new) | V1/V3 structural and numeric equality regressions. |
| `src/NJsonSchema.Tests/Validation/NumericNodeValidationTests.cs` (new) | V2/V4 direct-node and exact-integrality regressions. |
| `docs/changelog_v12.md` | Record landed repairs and the whole-valued-number policy. |

If these helpers already exist when execution begins, extend them instead of creating competing implementations. Check consumers of `ConvertJsonElement`, including code-generation value conversion, before changing its contract.

## Task 1: preserve literal values (S1)

- [x] Add `LiteralJsonValueTests` covering default, example where supported, and enum objects with schema-shaped keys at the root and nested inside arrays/objects. Include `{"type":"foo","name":"test"}`, valid-looking `{"type":"string"}`, and nested `properties` payloads.
- [x] Add precision cases to the same roundtrip matrix: adjacent integers above 2^53 and a decimal with more precision than double. Compare exact numeric values, not formatting or `ToString()` output.
- [x] Run the focused tests and capture the pre-fix failures.
- [x] Separate literal conversion from extension-schema detection. Default/example/enum conversion must recursively use only literal conversion. Keep extension schema materialization in its own entry point and preserve its existing reference behavior for the later traversal phase.
- [x] Keep int/long and other existing lossless primitive representations where appropriate; retain a cloned JSON representation for values that would otherwise lose precision. Verify code-generation callers can consume it; do not retain elements backed by a disposed document.
- [x] Run serialization and existing extension-reference tests. Add a positive control showing an existing supported extension-schema reference still resolves.
- [x] Update the changelog, inspect the diff, and commit `Preserve literal JSON values during schema deserialization`.

Representative public regression (add required usings and the surrounding test class):

```csharp
[Fact]
public async Task Default_WithSchemaShapedObject_PreservesLiteralType()
{
    // Arrange
    const string json = """{"default":{"type":"foo","name":"test"}}""";

    // Act
    var schema = await JsonSchema.FromJsonAsync(json);
    var output = JsonNode.Parse(schema.ToJson());

    // Assert
    Assert.Equal("foo", output["default"]["type"].GetValue<string>());
    Assert.Equal("test", output["default"]["name"].GetValue<string>());
}
```

Use `JsonSchemaSerialization.FromJsonAsync` and `ToJson` with `JsonSchema.CreateSchemaSerializationConverter(schemaType)` for the dialect matrix; copy the existing resolver setup from serialization tests. Assert the keyword supported by that dialect, not invented cross-dialect output.

## Task 2: exact numeric identity and integer recognition (V4; foundation for V1/V3)

- [x] Add public integer-validation cases for `1`, `1.0`, `1e0`, `100e-2`, `-0`, `1.00000000000000001`, `1e-1000`, negative fractions, and large exponents. The last two reported regressions must fail before implementation.
- [x] Implement internal `JsonNumber` from valid JSON numeric text: sign, significant decimal digits, and base-10 exponent. Remove leading coefficient zeros; remove trailing zeros while adjusting exponent; normalize signed zero. Never expand `10^exponent` or parse through double for equality/integrality.
- [x] Store/compare exponents without fixed-width overflow. Use a built-in representation available on every supported TFM; if using `BigInteger`, verify the net462 reference requirements in the production build. Do not add a NuGet dependency just for this helper.
- [x] Define equality as identical normalized sign/digits/exponent, and integrality as zero or a nonnegative normalized exponent. Generate hashes from the same canonical components. Parse numeric `JsonElement` text without rounding; serialize supported CLR numeric values to valid JSON numeric text before normalization.
- [x] Route integer recognition through the helper. Keep the existing numeric-bound pipeline until Task 4; do not widen public numeric schema properties.
- [x] Check malformed/non-finite CLR values follow STJ's supported JSON-input behavior and cannot silently validate as finite numbers.
- [x] Run numeric validation tests on net8.0 and net9.0, build the core project's full TFM set, update the changelog, and commit `Recognize integer JSON values without floating point rounding`.

Acceptance examples for canonicalization:

| Input | Canonical meaning | Integer? |
| --- | --- | --- |
| `1`, `1.0`, `1e0`, `100e-2` | positive digits `1`, exponent `0` | yes |
| `-0`, `0.000e999999` | zero | yes |
| `9007199254740993` | digits retained exactly | yes |
| `1.00000000000000001` | digits `100000000000000001`, exponent `-17` | no |
| `1e-1000` | digits `1`, exponent `-1000` | no |

Representative public regression:

```csharp
[Theory]
[InlineData("1.00000000000000001")]
[InlineData("1e-1000")]
public void Integer_WithNonIntegralExactValue_IsRejected(string json)
{
    // Arrange
    var schema = new JsonSchema { Type = JsonObjectType.Integer };

    // Act
    var errors = schema.Validate(json);

    // Assert
    Assert.Contains(errors, error => error.Kind == ValidationErrorKind.IntegerExpected);
}
```

## Task 3: structural enum and uniqueness comparison (V1, V3)

- [x] Add positive and negative enum cases for booleans, null, numbers, strings, objects, and arrays across all three `SchemaType` values. Include enum `true` versus instance `true`, enum `1.0` versus instance `1`, and an object enum with reordered properties.
- [x] Add `uniqueItems` cases for adjacent large integers, equal alternate number spellings, nested objects with reordered properties, ordered arrays, missing versus null properties, and string `"1"` versus number `1`. Confirm the reported failures before implementing.
- [x] Implement `JsonValueComparer` using Task 2 numeric identity, ordinal string/property-name comparison, unordered object members, ordered array elements, and explicit null/boolean handling. Equality and hashing must agree. Avoid quadratic pairwise comparison of every array item; use hash buckets with structural equality to handle collisions.
- [x] Adapt enum values from their supported public representations (CLR primitives, dictionaries/arrays, `JsonElement`, `JsonNode`) at the comparison boundary without treating their objects as schemas or changing user-owned nodes. Preserve existing explicit nullability behavior around enum checks.
- [x] Replace enum `ToString()` comparison and the double-based uniqueness key with the shared comparer. Test both parsed schemas and schemas assembled through the public object model.
- [x] Run all validation tests and literal serialization tests; record any intentional standards correction, update the changelog, and commit `Compare enumeration and unique items as exact JSON values`.

Representative public regression:

```csharp
[Fact]
public async Task UniqueItems_WithAdjacentLargeIntegers_AcceptsBoth()
{
    // Arrange
    var schema = await JsonSchema.FromJsonAsync(
        """{"type":"array","uniqueItems":true}""");

    // Act
    var errors = schema.Validate("[9007199254740992,9007199254740993]");

    // Assert
    Assert.Empty(errors);
}
```

## Task 4: CLR-backed numeric nodes and constraints (V2)

- [x] Add direct `new JsonSchemaValidator().Validate(node, schema)` tests for `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, and `decimal` nodes. Cover integer/number schemas, an untyped schema with bounds, and the numeric node produced by `SampleJsonDataGenerator`.
- [x] Assert parsed-node/direct-node parity for minimum, maximum, exclusive bounds, and multipleOf at representable boundaries. Include an unsigned value below minimum and values above `long.MaxValue`. Test fractions and negative values where the CLR type supports them.
- [x] Confirm the direct int/float failures and silent unsigned bound bypass before implementation.
- [x] Cover the adjacent direct-node regression V11 discovered during comparer review: the v11 validator accepted Date/Guid/TimeSpan/Uri as string values, while STJ `JsonValue` backed by those CLR types fails the current `TryGetValue<string>` check. Add direct/public parsed-node parity tests for these string forms (plus char), customized object/array values, and a custom serialized null, including nested nodes. Reuse the comparer repair's narrow internal customized-value normalization where appropriate; normalize only incompatible `JsonValue` representations, never reparse the whole document or mutate caller nodes. Preserve numeric primitive coverage and raw parsed number precision. Include format and length constraints to prove these values do not bypass validation. Record actual semantics in the changelog.
- [x] Consolidate numeric recognition/conversion used by `IsNumericValue`, `GetDecimalValue`, and `GetDoubleValue`. Cover every supported primitive and parsed element. Use checked conversions and the existing overflow fallback intentionally; never interpret conversion failure as successful validation or as a nonnumeric value that skips constraints.
- [x] Reuse exact integer recognition from Task 2. Do not fix the direct API merely by adding an unconditional serialize/parse cycle to the whole document.
- [x] Run the whole core test project on net8.0 and net9.0, build all production TFMs, update the changelog, and commit `Validate CLR-backed numeric JsonValue instances consistently`.

Representative public regression:

```csharp
[Fact]
public void Minimum_WithUnsignedNode_ReportsBoundViolation()
{
    // Arrange
    var schema = new JsonSchema { Minimum = 5 };
    var node = JsonValue.Create((uint)1);

    // Act
    var errors = new JsonSchemaValidator().Validate(node, schema);

    // Assert
    Assert.Contains(errors, error => error.Kind == ValidationErrorKind.NumberTooSmall);
}
```

## Commands and phase gate

Run from the NJsonSchema repository root. The quoted filter matches the three proposed test classes; narrow it per task when demonstrating a failure.

```bash
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -c Release -f net8.0 --filter 'FullyQualifiedName~LiteralJsonValueTests|FullyQualifiedName~JsonValueValidationTests|FullyQualifiedName~NumericNodeValidationTests'
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -c Release -f net8.0
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -c Release -f net9.0
dotnet build src/NJsonSchema/NJsonSchema.csproj -c Release
git -c core.whitespace=cr-at-eol diff --check
```

- [ ] Review the five finding IDs against their original reproductions and the new tests. Check exact-number behavior, object-order/hash consistency, node ownership, disposed element lifetimes, and all callers of changed helpers.
- [ ] Run the existing C# and TypeScript code-generation suites because defaults and enum values feed generators. Use `npm ci` in `src/NJsonSchema.CodeGeneration.TypeScript.Tests` if dependencies are missing, then run:

```bash
dotnet test src/NJsonSchema.CodeGeneration.CSharp.Tests/NJsonSchema.CodeGeneration.CSharp.Tests.csproj -c Release
dotnet test src/NJsonSchema.CodeGeneration.TypeScript.Tests/NJsonSchema.CodeGeneration.TypeScript.Tests.csproj -c Release
```

- [ ] Cross-check affected APIs/value consumption in the NSwag companion sources and record the inspected head. The full downstream test gate remains mandatory in Phase 5; do not label NSwag migration compatibility complete here.
- [ ] Verify Windows framework tests in CI on the exact pushed repair head. Do not infer those results from macOS test runs or a cross-compilation.
- [ ] Report the completed IDs, actual test totals, any new findings/contract decisions, and the remaining phases. Prepare the Phase 2 execution plan against the resulting code before beginning reference/context repairs.
